using IdeaGenerator.Core;
using IdeaGenerator.Core.Models;
using IdeaGenerator.Core.Services;
using Microsoft.Maui.Controls;

namespace IdeaGenerator.Maui.Views;

/// <summary>
/// Shared Add/Edit form, shown as a modal (Navigation.PushModalAsync).
/// One form instance handles both create and edit.
/// </summary>
public partial class IdeaFormPage : ContentPage
{
    private readonly IdeaDataService _data;
    private readonly Guid? _editingId;
    private bool _initialized;

    private static readonly string[] ContentTypeOptions =
    {
        "Use Case Sample", "Use Case with AI", "Use Case without AI", "AI Solution", "UI Kit",
    };

    private static readonly string[] SubtypeOptions =
    {
        "Use Case without AI", "Use Case with AI",
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
        SubtypePicker.ItemsSource = SubtypeOptions;
        CategoryPicker.ItemsSource = Categories.All.ToList();
        StatusPicker.ItemsSource = StatusOptions;
        _primaryControlOptions = new[] { "—" }.Concat(ControlCatalogue.ControlNames).ToArray();
        PrimaryControlPicker.ItemsSource = _primaryControlOptions;
        ContentTypePicker.SelectedIndexChanged += OnContentTypeChanged;

        if (editingId is { } id && _data.Get(id) is { } idea)
        {
            Title = "Edit Idea";
            FormTitleLabel.Text = "Edit Idea";
            SaveButton.Text = "Save Changes";
            PopulateFrom(idea);
        }
        else
        {
            Title = "Add Idea";
            FormTitleLabel.Text = "Add Idea";
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Picker index values are applied once, after the page is fully
        // attached. Constructor-time SelectedIndex is unreliable for Picker
        // items populated after InitializeComponent on Mac Catalyst.
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (_editingId is { } id && _data.Get(id) is { } idea)
        {
            ApplyPickerSelections(idea);
        }
        else
        {
            StatusPicker.SelectedIndex = 0;
            PrimaryControlPicker.SelectedIndex = 0;
            SubtypePicker.SelectedIndex = 0;
            UpdateConditionalFields(0);
        }
    }

    private void ApplyPickerSelections(Idea idea)
    {
        UpdatePickerSelectionsFor(idea);
        UpdateConditionalFields(ContentTypePicker.SelectedIndex);
        UpdateSubtypeSelection(idea.ContentType, idea.UseCaseSubtype);
    }

    private void UpdatePickerSelectionsFor(Idea idea)
    {
        ContentTypePicker.SelectedIndex = IndexForContentType(idea.ContentType, idea.UseCaseSubtype);
        CategoryPicker.SelectedIndex = Math.Max(0, Categories.All.ToList().FindIndex(c => c == idea.Category));
        StatusPicker.SelectedIndex = (int)idea.Status;
        PrimaryControlPicker.SelectedIndex = Math.Max(0, ControlNamesIndex(idea.PrimaryControl));
    }

    private void PopulateFrom(Idea idea)
    {
        TitleEntry.Text = idea.Title;
        TargetCustomerEntry.Text = idea.TargetCustomer;
        CustomerProblemBox.Text = idea.CustomerProblem;
        DescriptionBox.Text = idea.ShortDescription;
        KeywordsEntry.Text = idea.Keywords;
        SupportingControlsBox.Text = idea.SupportingControls;
        ControlFeaturesBox.Text = idea.ControlFeatures;
        WorkflowBox.Text = idea.WorkflowSummary;
        ScreenNamesEntry.Text = idea.ScreenNames;
        AiOperationEntry.Text = idea.AiOperation;
        PinnedToggle.IsToggled = idea.IsPinned;
    }

    private void OnContentTypeChanged(object? sender, EventArgs e) =>
        UpdateConditionalFields(ContentTypePicker.SelectedIndex);

    private void UpdateConditionalFields(int contentTypeIndex)
    {
        // Use Case subtype only applies to Use Case Sample content type.
        SubtypeBlock.IsVisible = contentTypeIndex == 0;
        // AI operation only applies to AI content types.
        AiOperationBlock.IsVisible = contentTypeIndex is 1 or 3;
        // Screen names only apply to UI Kits.
        ScreenNamesBlock.IsVisible = contentTypeIndex == 4;
    }

    private void UpdateSubtypeSelection(ContentType contentType, UseCaseSubtype? subtype)
    {
        if (SubtypeBlock.IsVisible)
        {
            SubtypePicker.SelectedIndex = subtype == UseCaseSubtype.WithAi ? 1 : 0;
        }
    }

    private static int IndexForContentType(ContentType type, UseCaseSubtype? subtype) => (type, subtype) switch
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

        for (var i = 1; i < _primaryControlOptions.Length; i++)
        {
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

        // For a plain Use Case Sample, honor the explicit subtype picker.
        if (selection == 0)
        {
            subtype = SubtypePicker.SelectedIndex == 1 ? UseCaseSubtype.WithAi : UseCaseSubtype.WithoutAi;
        }

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
        idea.Category = (string?)CategoryPicker.SelectedItem ?? Categories.CommonOrGeneral;
        idea.Status = (IdeaStatus)Math.Clamp(StatusPicker.SelectedIndex, 0, StatusOptions.Length - 1);
        idea.TargetCustomer = NullIfBlank(TargetCustomerEntry.Text);
        idea.CustomerProblem = NullIfBlank(CustomerProblemBox.Text);
        idea.ShortDescription = DescriptionBox.Text.Trim();
        idea.Keywords = NullIfBlank(KeywordsEntry.Text);
        idea.PrimaryControl = PrimaryControlPicker.SelectedIndex > 0
            ? _primaryControlOptions[PrimaryControlPicker.SelectedIndex]
            : string.Empty;
        idea.SupportingControls = NullIfBlank(SupportingControlsBox.Text);
        idea.ControlFeatures = NullIfBlank(ControlFeaturesBox.Text);
        idea.WorkflowSummary = NullIfBlank(WorkflowBox.Text);
        idea.ScreenNames = NullIfBlank(ScreenNamesEntry.Text);
        idea.AiOperation = NullIfBlank(AiOperationEntry.Text);
        idea.IsPinned = PinnedToggle.IsToggled;

        _data.Save(idea);
        _ = Navigation.PopModalAsync();
    }

    private void OnCancelClicked(object? sender, EventArgs e) =>
        _ = Navigation.PopModalAsync();

    private static string NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
