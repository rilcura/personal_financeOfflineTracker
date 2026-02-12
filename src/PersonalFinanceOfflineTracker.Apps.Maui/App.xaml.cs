using PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

namespace PersonalFinanceOfflineTracker.Apps.Maui;

public partial class App : Application
{
    private readonly ISyncBackgroundWorker _syncBackgroundWorker;

    public App(ISyncBackgroundWorker syncBackgroundWorker)
    {
        _syncBackgroundWorker = syncBackgroundWorker;
        InitializeComponent();
        _ = _syncBackgroundWorker.StartAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "PersonalFinanceOfflineTracker.Apps.Maui" };
    }
}
