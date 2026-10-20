using Foundation;
using UIKit;

namespace IdeaGenerator.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

public static class Program
{
    public static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
