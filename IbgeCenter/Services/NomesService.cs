using System.Globalization;
using System.Text.Json;
using IbgeCenter.Helpers;
using IbgeCenter.Models;

namespace IbgeCenter.Services;

public interface INomesService
{
    Task<NomesResultado> GetNomesAsync(int idLocalidade, CancellationToken ct = default);
}

/// <summary>
/// Cliente da API de Nomes do IBGE (Censo 2022).
/// Consulta primeiro o endpoint v3 pedido na atividade. Como o formato do JSON é lido de forma
/// tolerante (procura objetos com "nome" + "frequencia"/"ranking" em qualquer nível), o parser
/// aceita variações de estrutura. Se o v3 falhar ou vier vazio, usa o ranking v2 por sexo.
/// </summary>
public class NomesService : INomesService
{
    private const string UrlV3 = "https://servicodados.ibge.gov.br/api/v3/nomes/2022/localidade/{0}";
    private const string UrlV2 = "https://servicodados.ibge.gov.br/api/v2/censos/nomes/ranking?localidade={0}&sexo={1}";
    private const int Limite = 20;

    private readonly HttpClient _http;

    public NomesService(HttpClient http) => _http = http;

    public async Task<NomesResultado> GetNomesAsync(int idLocalidade, CancellationToken ct = default)
    {
        var coletor = new Coletor();

        try
        {
            var json = await _http.GetStringAsync(string.Format(UrlV3, idLocalidade), ct);
            using var doc = JsonDocument.Parse(json);
            Percorrer(doc.RootElement, new Contexto(), coletor);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // segue para o fallback abaixo
        }

        if (!coletor.TemDados)
        {
            foreach (var (sexo, genero) in new[] { ("M", Genero.Masculino), ("F", Genero.Feminino) })
            {
                var json = await _http.GetStringAsync(string.Format(UrlV2, idLocalidade, sexo), ct);
                using var doc = JsonDocument.Parse(json);
                Percorrer(doc.RootElement, new Contexto(genero), coletor);
            }
        }

        return new NomesResultado
        {
            Homens = Montar(coletor.Homens),
            Mulheres = Montar(coletor.Mulheres),
            Sobrenomes = Montar(coletor.Sobrenomes),
            Gerais = Montar(coletor.Gerais)
        };
    }

    // ---------- Parser tolerante ----------

    private enum Genero { Masculino, Feminino }

    private readonly record struct Contexto(Genero? Genero = null, bool Sobrenome = false);

    private readonly record struct Item(string Nome, long Frequencia, int? Ranking);

    private sealed class Coletor
    {
        public List<Item> Homens { get; } = new();
        public List<Item> Mulheres { get; } = new();
        public List<Item> Sobrenomes { get; } = new();
        public List<Item> Gerais { get; } = new();

        public bool TemDados => Homens.Count + Mulheres.Count + Sobrenomes.Count + Gerais.Count > 0;
    }

    private static void Percorrer(JsonElement el, Contexto ctx, Coletor col)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var filho in el.EnumerateArray())
                    Percorrer(filho, ctx, col);
                break;

            case JsonValueKind.Object:
                var atual = AtualizarContexto(el, ctx);

                if (TentarLerEntrada(el, out var entrada))
                {
                    Adicionar(col, atual, entrada);
                    return;
                }

                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                        Percorrer(prop.Value, ContextoPorChave(prop.Name, atual), col);
                }
                break;
        }
    }

    /// <summary>Lê propriedades de texto como "sexo" ou "tipo" para saber a categoria dos filhos.</summary>
    private static Contexto AtualizarContexto(JsonElement obj, Contexto ctx)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (p.Value.ValueKind != JsonValueKind.String) continue;

            var chave = NormalizarChave(p.Name);
            var valor = p.Value.GetString() ?? string.Empty;

            if (chave is "sexo" or "genero")
            {
                var g = LerGenero(valor);
                if (g is not null) ctx = ctx with { Genero = g };
            }
            else if (chave is "tipo" or "categoria" or "tiponome")
            {
                if (valor.Contains("sobrenome", StringComparison.OrdinalIgnoreCase))
                    ctx = ctx with { Sobrenome = true };
            }
        }
        return ctx;
    }

    /// <summary>O nome da própria propriedade também indica a categoria (ex.: "sobrenomes", "masculino").</summary>
    private static Contexto ContextoPorChave(string chave, Contexto ctx)
    {
        var k = NormalizarChave(chave);

        if (k.Contains("sobrenome")) return ctx with { Sobrenome = true };
        if (k.Contains("masc") || k.Contains("homem")) return ctx with { Genero = Genero.Masculino };
        if (k.Contains("fem") || k.Contains("mulher")) return ctx with { Genero = Genero.Feminino };
        return ctx;
    }

    private static bool TentarLerEntrada(JsonElement obj, out Item item)
    {
        item = default;

        if (!obj.TryGetProperty("nome", out var nomeProp) || nomeProp.ValueKind != JsonValueKind.String)
            return false;

        var nome = nomeProp.GetString();
        if (string.IsNullOrWhiteSpace(nome)) return false;

        long? freq = null;
        int? rank = null;

        foreach (var p in obj.EnumerateObject())
        {
            var chave = NormalizarChave(p.Name);
            if (chave is "frequencia" or "freq") freq = LerLong(p.Value);
            else if (chave is "ranking" or "rank" or "posicao") rank = (int?)LerLong(p.Value);
        }

        // Objeto de localidade também tem "nome", mas não tem frequência nem ranking.
        if (freq is null && rank is null) return false;

        item = new Item(nome, freq ?? 0, rank);
        return true;
    }

    private static void Adicionar(Coletor col, Contexto ctx, Item item)
    {
        var lista = ctx.Sobrenome
            ? col.Sobrenomes
            : ctx.Genero switch
            {
                Genero.Masculino => col.Homens,
                Genero.Feminino => col.Mulheres,
                _ => col.Gerais
            };
        lista.Add(item);
    }

    private static Genero? LerGenero(string valor) =>
        valor.Trim().ToLowerInvariant() switch
        {
            "m" or "h" or "masculino" or "homem" or "homens" => Genero.Masculino,
            "f" or "feminino" or "mulher" or "mulheres" => Genero.Feminino,
            _ => null
        };

    private static long? LerLong(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : (long)e.GetDouble(),
        JsonValueKind.String when long.TryParse(e.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) => s,
        _ => null
    };

    private static string NormalizarChave(string chave) =>
        TextoUtil.Normalizar(chave).Replace("_", string.Empty);

    private static IReadOnlyList<NomeFrequencia> Montar(List<Item> itens) =>
        itens
            .GroupBy(i => i.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.Frequencia).First())
            .OrderBy(i => i.Ranking ?? int.MaxValue)
            .ThenByDescending(i => i.Frequencia)
            .Take(Limite)
            .Select((i, idx) => new NomeFrequencia
            {
                Ranking = i.Ranking ?? idx + 1,
                Nome = TextoUtil.Capitalizar(i.Nome),
                Frequencia = i.Frequencia
            })
            .ToList();
}
