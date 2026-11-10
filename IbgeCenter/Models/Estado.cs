namespace IbgeCenter.Models;

/// <summary>Mapeia o JSON de https://servicodados.ibge.gov.br/api/v1/localidades/estados</summary>
public class Estado
{
    public int Id { get; set; }
    public string Sigla { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
}
