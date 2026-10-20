namespace IdeaGenerator.Core.Models;

/// <summary>Search + filter criteria for the Idea Library list.</summary>
public sealed class IdeaFilter
{
    /// <summary>Free-text search over title, description, customer problem, keywords.</summary>
    public string? Search { get; set; }

    public ContentType? ContentType { get; set; }

    /// <summary>Only applied when <see cref="ContentType"/> is UseCaseSample.</summary>
    public UseCaseSubtype? UseCaseSubtype { get; set; }

    public string? Category { get; set; }

    public IdeaStatus? Status { get; set; }

    public string? PrimaryControl { get; set; }

    public bool? PinnedOnly { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        ContentType.HasValue ||
        UseCaseSubtype.HasValue ||
        !string.IsNullOrWhiteSpace(Category) ||
        Status.HasValue ||
        !string.IsNullOrWhiteSpace(PrimaryControl) ||
        PinnedOnly == true;

    public static IdeaFilter None { get; } = new();
}
