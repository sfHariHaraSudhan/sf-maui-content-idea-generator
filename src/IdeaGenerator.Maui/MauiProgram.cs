using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdeaGenerator.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLogging(logging =>
            {
                logging.ClearExisting();
                logging.AddDebug();
            });

        return builder.Build();
    }
}
