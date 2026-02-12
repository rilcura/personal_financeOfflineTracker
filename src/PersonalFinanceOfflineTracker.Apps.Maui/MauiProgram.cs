using Microsoft.Extensions.Logging;
using PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

namespace PersonalFinanceOfflineTracker.Apps.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        var syncOptions = new SyncClientOptions();
        builder.Services.AddSingleton(syncOptions);
        builder.Services.AddSingleton<ISyncOutboxStore, SqliteSyncOutboxStore>();
        builder.Services.AddSingleton<ISyncTokenStore, SecureStorageSyncTokenStore>();
        builder.Services.AddSingleton<ISyncOutboxService, SyncOutboxService>();
        builder.Services.AddSingleton<ISyncBackgroundWorker, SyncBackgroundWorker>();
        builder.Services.AddSingleton(sp =>
        {
            return new HttpClient
            {
                BaseAddress = new Uri(syncOptions.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(20),
            };
        });
        builder.Services.AddSingleton<ISyncApiClient, SyncApiClient>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
