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
        };
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new IdeaFormPage(_data));
    }

    private void OnSearchCompleted(object? sender, EventArgs e) => Refresh();

    private void OnFilterChanged(object? sender, EventArgs e) => Refresh();

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
            _ = Navigation.PushAsync(new IdeaFormPage(_data, id));
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
