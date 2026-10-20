using Microsoft.Extensions.Hosting;

namespace IdeaGenerator.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Main = new AppShell();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Eagerly initialize the local database (and idempotent sample seed)
        // before any page renders.
        _ = AppDataService.Current;
        return base.CreateWindow(activationState);
    }
}
