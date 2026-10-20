using System.Text.Json.Serialization;

namespace IdeaGenerator.Core.Models;

/// <summary>
/// A saved idea in the Idea Library. "Generated" is NOT a status:
/// candidates are transient and become an Idea with a real status on explicit save.
/// <see cref="IsPinned"/> is independent of saving.
/// </summary>
public sealed class Idea
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public ContentType ContentType { get; set; } = ContentType.UseCaseSample;

    /// <summary>Only meaningful for <see cref="ContentType.UseCaseSample"/>.</summary>
    public UseCaseSubtype? UseCaseSubtype { get; set; }

    public string Category { get; set; } = Categories.CommonOrGeneral;

    public IdeaStatus Status { get; set; } = IdeaStatus.Planned;

    public bool IsPinned { get; set; }

    public IdeaSource Source { get; set; } = IdeaSource.UserAdded;

    public string TargetCustomer { get; set; } = string.Empty;

    public string CustomerProblem { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string PrimaryControl { get; set; } = string.Empty;

    public string SupportingControls { get; set; } = string.Empty;

    public string ControlFeatures { get; set; } = string.Empty;

    public string Keywords { get; set; } = string.Empty;

    public string WorkflowSummary { get; set; } = string.Empty;

    /// <summary>AI operation summary; empty for non-AI paths.</summary>
    public string AiOperation { get; set; } = string.Empty;

    /// <summary>Screen names for UI Kit ideas, comma-separated.</summary>
    public string ScreenNames { get; set; } = string.Empty;

    /// <summary>Verbatim original input for I Have an Idea sources (later phase); never overwritten.</summary>
    public string? OriginalUserInput { get; set; }

    /// <summary>Content-path-specific payload, serialized to JSON.</summary>
    public IdeaDetails? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public string DisplayPath => ContentType.DisplayName(UseCaseSubtype);

    [JsonIgnore]
    public string StatusDisplay => Status.DisplayName();
}

/// <summary>Fixed initial category list. "Custom Category" is the escape hatch.</summary>
public static class Categories
{
    public const string CustomCategory = "Custom Category";
    public const string CommonOrGeneral = "Common or General";
    public const string Productivity = "Productivity";

    public static readonly IReadOnlyList<string> All = new[]
    {
        "Productivity",
        "Utility",
        CommonOrGeneral,
        "Trending Apps",
        "Professional Apps",
        "Healthcare",
        "Finance",
        "Retail",
        "Education",
        "Logistics",
        "Manufacturing",
        "Field Services",
        "Project Management",
        "Communication",
        "Personal Management",
        "Developer Tools",
        "Enterprise Operations",
        CustomCategory,
    };
}
