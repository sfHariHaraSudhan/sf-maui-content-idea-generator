namespace IdeaGenerator.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Initialize the local database and idempotent sample seed
        // before the Idea Library is displayed.
        _ = AppDataService.Current;

        return new Window(new AppShell());
    }
}
