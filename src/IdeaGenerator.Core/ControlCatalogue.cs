using System.Reflection;
using System.Text.Json;
using IdeaGenerator.Core.Models;

namespace IdeaGenerator.Core;

/// <summary>
/// Loads the embedded, verified control-knowledge catalogue.
/// Minimal helper (not one of the five primary services): Phase 1 only needs
/// the structure and the control list for library filters.
/// </summary>
public static class ControlCatalogue
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public sealed record CatalogFile(
        string CatalogVersion,
        string LastReviewedDate,
        string SourceNote,
        List<ControlRecord> Controls);

    public static CatalogFile Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("controls.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded controls.json not found.");
        using var reader = new StreamReader(stream);
        return JsonSerializer.Deserialize<CatalogFile>(reader.ReadToEnd(), JsonOptions)
            ?? throw new InvalidOperationException("controls.json is empty or invalid.");
    }

    public static string CatalogVersion => Load().CatalogVersion;

    public static IReadOnlyList<string> ControlNames =>
        Load().Controls.Select(c => c.ControlName).OrderBy(n => n, StringComparer.Ordinal).ToList();
}
