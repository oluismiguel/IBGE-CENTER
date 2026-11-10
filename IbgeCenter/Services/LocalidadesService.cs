using System.Net.Http.Json;
using System.Text.Json;
using IbgeCenter.Models;

namespace IbgeCenter.Services;

public interface ILocalidadesService
{
    Task<IReadOnlyList<Localidade>> GetEstadosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Localidade>> GetMunicipiosAsync(CancellationToken ct = default);
}

/// <summary>Cliente da API de Localidades do IBGE (com cache em memória).</summary>
public class LocalidadesService : ILocalidadesService
{
    private const string BaseUrl = "https://servicodados.ibge.gov.br/api/v1/localidades";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private IReadOnlyList<Localidade>? _estados;
    private IReadOnlyList<Localidade>? _municipios;

    public LocalidadesService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<Localidade>> GetEstadosAsync(CancellationToken ct = default)
    {
        if (_estados is not null) return _estados;

        var dados = await _http.GetFromJsonAsync<List<Estado>>($"{BaseUrl}/estados?orderBy=nome", JsonOptions, ct)
                    ?? new List<Estado>();

        _estados = dados
            .Select(e => new Localidade { Id = e.Id, Nome = e.Nome, Uf = e.Sigla, Tipo = TipoLocalidade.Estado })
            .ToList();
        return _estados;
    }

    public async Task<IReadOnlyList<Localidade>> GetMunicipiosAsync(CancellationToken ct = default)
    {
        if (_municipios is not null) return _municipios;

        var dados = await _http.GetFromJsonAsync<List<Municipio>>($"{BaseUrl}/municipios?orderBy=nome", JsonOptions, ct)
                    ?? new List<Municipio>();

        _municipios = dados
            .Select(m => new Localidade { Id = m.Id, Nome = m.Nome, Uf = m.UfSigla, Tipo = TipoLocalidade.Municipio })
            .ToList();
        return _municipios;
    }
}
