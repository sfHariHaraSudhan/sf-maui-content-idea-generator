using IdeaGenerator.Core;
using IdeaGenerator.Core.Models;
using IdeaGenerator.Core.Services;
using Microsoft.Maui.Controls;

namespace IdeaGenerator.Maui.Views;

public partial class IdeaFormPage : ContentPage
{
    private readonly IdeaDataService _data;
    private readonly Guid? _editingId;

    private readonly string[] _contentTypes =
    {
        "Use Case Sample", "Use Case with AI", "Use Case without AI", "AI Solution", "UI Kit",
    };

    public IdeaFormPage(IdeaDataService data, Guid? editingId = null)
    {
        _data = data;
        _editingId = editingId;
        InitializeComponent();

        ContentTypePicker.ItemsSource = _contentTypes;
        CategoryPicker.ItemsSource = Categories.All;
        StatusPicker.ItemsSource = new[]
        {
            "Planned", "In progress", "Under review", "Completed", "Published",
        };
        StatusPicker.SelectedIndex = 0;
        PrimaryControlPicker.ItemsSource = new[] { "—" }.Concat(ControlCatalogue.ControlNames).ToArray();
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
            KeywordsEntry.Text = string.Join(", ", idea.Keywords);
            ReferenceUrlEntry.Text = idea.ReferenceUrl;
            PrimaryControlPicker.SelectedIndex = Math.Max(0, ControlNamesIndex(idea.PrimaryControl));
            SupportingControlsBox.Text = string.Join(", ", idea.SupportingControls);
            ControlFeaturesBox.Text = string.Join(", ", idea.ControlFeatures);
            WorkflowBox.Text = idea.WorkflowSummary;
            ScreenNamesBox.Text = idea.ScreenNames;
            AiOperationBox.Text = idea.AiOperation;
            PinnedToggle.IsToggled = idea.IsPinned;
            PinnedToggle.IsVisible = true;
            PinnedLabel.IsVisible = true;
            SaveButton.Text = "Save Changes";
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

        var names = ControlCatalogue.ControlNames;
        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            _ = DisplayAlert("Missing field", "A title is required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            _ = DisplayAlert("Missing field", "A short description is required.", "OK");
            return;
        }

        var (contentType, subtype) = (ContentTypePicker.SelectedIndex, ContentTypePicker.SelectedIndex) switch
        {
            (1, _) => (ContentType.UseCaseSample, UseCaseSubtype.WithAi),
            (2, _) => (ContentType.UseCaseSample, UseCaseSubtype.WithoutAi),
            (3, _) => (ContentType.AiSolution, null),
            (4, _) => (ContentType.UiKit, null),
            _ => (ContentType.UseCaseSample, null),
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
        idea.Category = CategoryPicker.Text ?? Categories.CommonOrGeneral;
        idea.Status = (IdeaStatus)StatusPicker.SelectedIndex;
        idea.TargetCustomer = NullIfBlank(TargetCustomerEntry.Text);
        idea.CustomerProblem = NullIfBlank(CustomerProblemBox.Text);
        idea.ShortDescription = DescriptionBox.Text.Trim();
        idea.Keywords = ParseList(KeywordsEntry.Text);
        idea.ReferenceUrl = NullIfBlank(ReferenceUrlEntry.Text);
        idea.PrimaryControl = ControlNames().ElementAtOrDefault(PrimaryControlPicker.SelectedIndex - 1);
        idea.SupportingControls = ParseList(SupportingControlsBox.Text);
        idea.ControlFeatures = ParseList(ControlFeaturesBox.Text);
        idea.WorkflowSummary = NullIfBlank(WorkflowBox.Text);
        idea.ScreenNames = ParseList(ScreenNamesEntry.Text);
        idea.AiOperation = NullIfBlank(AiOperationEntry.Text);

        if (_editingId.HasValue && _data.Get(_editingId.Value) is { } current)
        {
            // Preserve any detail payload from a previous phase; Phase 1 edits top-level fields.
            idea.Details = current.Details;
        }

        _data.Save(idea);
        if (PinnedToggle.IsVisible)
        {
            _data.SetPinned(idea.Id, PinnedToggle.IsToggled);
        }

        _ = Navigation.PopAsync();
    }

    private static IReadOnlyList<string> ControlNames() => ControlCatalogue.ControlNames;

    private static string? NullIfBlank(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static List<string> ParseList(string? text) => string.IsNullOrWhiteSpace(text)
        ? new List<string>()
        : text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
