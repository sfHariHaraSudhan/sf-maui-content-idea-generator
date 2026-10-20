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

    public IdeaLibraryPage()
        : this(AppDataService.Current)
    {
    }

    public IdeaLibraryPage(IdeaDataService data)
    {
        _data = data;
        InitializeComponent();

        ContentTypeFilter.ItemsSource = new[]
        {
            "All", "AI Solution", "Use Case Sample",
            "Use Case with AI", "Use Case without AI", "UI Kit",
        };
        CategoryFilter.ItemsSource = Categories.All;
        StatusFilter.ItemsSource = new[]
        {
            "All", "Planned", "In progress", "Under review", "Completed", "Published",
        };
        PrimaryControlFilter.ItemsSource = new[] { "All" }
            .Concat(ControlCatalogue.ControlNames).ToArray();

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
        var filter = BuildFilter();
        var ideas = _data.GetAll(filter);

        _rows.Clear();
        foreach (var idea in ideas)
        {
            _rows.Add(IdeaRow.From(idea));
        }

        EmptyMessage.Text = filter.HasFilters
            ? "No ideas match the current filters."
            : "The Idea Library is empty. Tap “Add Idea” to create the first one.";
    }

    private IdeaFilter BuildFilter()
    {
        var filter = new IdeaFilter
        {
            Search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim(),
            Category = "All".Equals(CategoryFilter.Text, StringComparison.Ordinal) ? null : CategoryFilter.Text,
            PrimaryControl = "All".Equals(PrimaryControlFilter.Text, StringComparison.Ordinal) ? null : PrimaryControlFilter.Text,
        };

        filter.ContentType = ContentTypeFilter.Text switch
        {
            "AI Solution" => ContentType.AiSolution,
            "Use Case Sample" => ContentType.UseCaseSample,
            "Use Case with AI" => ContentType.UseCaseSample,
            "Use Case without AI" => ContentType.UseCaseSample,
            "UI Kit" => ContentType.UiKit,
            _ => null,
        };

        filter.UseCaseSubtype = ContentTypeFilter.Text switch
        {
            "Use Case with AI" => UseCaseSubtype.WithAi,
            "Use Case without AI" => UseCaseSubtype.WithoutAi,
            _ => null,
        };

        filter.Status = StatusFilter.Text switch
        {
            "Planned" => IdeaStatus.Planned,
            "In progress" => IdeaStatus.InProgress,
            "Under review" => IdeaStatus.UnderReview,
            "Completed" => IdeaStatus.Completed,
            "Published" => IdeaStatus.Published,
            _ => null,
        };

        return filter;
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new IdeaFormPage(_data));
    }

    private void OnSearchCompleted(object? sender, EventArgs e) => Refresh();

    private void OnFilterChanged(object? sender, ItemTappedEventArgs e) => Refresh();

    private void OnRowTapped(object? sender, ItemTappedEventArgs e)
    {
        if (e.Item is not IdeaRow row)
        {
            return;
        }

        IdeasList.SelectedItem = null;
        OpenDetail(row.Id);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Tapping handled via ItemTapped; reset selection so re-tapping works.
        IdeasList.SelectedItem = null;
    }

    private void OpenDetail(Guid id)
    {
        if (_data.Get(id) is { } idea)
        {
            var page = new IdeaDetailPage(_data, idea);
            _ = Navigation.PushAsync(page);
        }
    }

    private void OnViewMenuTapped(object? sender, MenuItemTappedEventArgs e)
    {
        if (e.Value is Guid id)
        {
            OpenDetail(id);
        }
    }

    private void OnPinMenuTapped(object? sender, MenuItemTappedEventArgs e)
    {
        if (e.Value is not Guid id)
        {
            return;
        }

        var current = _data.Get(id)?.IsPinned ?? false;
        _data.SetPinned(id, !current);
        Refresh();
    }

    private void OnEditMenuTapped(object? sender, MenuItemTappedEventArgs e)
    {
        if (e.Value is Guid id)
        {
            _ = Navigation.PushAsync(new IdeaFormPage(_data, id));
        }
    }

    private async void OnDeleteMenuTapped(object? sender, MenuItemTappedEventArgs e)
    {
        if (e.Value is not Guid id)
        {
            return;
        }

        var confirm = await DisplayAlert("Delete idea", "Delete this idea? This cannot be undone.", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        _data.Delete(id);
        Refresh();
    }
}
