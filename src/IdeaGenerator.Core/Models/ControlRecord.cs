namespace IdeaGenerator.Core.Models;

/// <summary>
/// One verified Syncfusion .NET MAUI control entry in the local knowledge
/// catalogue (Content/controls.json). Read-only reference data — Phase 1 only
/// defines the structure; knowledge is consumed by later phases.
/// </summary>
public sealed class ControlRecord
{
    public string ControlName { get; set; } = string.Empty;

    /// <summary>Common/legacy names mapped to this record for lookups and similarity.</summary>
    public List<string> Aliases { get; set; } = new();

    public string Purpose { get; set; } = string.Empty;

    public List<string> VerifiedFeatures { get; set; } = new();

    public List<string> TypicalCustomerOperations { get; set; } = new();

    public List<string> SuitableCategories { get; set; } = new();

    public List<string> SuitableWorkflows { get; set; } = new();

    public List<string> UsefulSupportingControls { get; set; } = new();

    public List<string> PossibleAiAssistedOperations { get; set; } = new();

    public List<string> MobileDesignConsiderations { get; set; } = new();

    public List<string> PatternsToAvoid { get; set; } = new();

    public string DocumentationReference { get; set; } = string.Empty;

    public string LastReviewedDate { get; set; } = string.Empty;
}
