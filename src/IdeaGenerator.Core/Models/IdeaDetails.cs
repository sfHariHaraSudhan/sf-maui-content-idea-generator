using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdeaGenerator.Core.Models;

/// <summary>
/// Base for the four content-path payloads. Each idea stores exactly one of
/// these in <see cref="Idea.Details"/> (persisted as JSON).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(UseCaseWithoutAiDetails), nameof(UseCaseWithoutAiDetails))]
[JsonDerivedType(typeof(UseCaseWithAiDetails), nameof(UseCaseWithAiDetails))]
[JsonDerivedType(typeof(AiSolutionDetails), nameof(AiSolutionDetails))]
[JsonDerivedType(typeof(UiKitDetails), nameof(UiKitDetails))]
public abstract class IdeaDetails
{
    [JsonPropertyName("kind")]
    public abstract string Kind { get; }
}

/// <summary>Use Case Sample without AI. Must never contain AI behavior or terminology.</summary>
public sealed class UseCaseWithoutAiDetails : IdeaDetails
{
    public override string Kind => nameof(UseCaseWithoutAiDetails);

    public string DesiredOutcome { get; set; } = string.Empty;
    public string PracticalWorkflow { get; set; } = string.Empty;
    public string Pages { get; set; } = string.Empty;
    public string CustomerBenefit { get; set; } = string.Empty;
    public string WhyControlsAreEssential { get; set; } = string.Empty;
    public string ImplementationScope { get; set; } = string.Empty;
    public string ExpectedEffort { get; set; } = "1 to 2 working days";
}

/// <summary>Use Case Sample with AI. AI must operate on, not merely decorate, the controls.</summary>
public sealed class UseCaseWithAiDetails : IdeaDetails
{
    public override string Kind => nameof(UseCaseWithAiDetails);

    public string DesiredOutcome { get; set; } = string.Empty;
    public string PracticalWorkflow { get; set; } = string.Empty;
    public string Pages { get; set; } = string.Empty;
    public string CustomerBenefit { get; set; } = string.Empty;
    public string CustomerInput { get; set; } = string.Empty;
    public string AiOperation { get; set; } = string.Empty;
    public string StructuredAiOutput { get; set; } = string.Empty;
    public string HowOutputDrivesControls { get; set; } = string.Empty;
    public string UserReviewAndCorrection { get; set; } = string.Empty;
    public string InvalidResponseHandling { get; set; } = string.Empty;
    public string RetryOrFallback { get; set; } = string.Empty;
    public string WhyAiAddsCustomerValue { get; set; } = string.Empty;
    public string WhyControlsRemainEssential { get; set; } = string.Empty;
    public string ExpectedEffort { get; set; } = "1 to 2 working days";
}

/// <summary>AI Solution. One focused AI capability around one primary control.</summary>
public sealed class AiSolutionDetails : IdeaDetails
{
    public override string Kind => nameof(AiSolutionDetails);

    public string CustomerProblem { get; set; } = string.Empty;
    public string FeaturesEnhanced { get; set; } = string.Empty;
    public string CustomerInput { get; set; } = string.Empty;
    public string AiOperation { get; set; } = string.Empty;
    public string StructuredOutput { get; set; } = string.Empty;
    public string ApplicationToControl { get; set; } = string.Empty;
    public string ExpectedInteraction { get; set; } = string.Empty;
    public string WhyAiIsUseful { get; set; } = string.Empty;
    public string WhyControlIsEssential { get; set; } = string.Empty;
    /// <summary>Mandatory Label-replacement test answer (free text, "No" is required to pass).</summary>
    public string LabelReplacementAnswer { get; set; } = string.Empty;
    public string MinimalSampleWorkflow { get; set; } = string.Empty;
    public string ExpectedEffort { get; set; } = "3 to 5 hours";
}

/// <summary>UI Kit. A connected, reusable set of mobile screens.</summary>
public sealed class UiKitDetails : IdeaDetails
{
    public override string Kind => nameof(UiKitDetails);

    public string CustomerNeed { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<UiKitScreen> Screens { get; set; } = new();
    public string NavigationFlow { get; set; } = string.Empty;
    public string RelevantControls { get; set; } = string.Empty;
    public string RelevantFeatures { get; set; } = string.Empty;
    public int SuggestedScreenCount { get; set; }
    public string CustomerReuseValue { get; set; } = string.Empty;
    public string ExpectedEffort { get; set; } = "1 to 3 working days";
}

public sealed class UiKitScreen
{
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}

public static class IdeaDetailsCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string ToJson(IdeaDetails details) => JsonSerializer.Serialize(details, details.GetType(), Options);

    public static IdeaDetails? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        var kind = doc.RootElement.TryGetProperty("kind", out var kindEl) ? kindEl.GetString() : null;
        return kind switch
        {
            nameof(UseCaseWithoutAiDetails) => JsonSerializer.Deserialize<UseCaseWithoutAiDetails>(json, Options),
            nameof(UseCaseWithAiDetails) => JsonSerializer.Deserialize<UseCaseWithAiDetails>(json, Options),
            nameof(AiSolutionDetails) => JsonSerializer.Deserialize<AiSolutionDetails>(json, Options),
            nameof(UiKitDetails) => JsonSerializer.Deserialize<UiKitDetails>(json, Options),
            _ => null,
        };
    }
}
