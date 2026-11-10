using IbgeCenter.Helpers;

namespace IbgeCenter.Models;

public enum TipoLocalidade
{
    Estado,
    Municipio
}

/// <summary>Item exibido no Picker (estado ou município), já normalizado para a UI.</summary>
public class Localidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Uf { get; set; }
    public TipoLocalidade Tipo { get; set; }

    public string DisplayName => string.IsNullOrEmpty(Uf) ? Nome : $"{Nome} ({Uf})";

    private string? _textoBusca;
    public string TextoBusca => _textoBusca ??= TextoUtil.Normalizar(DisplayName);
}
