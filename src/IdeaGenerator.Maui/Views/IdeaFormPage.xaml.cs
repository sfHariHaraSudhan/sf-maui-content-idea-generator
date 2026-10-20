using IdeaGenerator.Core;
using IdeaGenerator.Core.Models;
using IdeaGenerator.Core.Services;
using Microsoft.Maui.Controls;

namespace IdeaGenerator.Maui.Views;

public partial class IdeaFormPage : ContentPage
{
    private readonly IdeaDataService _data;
    private readonly Guid? _editingId;

    private static readonly string[] ContentTypeOptions =
    {
        "Use Case Sample", "Use Case with AI", "Use Case without AI", "AI Solution", "UI Kit",
    };

    private static readonly string[] StatusOptions =
    {
        "Planned", "In progress", "Under review", "Completed", "Published",
    };

    private readonly string[] _primaryControlOptions;

    public IdeaFormPage(IdeaDataService data, Guid? editingId = null)
    {
        _data = data;
        _editingId = editingId;
        InitializeComponent();

        ContentTypePicker.ItemsSource = ContentTypeOptions;
        CategoryPicker.ItemsSource = Categories.All.ToList();
        StatusPicker.ItemsSource = StatusOptions;
        StatusPicker.SelectedIndex = 0;
        _primaryControlOptions = new[] { "—" }.Concat(ControlCatalogue.ControlNames).ToArray();
        PrimaryControlPicker.ItemsSource = _primaryControlOptions;
        PrimaryControlPicker.SelectedIndex = 0;

        if (editingId is { } id && _data.Get(id) is { } idea)
        {
            Title = "Edit Idea";
            TitleEntry.Text = idea.Title;
            ContentTypePicker.SelectedIndex = IndexForContentType(idea.ContentType, idea.UseCaseSubtype);
            CategoryPicker.SelectedIndex = Math.Max(0, Categories.All.ToList().FindIndex(c => c == idea.Category));
            StatusPicker.SelectedIndex = (int)idea.Status;
            TargetCustomerEntry.Text = idea.TargetCustomer;
            CustomerProblemBox.Text = idea.CustomerProblem;
            DescriptionBox.Text = idea.ShortDescription;
            KeywordsEntry.Text = idea.Keywords;
            PrimaryControlPicker.SelectedIndex = Math.Max(0, ControlNamesIndex(idea.PrimaryControl));
            SupportingControlsBox.Text = idea.SupportingControls;
            ControlFeaturesBox.Text = idea.ControlFeatures;
            WorkflowBox.Text = idea.WorkflowSummary;
            ScreenNamesEntry.Text = idea.ScreenNames;
            AiOperationEntry.Text = idea.AiOperation;
            PinnedToggle.IsToggled = idea.IsPinned;
            PinnedToggle.IsVisible = true;
            PinnedLabel.IsVisible = true;
        }
        else
        {
            Title = "Add Idea";
            PinnedToggle.IsVisible = false;
            PinnedLabel.IsVisible = false;
            ContentTypePicker.SelectedIndex = 0;
            CategoryPicker.SelectedIndex = 0;
        }
    }

    private int IndexForContentType(ContentType type, UseCaseSubtype? subtype) => (type, subtype) switch
    {
        (ContentType.UseCaseSample, UseCaseSubtype.WithAi) => 1,
        (ContentType.UseCaseSample, UseCaseSubtype.WithoutAi) => 2,
        (ContentType.UseCaseSample, _) => 0,
        (ContentType.AiSolution, _) => 3,
        (ContentType.UiKit, _) => 4,
        _ => 0,
    };

    private int ControlNamesIndex(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 0;
        }

        for (var i = 0; i < _primaryControlOptions.Length; i++)
        {
            if (i == 0)
            {
                continue;
            }

            if (string.Equals(_primaryControlOptions[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            _ = DisplayAlertAsync("Missing field", "A title is required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            _ = DisplayAlertAsync("Missing field", "A short description is required.", "OK");
            return;
        }

        var selection = ContentTypePicker.SelectedIndex;
        (ContentType contentType, UseCaseSubtype? subtype) = selection switch
        {
            1 => (ContentType.UseCaseSample, UseCaseSubtype.WithAi),
            2 => (ContentType.UseCaseSample, UseCaseSubtype.WithoutAi),
            3 => (ContentType.AiSolution, (UseCaseSubtype?)null),
            4 => (ContentType.UiKit, (UseCaseSubtype?)null),
            _ => (ContentType.UseCaseSample, (UseCaseSubtype?)null),
        };

        Idea idea;
        if (_editingId is { } id && _data.Get(id) is { } existing)
        {
            idea = existing;
        }
        else
        {
            idea = new Idea
            {
                Source = IdeaSource.UserAdded,
            };
        }

        idea.Title = TitleEntry.Text.Trim();
        idea.ContentType = contentType;
        idea.UseCaseSubtype = subtype;
        idea.Category = (CategoryPicker.SelectedItem as string) ?? Categories.CommonOrGeneral;
        idea.Status = (IdeaStatus)StatusPicker.SelectedIndex;
        idea.TargetCustomer = NullIfBlank(TargetCustomerEntry.Text);
        idea.CustomerProblem = NullIfBlank(CustomerProblemBox.Text);
        idea.ShortDescription = DescriptionBox.Text.Trim();
        idea.Keywords = NullIfBlank(KeywordsEntry.Text);
        idea.PrimaryControl = PrimaryControlPicker.SelectedIndex > 0 ? _primaryControlOptions[PrimaryControlPicker.SelectedIndex] : string.Empty;
        idea.SupportingControls = NullIfBlank(SupportingControlsBox.Text);
        idea.ControlFeatures = NullIfBlank(ControlFeaturesBox.Text);
        idea.WorkflowSummary = NullIfBlank(WorkflowBox.Text);
        idea.ScreenNames = NullIfBlank(ScreenNamesEntry.Text);
        idea.AiOperation = NullIfBlank(AiOperationEntry.Text);

        _data.Save(idea);
        if (PinnedToggle.IsVisible)
        {
            _data.SetPinned(idea.Id, PinnedToggle.IsToggled);
        }

        _ = Navigation.PopAsync();
    }

    private static string NullIfBlank(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? string.Empty : trimmed;
    }
}
