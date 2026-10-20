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
        AppendMeta(sb, "Created", _idea.CreatedAt.ToString("yyyy-MM-dd"));
        AppendMeta(sb, "Primary control", _idea.PrimaryControl);
        AppendMeta(sb, "Supporting controls", _idea.SupportingControls);
        AppendMeta(sb, "Control features", _idea.ControlFeatures);

        AppendBlock(sb, "Target customer", _idea.TargetCustomer);
        AppendBlock(sb, "Customer problem", _idea.CustomerProblem);
        AppendBlock(sb, "Description", _idea.ShortDescription);
        AppendBlock(sb, "Workflow summary", _idea.WorkflowSummary);
        AppendBlock(sb, "Screens", _idea.ScreenNames);
        AppendBlock(sb, "AI operation", _idea.AiOperation);
        AppendBlock(sb, "Keywords", _idea.Keywords);

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
                AppendBlock(sb, "How the output drives the controls", d.HowOutputDrivesControls);
                AppendBlock(sb, "User review and correction", d.UserReviewAndCorrection);
                AppendBlock(sb, "Invalid-response handling", d.InvalidResponseHandling);
                AppendBlock(sb, "Retry or fallback", d.RetryOrFallback);
                AppendBlock(sb, "Why AI adds customer value", d.WhyAiAddsCustomerValue);
                AppendBlock(sb, "Why the controls remain essential", d.WhyControlsRemainEssential);
                AppendBlock(sb, "Expected effort", d.ExpectedEffort);
                break;
            case AiSolutionDetails d:
                AppendBlock(sb, "Customer input", d.CustomerInput);
                AppendBlock(sb, "AI operation", d.AiOperation);
                AppendBlock(sb, "Structured AI output", d.StructuredOutput);
                AppendBlock(sb, "How the output is applied in the control", d.ApplicationToControl);
                AppendBlock(sb, "Expected interaction", d.ExpectedInteraction);
                AppendBlock(sb, "Why AI is useful", d.WhyAiIsUseful);
                AppendBlock(sb, "Why the control is essential", d.WhyControlIsEssential);
                AppendBlock(sb, "Minimal sample workflow", d.MinimalSampleWorkflow);
                AppendBlock(sb, "Expected effort", d.ExpectedEffort);
                break;
            case UiKitDetails d:
                AppendBlock(sb, "Customer need", d.CustomerNeed);
                if (d.Screens.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Screens**");
                    sb.AppendLine();
                    foreach (var screen in d.Screens)
                    {
                        sb.AppendLine($"  • {screen.Name} — {screen.Purpose}");
                    }
                }
                AppendBlock(sb, "Navigation flow", d.NavigationFlow);
                AppendBlock(sb, "Customer reuse value", d.CustomerReuseValue);
                AppendBlock(sb, "Suggested screen count", d.SuggestedScreenCount.ToString());
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
        await Clipboard.SetTextAsync(Body.Text);
        await DisplayAlertAsync("Copied", "Idea details copied to clipboard as Markdown.", "OK");
    }

    private void OnPinToggleClicked(object? sender, EventArgs e)
    {
        _idea.IsPinned = !_idea.IsPinned;
        _data.SetPinned(_idea.Id, _idea.IsPinned);
        PinButton.Text = _idea.IsPinned ? "Unpin" : "Pin";
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new IdeaFormPage(_data, _idea.Id));
    }
}
