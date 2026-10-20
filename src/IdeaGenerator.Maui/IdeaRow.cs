using IdeaGenerator.Core.Models;

namespace IdeaGenerator.Maui;

/// <summary>Display row for the Idea Library list.</summary>
public sealed class IdeaRow
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PrimaryControl { get; init; } = string.Empty;
    public bool IsPinned { get; init; }
    public bool CanUnpin => IsPinned;

    public static IdeaRow From(Idea idea) => new()
    {
        Id = idea.Id,
        Title = idea.Title,
        Path = idea.ContentType.DisplayName(idea.UseCaseSubtype),
        Category = idea.Category,
        Status = idea.Status.DisplayName(),
        PrimaryControl = idea.PrimaryControl,
        IsPinned = idea.IsPinned,
    };
}
