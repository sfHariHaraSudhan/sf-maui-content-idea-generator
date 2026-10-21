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

    private readonly string[] _categoryOptions;
    private readonly string[] _primaryControlOptions;

    // Explicit filter state. "All" is a real, selectable value and means
    // no filter. This is independent of any Picker state (there are no
    // Pickers in the library), so first launch is always unfiltered.
    private string _contentType = "All";
    private string _category = "All";
    private string _status = "All";
    private string _primaryControl = "All";

    public IdeaLibraryPage()
        : this(AppDataService.Current)
    {
    }

    public IdeaLibraryPage(IdeaDataService data)
    {
        _data = data;
        InitializeComponent();

        _categoryOptions = new[] { "All" }.Concat(Categories.All).ToArray();
        _primaryControlOptions = new[] { "All" }.Concat(ControlCatalogue.ControlNames).ToArray();
        IdeasList.ItemsSource = _rows;

        VersionLabel.Text = $"v{AppInfo.Version} · DB schema v{_data.SchemaVersion} · catalog v{ControlCatalogue.CatalogVersion}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
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
            UpdateClearFiltersVisibility();
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
        ResultCountLabel.Text = AnyFilterActive()
            ? $"{showing} of {_totalCount} idea{(showing == 1 ? "" : "s")}"
            : $"{_totalCount} idea{(_totalCount == 1 ? "" : "s")}";
    }

    // Search + the four dropdown filters + Pinned-only all reduce the list,
    // so all of them make the count show "N of total".
    private bool AnyFilterActive() =>
        !string.IsNullOrWhiteSpace(SearchBox.Text) ||
        _contentType != "All" ||
        _category != "All" ||
        _status != "All" ||
        _primaryControl != "All" ||
        PinnedOnlyToggle.IsToggled;

    // Clear Filters represents the search + dropdown filters only.
    // Pinned-only is independent and must not trigger or be cleared by it.
    private bool ShouldShowClearFilters() =>
        !string.IsNullOrWhiteSpace(SearchBox.Text) ||
        _contentType != "All" ||
        _category != "All" ||
        _status != "All" ||
        _primaryControl != "All";

    private void UpdateClearFiltersVisibility() =>
        ClearFiltersButton.IsVisible = ShouldShowClearFilters();

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

    private IdeaFilter BuildFilter()
    {
        return new IdeaFilter
        {
            Search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim(),
            Category = _category == "All" ? null : _category,
            PrimaryControl = _primaryControl == "All" ? null : _primaryControl,
            ContentType = _contentType switch
            {
                "AI Solution" => ContentType.AiSolution,
                "Use Case Sample" => ContentType.UseCaseSample,
                "Use Case with AI" => ContentType.UseCaseSample,
                "Use Case without AI" => ContentType.UseCaseSample,
                "UI Kit" => ContentType.UiKit,
                _ => null,
            },
            UseCaseSubtype = _contentType switch
            {
                "Use Case with AI" => UseCaseSubtype.WithAi,
                "Use Case without AI" => UseCaseSubtype.WithoutAi,
                _ => null,
            },
            Status = _status switch
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

    // ─────────────────────────── filter buttons ───────────────────────────

    private async Task<string?> PickFilterValue(string title, string[] options, string current)
    {
        var selector = new FilterSelectionPage(title, options, current);
        await Navigation.PushModalAsync(selector);
        return selector.SelectedValue;
    }

    private async void OnContentTypeFilterClicked(object? sender, EventArgs e)
    {
        _contentType = await PickFilterValue("Content type", ContentTypeOptions, _contentType) ?? _contentType;
        ContentTypeFilterButton.Text = _contentType;
        Refresh();
    }

    private async void OnCategoryFilterClicked(object? sender, EventArgs e)
    {
        _category = await PickFilterValue("Category", _categoryOptions, _category) ?? _category;
        CategoryFilterButton.Text = _category;
        Refresh();
    }

    private async void OnStatusFilterClicked(object? sender, EventArgs e)
    {
        _status = await PickFilterValue("Status", StatusOptions, _status) ?? _status;
        StatusFilterButton.Text = _status;
        Refresh();
    }

    private async void OnPrimaryControlFilterClicked(object? sender, EventArgs e)
    {
        _primaryControl = await PickFilterValue("Primary control", _primaryControlOptions, _primaryControl) ?? _primaryControl;
        PrimaryControlFilterButton.Text = _primaryControl;
        Refresh();
    }

    private void OnPinnedOnlyToggled(object? sender, ToggledEventArgs e) => Refresh();

    private void OnClearFiltersClicked(object? sender, EventArgs e)
    {
        // Clears search + the four dropdown filters only.
        // Pinned-only keeps its current state.
        SearchBox.Text = string.Empty;
        ClearSearchButton.IsVisible = false;
        _contentType = "All";
        _category = "All";
        _status = "All";
        _primaryControl = "All";
        ContentTypeFilterButton.Text = "All";
        CategoryFilterButton.Text = "All";
        StatusFilterButton.Text = "All";
        PrimaryControlFilterButton.Text = "All";
        Refresh();
    }

    // ─────────────────────────── search ───────────────────────────

    private void OnSearchPressed(object? sender, EventArgs e)
    {
        ClearSearchButton.IsVisible = !string.IsNullOrWhiteSpace(SearchBox.Text);
        Refresh();
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        SearchBox.Text = string.Empty;
        ClearSearchButton.IsVisible = false;
        Refresh();
    }

    private void OnRetryClicked(object? sender, EventArgs e) => Refresh();

    // ─────────────────────────── add / edit / detail ───────────────────────────

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

        return sender is BindableObject bindableObject
            && bindableObject.BindingContext is IdeaRow row
            && Guid.TryParse(row.Id.ToString(), out id);
    }
}
