using System.Globalization;

namespace IbgeCenter.Models;

/// <summary>Um nome (ou sobrenome) com sua frequência e posição no ranking.</summary>
public class NomeFrequencia
{
    public int Ranking { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long Frequencia { get; set; }

    public string RankingText => $"{Ranking}º";

    public string FrequenciaText =>
        Frequencia > 0
            ? Frequencia.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) + " pessoas"
            : string.Empty;
}
