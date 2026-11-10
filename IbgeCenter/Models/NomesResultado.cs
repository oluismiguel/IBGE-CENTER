namespace IbgeCenter.Models;

/// <summary>Resultado já separado por categoria, pronto para a UI.</summary>
public class NomesResultado
{
    public IReadOnlyList<NomeFrequencia> Homens { get; set; } = Array.Empty<NomeFrequencia>();
    public IReadOnlyList<NomeFrequencia> Mulheres { get; set; } = Array.Empty<NomeFrequencia>();
    public IReadOnlyList<NomeFrequencia> Sobrenomes { get; set; } = Array.Empty<NomeFrequencia>();

    /// <summary>Nomes que a API devolveu sem indicar o sexo.</summary>
    public IReadOnlyList<NomeFrequencia> Gerais { get; set; } = Array.Empty<NomeFrequencia>();

    public bool TemDados => Homens.Count + Mulheres.Count + Sobrenomes.Count + Gerais.Count > 0;
}
