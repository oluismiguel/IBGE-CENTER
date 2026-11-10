using CommunityToolkit.Mvvm.ComponentModel;
using IbgeCenter.Helpers;
using IbgeCenter.Models;
using IbgeCenter.Services;

namespace IbgeCenter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string TipoEstado = "Estado";
    private const string TipoMunicipio = "Município";
    private const int MaxItensNoPicker = 300;

    private readonly ILocalidadesService _localidadesService;
    private readonly INomesService _nomesService;

    private IReadOnlyList<Localidade> _todas = Array.Empty<Localidade>();
    private CancellationTokenSource? _nomesCts;
    private int _versaoCarga;

    public MainViewModel(ILocalidadesService localidadesService, INomesService nomesService)
    {
        _localidadesService = localidadesService;
        _nomesService = nomesService;
        _ = CarregarLocalidadesAsync();
    }

    public IReadOnlyList<string> Tipos { get; } = new[] { TipoEstado, TipoMunicipio };

    // ---------- Seleção ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMunicipio))]
    private string tipoSelecionado = TipoEstado;

    [ObservableProperty]
    private string textoBusca = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<Localidade> localidades = Array.Empty<Localidade>();

    [ObservableProperty]
    private Localidade? localidadeSelecionada;

    public bool IsMunicipio => TipoSelecionado == TipoMunicipio;

    // ---------- Estado da tela ----------

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemStatus))]
    private string status = string.Empty;

    [ObservableProperty]
    private string tituloResultado = string.Empty;

    public bool TemStatus => !string.IsNullOrWhiteSpace(Status);

    // ---------- Resultados ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemHomens))]
    [NotifyPropertyChangedFor(nameof(TemResultado))]
    private IReadOnlyList<NomeFrequencia> homens = Array.Empty<NomeFrequencia>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemMulheres))]
    [NotifyPropertyChangedFor(nameof(TemResultado))]
    private IReadOnlyList<NomeFrequencia> mulheres = Array.Empty<NomeFrequencia>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemSobrenomes))]
    [NotifyPropertyChangedFor(nameof(TemResultado))]
    private IReadOnlyList<NomeFrequencia> sobrenomes = Array.Empty<NomeFrequencia>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemGerais))]
    [NotifyPropertyChangedFor(nameof(TemResultado))]
    private IReadOnlyList<NomeFrequencia> gerais = Array.Empty<NomeFrequencia>();

    public bool TemHomens => Homens.Count > 0;
    public bool TemMulheres => Mulheres.Count > 0;
    public bool TemSobrenomes => Sobrenomes.Count > 0;
    public bool TemGerais => Gerais.Count > 0;
    public bool TemResultado => TemHomens || TemMulheres || TemSobrenomes || TemGerais;

    // ---------- Reações às mudanças da UI ----------

    partial void OnTipoSelecionadoChanged(string value) => _ = CarregarLocalidadesAsync();

    partial void OnTextoBuscaChanged(string value) => AplicarFiltro();

    partial void OnLocalidadeSelecionadaChanged(Localidade? value)
    {
        // Seleção feita no dropdown dispara a consulta encadeada na API de Nomes.
        if (value is not null)
            _ = CarregarNomesAsync(value);
    }

    // ---------- Operações ----------

    private async Task CarregarLocalidadesAsync()
    {
        var versao = ++_versaoCarga;
        var municipio = IsMunicipio;

        _nomesCts?.Cancel();
        LimparResultados();
        LocalidadeSelecionada = null;
        TextoBusca = string.Empty;

        IsBusy = true;
        Status = municipio
            ? "Carregando municípios do IBGE (pode levar alguns segundos)..."
            : "Carregando estados do IBGE...";

        try
        {
            var lista = municipio
                ? await _localidadesService.GetMunicipiosAsync()
                : await _localidadesService.GetEstadosAsync();

            if (versao != _versaoCarga) return;

            _todas = lista;
            AplicarFiltro();
            Status = municipio
                ? "Digite para filtrar e escolha um município."
                : "Escolha um estado.";
        }
        catch (Exception ex)
        {
            if (versao != _versaoCarga) return;
            _todas = Array.Empty<Localidade>();
            Localidades = Array.Empty<Localidade>();
            Status = $"Erro ao carregar localidades: {ex.Message}";
        }
        finally
        {
            if (versao == _versaoCarga) IsBusy = false;
        }
    }

    private void AplicarFiltro()
    {
        var termo = TextoUtil.Normalizar(TextoBusca);

        IEnumerable<Localidade> consulta = _todas;
        if (termo.Length > 0)
            consulta = consulta.Where(l => l.TextoBusca.Contains(termo));

        Localidades = consulta.Take(MaxItensNoPicker).ToList();
    }

    private async Task CarregarNomesAsync(Localidade localidade)
    {
        _nomesCts?.Cancel();
        _nomesCts = new CancellationTokenSource();
        var ct = _nomesCts.Token;

        IsBusy = true;
        Status = $"Buscando nomes em {localidade.DisplayName}...";
        LimparResultados();

        try
        {
            var resultado = await _nomesService.GetNomesAsync(localidade.Id, ct);
            if (ct.IsCancellationRequested) return;

            Homens = resultado.Homens;
            Mulheres = resultado.Mulheres;
            Sobrenomes = resultado.Sobrenomes;
            Gerais = resultado.Gerais;

            TituloResultado = $"Nomes mais comuns em {localidade.DisplayName}";
            Status = resultado.TemDados
                ? string.Empty
                : "A API do IBGE não retornou dados para esta localidade.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // uma nova seleção substituiu esta consulta
        }
        catch (Exception ex)
        {
            Status = $"Não foi possível consultar os nomes: {ex.Message}";
        }
        finally
        {
            if (!ct.IsCancellationRequested) IsBusy = false;
        }
    }

    private void LimparResultados()
    {
        Homens = Array.Empty<NomeFrequencia>();
        Mulheres = Array.Empty<NomeFrequencia>();
        Sobrenomes = Array.Empty<NomeFrequencia>();
        Gerais = Array.Empty<NomeFrequencia>();
        TituloResultado = string.Empty;
    }
}
