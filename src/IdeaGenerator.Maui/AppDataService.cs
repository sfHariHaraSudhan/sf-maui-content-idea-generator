using System.IO;
using IdeaGenerator.Core.Services;
using Microsoft.Maui.Storage;

namespace IdeaGenerator.Maui;

/// <summary>
/// Lazily creates the single IdeaDataService for the running app, stored in
/// the platform app-data directory. Shell pages are constructed by XAML
/// DataTemplates (no DI), so they resolve the service through this holder.
/// </summary>
public static class AppDataService
{
    private static readonly object Lock = new();
    private static IdeaDataService? _instance;

    public static IdeaDataService Current
    {
        get
        {
            if (_instance is null)
            {
                lock (Lock)
                {
                    _instance ??= Create();
                }
            }

            return _instance;
        }
    }

    private static IdeaDataService Create()
    {
        var directory = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(directory);
        var dbPath = Path.Combine(directory, "idea-library.sqlite");
        var service = new IdeaDataService(dbPath);
        service.LoadSampleIdeasIfMissing();
        return service;
    }
}
