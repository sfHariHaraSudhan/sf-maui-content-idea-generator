using Foundation;
using UIKit;

namespace IdeaGenerator.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

// Required for the UIScene lifecycle (Xcode 27 runtime validation).
// MAUI window creation stays in App.CreateWindow; this only identifies
// the scene delegate referenced by UIApplicationSceneManifest.
[Register("SceneDelegate")]
public class SceneDelegate : MauiUIWindowSceneDelegate
{
}

public static class Program
{
    public static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
