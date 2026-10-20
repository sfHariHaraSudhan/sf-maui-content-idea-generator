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

    public static string BuildNumber
    {
        get
        {
            var metadata = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>();
            foreach (var attribute in metadata)
            {
                if (attribute.Key == "BuildNumber" && !string.IsNullOrWhiteSpace(attribute.Value))
                {
                    return attribute.Value;
                }
            }

            return "1";
        }
    }
}
