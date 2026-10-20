using System.Text;
using IdeaGenerator.Core.Models;
using IdeaGenerator.Core.Services;
using Microsoft.Maui.Controls;

namespace IdeaGenerator.Maui.Views;

public partial class IdeaDetailPage : ContentPage
{
    private readonly IdeaDataService _data;
    private readonly Idea _idea;

    public IdeaDetailPage(IdeaDataService data, Idea idea)
    {
        _data = data;
        _idea = idea;
        InitializeComponent();
        Populate();
    }

    private void Populate()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {_idea.Title}");
        sb.AppendLine();
        AppendMeta(sb, "Content type", _idea.ContentType.DisplayName(_idea.UseCaseSubtype));
        AppendMeta(sb, "Category", _idea.Category);
        AppendMeta(sb, "Status", _idea.Status.DisplayName());
        AppendMeta(sb, "Source", _idea.Source.ToString());
        if (_idea.CreatedAt is { } created)
        {
            AppendMeta(sb, "Created", created.ToString("yyyy-MM-dd"));
        }

        AppendMeta(sb, "Primary control", _idea.PrimaryControl);
        if (_idea.SupportingControls.Count > 0)
        {
            AppendMeta(sb, "Supporting controls", string.Join(", ", _idea.SupportingControls));
        }
        if (_idea.ControlFeatures.Count > 0)
        {
            AppendMeta(sb, "Control features", string.Join(", ", _idea.ControlFeatures));
        }

        AppendBlock(sb, "Target customer", _idea.TargetCustomer);
        AppendBlock(sb, "Customer problem", _idea.CustomerProblem);
        AppendBlock(sb, "Description", _idea.ShortDescription);
        AppendBlock(sb, "Workflow summary", _idea.WorkflowSummary);
        if (_idea.ScreenNames.Count > 0)
        {
            AppendBlock(sb, "Screens", string.Join(" → ", _idea.ScreenNames));
        }
        AppendBlock(sb, "AI operation", _idea.AiOperation);
        if (_idea.Keywords.Count > 0)
        {
            AppendBlock(sb, "Keywords", string.Join(", ", _idea.Keywords));
        }
        AppendBlock(sb, "Reference URL", _idea.ReferenceUrl);

        switch (_idea.Details)
        {
            case UseCaseWithoutAiDetails d:
                AppendBlock(sb, "Desired outcome", d.DesiredOutcome);
                AppendBlock(sb, "Practical workflow", d.PracticalWorkflow);
                AppendBlock(sb, "Customer benefit", d.CustomerBenefit);
                AppendBlock(sb, "Why the controls are essential", d.WhyControlsAreEssential);
                AppendBlock(sb, "Implementation scope", d.ImplementationScope);
                AppendBlock(sb, "Expected effort", d.ExpectedEffort);
                break;
            case UseCaseWithAiDetails d:
                AppendBlock(sb, "Desired outcome", d.DesiredOutcome);
                AppendBlock(sb, "Practical workflow", d.PracticalWorkflow);
                AppendBlock(sb, "Customer input", d.CustomerInput);
                AppendBlock(sb, "AI operation", d.AiOperation);
                AppendBlock(sb, "Structured AI output", d.StructuredAiOutput);
                AppendBlock(sb, "How the output operates the control", d.HowOutputOperatesControl);
                AppendBlock(sb, "User review and correction", d.UserReviewAndCorrection);
                AppendBlock(sb, "Invalid-response handling", d.InvalidResponseHandling);
                AppendBlock(sb, "Retry or fallback", d.RetryOrFallback);
                AppendBlock(sb, "Why AI adds customer value", d.WhyAiAddsCustomerValue);
                AppendBlock(sb, "Why the controls remain essential", d.WhyControlsRemainEssential);
                AppendBlock(sb, "Implementation scope", d.ImplementationScope);
                AppendBlock(sb, "Expected effort", d.ExpectedEffort);
                break;
            case AiSolutionDetails d:
                AppendBlock(sb, "Customer input", d.CustomerInput);
                AppendBlock(sb, "AI operation", d.AiOperation);
                AppendBlock(sb, "Structured AI output", d.StructuredAiOutput);
                AppendBlock(sb, "How the output is applied in the control", d.HowOutputIsApplied);
                AppendBlock(sb, "Why AI is useful", d.WhyAiIsUseful);
                AppendBlock(sb, "Why the control is essential", d.WhyControlIsEssential);
                AppendBlock(sb, "Minimal workflow", d.MinimalWorkflow);
                AppendBlock(sb, "Implementation scope", d.ImplementationScope);
                AppendBlock(sb, "Expected effort", d.ExpectedEffort);
                break;
            case UiKitDetails d:
                AppendBlock(sb, "Customer need", d.CustomerNeed);
                sb.AppendLine();
                sb.AppendLine("Screens:");
                foreach (var screen in d.Screens)
                {
                    sb.AppendLine($"  • {screen.Name} — {screen.Purpose}");
                }
                AppendBlock(sb, "Navigation flow", d.NavigationFlow);
                AppendBlock(sb, "Customer reuse value", d.CustomerReuseValue);
                AppendBlock(sb, "Suggested screen count", d.SuggestedScreenCount.ToString());
                AppendBlock(sb, "Implementation scope", d.ImplementationScope);
                AppendBlock(sb, "Expected effort", d.ExpectedEffort);
                break;
        }

        Body.Text = sb.ToString().TrimEnd();
        PinButton.Text = _idea.IsPinned ? "Unpin" : "Pin";
    }

    private static void AppendMeta(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        sb.AppendLine($"- **{label}:** {value}");
    }

    private static void AppendBlock(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine($"**{label}**");
        sb.AppendLine();
        sb.AppendLine(value);
    }

    private async void OnCopyMarkdownClicked(object? sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(Body.Text, CancellationToken.None);
        await DisplayAlert("Copied", "Idea details copied to clipboard as Markdown.", "OK");
    }

    private void OnPinToggleClicked(object? sender, EventArgs e)
    {
        _idea.IsPinned = !_idea.IsPinned;
        _data.SetPinned(_idea.Id, _idea.IsPinned);
        PinButton.Text = _idea.IsPinned ? "Unpin" : "Pin";
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new IdeaFormPage(_data, _idea.Id));
    }
}
