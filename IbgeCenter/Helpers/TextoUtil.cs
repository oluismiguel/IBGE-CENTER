using System.Globalization;
using System.Text;

namespace IbgeCenter.Helpers;

public static class TextoUtil
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Minúsculas e sem acentos, para buscas ("Sao Paulo" encontra "São Paulo").</summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var decomposto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>"MARIA DAS GRAÇAS" -> "Maria Das Graças".</summary>
    public static string Capitalizar(string texto) =>
        PtBr.TextInfo.ToTitleCase(texto.Trim().ToLower(PtBr));
}
