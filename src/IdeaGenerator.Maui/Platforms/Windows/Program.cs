using Microsoft.UI.Xaml;

// To customize application behavior such as theme, execution behavior, etc.,
// visit the docs: https://learn.microsoft.com/dotnet/maui/winmd/winmd-boilerplate?view=net-10.0

namespace IdeaGenerator.Maui.WinUi;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => IdeaGenerator.Maui.MauiProgram.CreateMauiApp();
}
