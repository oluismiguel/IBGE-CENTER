using System.Text.Json.Serialization;

namespace IbgeCenter.Models;

/// <summary>Mapeia o JSON de https://servicodados.ibge.gov.br/api/v1/localidades/municipios</summary>
public class Municipio
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    public Microrregiao? Microrregiao { get; set; }

    [JsonPropertyName("regiao-imediata")]
    public RegiaoImediata? RegiaoImediata { get; set; }

    /// <summary>A UF pode vir por caminhos diferentes dependendo do município.</summary>
    [JsonIgnore]
    public string? UfSigla =>
        Microrregiao?.Mesorregiao?.UF?.Sigla
        ?? RegiaoImediata?.RegiaoIntermediaria?.UF?.Sigla;
}

public class Microrregiao
{
    public Mesorregiao? Mesorregiao { get; set; }
}

public class Mesorregiao
{
    public UfRef? UF { get; set; }
}

public class RegiaoImediata
{
    [JsonPropertyName("regiao-intermediaria")]
    public RegiaoIntermediaria? RegiaoIntermediaria { get; set; }
}

public class RegiaoIntermediaria
{
    public UfRef? UF { get; set; }
}

public class UfRef
{
    public int Id { get; set; }
    public string Sigla { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
}
