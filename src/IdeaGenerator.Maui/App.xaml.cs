using Microsoft.Extensions.Hosting;

namespace IdeaGenerator.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Eagerly initialize the local database (and idempotent sample seed)
        // before any page renders.
        _ = AppDataService.Current;
        var window = base.CreateWindow(activationState);
        window.Page = new AppShell();
        return window;
    }
}
