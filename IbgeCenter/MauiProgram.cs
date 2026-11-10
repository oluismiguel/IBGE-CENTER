using IbgeCenter.Services;
using IbgeCenter.ViewModels;
using IbgeCenter.Views;
using Microsoft.Extensions.Logging;

namespace IbgeCenter;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Infraestrutura HTTP compartilhada pelos serviços de integração
        builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(30) });

        // Camada de Serviços / Integração (APIs do IBGE)
        builder.Services.AddSingleton<ILocalidadesService, LocalidadesService>();
        builder.Services.AddSingleton<INomesService, NomesService>();

        // Camada de Apresentação
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
