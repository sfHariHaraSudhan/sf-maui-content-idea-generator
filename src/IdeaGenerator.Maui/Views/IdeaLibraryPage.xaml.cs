using System.Collections.ObjectModel;
using IdeaGenerator.Core;
using IdeaGenerator.Core.Models;
using IdeaGenerator.Core.Services;
using Microsoft.Maui.Controls;

namespace IdeaGenerator.Maui.Views;

public partial class IdeaLibraryPage : ContentPage
{
    private readonly IdeaDataService _data;
    private readonly ObservableCollection<IdeaRow> _rows = new();
    private bool _initialized;
    private int _totalCount;

    private static readonly string[] ContentTypeOptions =
    {
        "All", "AI Solution", "Use Case Sample",
        "Use Case with AI", "Use Case without AI", "UI Kit",
    };

    private static readonly string[] StatusOptions =
    {
        "All", "Planned", "In progress", "Under review", "Completed", "Published",
    };

    private readonly string[] _primaryControlOptions;

    public IdeaLibraryPage()
        : this(AppDataService.Current)
    {
    }

    public IdeaLibraryPage(IdeaDataService data)
    {
        _data = data;
        InitializeComponent();

        _primaryControlOptions = new[] { "All" }.Concat(ControlCatalogue.ControlNames).ToArray();
        ContentTypeFilter.ItemsSource = ContentTypeOptions;
        CategoryFilter.ItemsSource = Categories.All.ToList();
        StatusFilter.ItemsSource = StatusOptions;
        PrimaryControlFilter.ItemsSource = _primaryControlOptions;
        IdeasList.ItemsSource = _rows;

        VersionLabel.Text = $"v{AppInfo.Version} · DB schema v{_data.SchemaVersion} · catalog v{ControlCatalogue.CatalogVersion}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Apply filter defaults once, after the page is fully attached.
        // Constructor-time SelectedIndex is unreliable for Picker items
        // populated after InitializeComponent on Mac Catalyst; the native
        // control can surface a non-"All" value on first launch, which
        // silently hid the seeded ideas.
        if (!_initialized)
        {
            _initialized = true;
            ApplyUnfilteredDefaults();
        }

        Refresh();
    }

    private void ApplyUnfilteredDefaults()
    {
        ContentTypeFilter.SelectedItem = null;
        CategoryFilter.SelectedItem = null;
        StatusFilter.SelectedItem = null;
        PrimaryControlFilter.SelectedItem = null;
        PinnedOnlyToggle.IsToggled = false;
        SearchBox.Text = string.Empty;
        UpdateClearFiltersVisibility();
    }

    private void Refresh()
    {
        LoadingOverlay.IsVisible = true;
        FailureOverlay.IsVisible = false;
        IdeasList.IsVisible = false;

        try
        {
            var filter = BuildFilter();
            _totalCount = _data.Count();
            var ideas = _data.GetAll(filter);

            _rows.Clear();
            foreach (var idea in ideas)
            {
                _rows.Add(IdeaRow.From(idea));
            }

            UpdateResultCount();
            UpdateEmptyState(filter);
            IdeasList.IsVisible = true;
        }
        catch (Exception ex)
        {
            IdeasList.IsVisible = false;
            FailureMessage.Text = $"The Idea Library could not be loaded. {ex.Message}";
            FailureOverlay.IsVisible = true;
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private void UpdateResultCount()
    {
        var showing = _rows.Count;
        ResultCountLabel.Text = filterActive()
            ? $"{showing} of {_totalCount} idea{(showing == 1 ? "" : "s")}"
            : $"{_totalCount} idea{(_totalCount == 1 ? "" : "s")}";

        bool filterActive() =>
            !string.IsNullOrWhiteSpace(SearchBox.Text) ||
            !IsUnselected(ContentTypeFilter) ||
            !IsUnselected(CategoryFilter) ||
            !IsUnselected(StatusFilter) ||
            !IsUnselected(PrimaryControlFilter) ||
            PinnedOnlyToggle.IsToggled;

        UpdateClearFiltersVisibility();
    }

    private void UpdateEmptyState(IdeaFilter filter)
    {
        if (_rows.Count > 0)
        {
            return;
        }

        if (filter.HasFilters)
        {
            EmptyIcon.Text = "🔍";
            EmptyMessage.Text = "No ideas match the current search or filters. Adjust them or tap Clear Filters.";
        }
        else if (_totalCount == 0)
        {
            EmptyIcon.Text = "🗂";
            EmptyMessage.Text = "The Idea Library is empty. Tap “Add Idea” to create the first one.";
        }
        else
        {
            EmptyIcon.Text = "🔍";
            EmptyMessage.Text = "No ideas match the current search or filters.";
        }
    }

    private static bool IsUnselected(Picker picker) =>
        picker.SelectedIndex < 0 || picker.SelectedItem is null || "All".Equals(picker.SelectedItem as string, StringComparison.Ordinal);

    private IdeaFilter BuildFilter()
    {
        // Null or unselected Picker values are always treated as "All".
        var category = (string?)CategoryFilter.SelectedItem;
        var primaryControl = (string?)PrimaryControlFilter.SelectedItem;
        var contentTypeText = (string?)ContentTypeFilter.SelectedItem;
        var statusText = (string?)StatusFilter.SelectedItem;

        return new IdeaFilter
        {
            Search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim(),
            Category = "All".Equals(category, StringComparison.Ordinal) ? null : category,
            PrimaryControl = "All".Equals(primaryControl, StringComparison.Ordinal) ? null : primaryControl,
            ContentType = contentTypeText switch
            {
                "AI Solution" => ContentType.AiSolution,
                "Use Case Sample" => ContentType.UseCaseSample,
                "Use Case with AI" => ContentType.UseCaseSample,
                "Use Case without AI" => ContentType.UseCaseSample,
                "UI Kit" => ContentType.UiKit,
                _ => null,
            },
            UseCaseSubtype = contentTypeText switch
            {
                "Use Case with AI" => UseCaseSubtype.WithAi,
                "Use Case without AI" => UseCaseSubtype.WithoutAi,
                _ => null,
            },
            Status = statusText switch
            {
                "Planned" => IdeaStatus.Planned,
                "In progress" => IdeaStatus.InProgress,
                "Under review" => IdeaStatus.UnderReview,
                "Completed" => IdeaStatus.Completed,
                "Published" => IdeaStatus.Published,
                _ => null,
            },
            PinnedOnly = PinnedOnlyToggle.IsToggled ? true : null,
        };
    }

    private void UpdateClearFiltersVisibility()
    {
        ClearFiltersButton.IsVisible =
            !string.IsNullOrWhiteSpace(SearchBox.Text) ||
            !IsUnselected(ContentTypeFilter) ||
            !IsUnselected(CategoryFilter) ||
            !IsUnselected(StatusFilter) ||
            !IsUnselected(PrimaryControlFilter) ||
            PinnedOnlyToggle.IsToggled;
    }

    private void OnSearchPressed(object? sender, EventArgs e) => Refresh();

    private void OnFilterChanged(object? sender, EventArgs e) => Refresh();

    private void OnClearFiltersClicked(object? sender, EventArgs e)
    {
        // Setting SelectedItem = null while the handler is attached fires
        // SelectedIndexChanged again for each Picker; that is harmless
        // because the subsequent Refresh runs with the cleared state.
        SearchBox.Text = string.Empty;
        ApplyUnfilteredDefaults();
        Refresh();
    }

    private void OnRetryClicked(object? sender, EventArgs e) => Refresh();

    private void OnAddClicked(object? sender, EventArgs e)
    {
        _ = Navigation.PushModalAsync(new IdeaFormPage(_data));
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not IdeaRow row)
        {
            return;
        }

        IdeasList.SelectedItem = null;
        OpenDetail(row.Id);
    }

    private void OpenDetail(Guid id)
    {
        if (_data.Get(id) is { } idea)
        {
            var page = new IdeaDetailPage(_data, idea);
            _ = Navigation.PushAsync(page);
        }
    }

    private void OnViewMenuTapped(object? sender, EventArgs e)
    {
        if (TryGetMenuIdeaId(sender, out var id))
        {
            OpenDetail(id);
        }
    }

    private void OnPinMenuTapped(object? sender, EventArgs e)
    {
        if (!TryGetMenuIdeaId(sender, out var id))
        {
            return;
        }

        var current = _data.Get(id)?.IsPinned ?? false;
        _data.SetPinned(id, !current);
        Refresh();
    }

    private void OnEditMenuTapped(object? sender, EventArgs e)
    {
        if (TryGetMenuIdeaId(sender, out var id))
        {
            _ = Navigation.PushModalAsync(new IdeaFormPage(_data, id));
        }
    }

    private async void OnDeleteMenuTapped(object? sender, EventArgs e)
    {
        if (!TryGetMenuIdeaId(sender, out var id))
        {
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Delete idea",
            "Delete this idea? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        _data.Delete(id);
        Refresh();
    }

    private static bool TryGetMenuIdeaId(object? sender, out Guid id)
    {
        id = Guid.Empty;
        return (sender as MenuItem)?.BindingContext is IdeaRow row && Guid.TryParse(row.Id.ToString(), out id);
    }
}
