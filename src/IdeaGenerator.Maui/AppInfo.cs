using System.Reflection;

namespace IdeaGenerator.Maui;

/// <summary>Application identity and version metadata.</summary>
public static class AppInfo
{
    public const string Name = "Syncfusion Content Idea Generator";

    public static string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? Assembly.GetExecutingAssembly().ImageRuntimeVersion
        ?? "1.0.0";

    public static string BuildNumber =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyMetadataAttribute>("BuildNumber")?.Value
        ?? "1";
}
