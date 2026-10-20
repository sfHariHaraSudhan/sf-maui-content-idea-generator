namespace IdeaGenerator.Core.Models;

/// <summary>Content type of an idea. The Use Case subtype is stored separately.</summary>
public enum ContentType
{
    AiSolution = 0,
    UseCaseSample = 1,
    UiKit = 2,
}

/// <summary>Subtype of a Use Case Sample idea.</summary>
public enum UseCaseSubtype
{
    WithAi = 0,
    WithoutAi = 1,
}

/// <summary>Persistent status of a saved idea. "Generated" is intentionally NOT a status.</summary>
public enum IdeaStatus
{
    Planned = 0,
    InProgress = 1,
    UnderReview = 2,
    Completed = 3,
    Published = 4,
}

/// <summary>Where an idea came from.</summary>
public enum IdeaSource
{
    Sample = 0,
    UserAdded = 1,
    Generated = 2,
    UserProvided = 3,
}

public static class ContentTypeExtensions
{
    public static string DisplayName(this ContentType type, UseCaseSubtype? subtype = null) =>
        type switch
        {
            ContentType.AiSolution => "AI Solution",
            ContentType.UseCaseSample => subtype switch
            {
                UseCaseSubtype.WithAi => "Use Case with AI",
                UseCaseSubtype.WithoutAi => "Use Case without AI",
                _ => "Use Case Sample",
            },
            ContentType.UiKit => "UI Kit",
            _ => type.ToString(),
        };

    public static string DisplayName(this IdeaStatus status) => status switch
    {
        IdeaStatus.Planned => "Planned",
        IdeaStatus.InProgress => "In progress",
        IdeaStatus.UnderReview => "Under review",
        IdeaStatus.Completed => "Completed",
        IdeaStatus.Published => "Published",
        _ => status.ToString(),
    };
}
