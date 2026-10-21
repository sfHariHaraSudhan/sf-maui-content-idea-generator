using Microsoft.Maui.Controls;

namespace IdeaGenerator.Maui.Views;

/// <summary>
/// Compact modal option selector used by the four Idea Library filters.
/// Presented with Navigation.PushModalAsync; the caller reads
/// <see cref="SelectedValue"/> after the modal pops.
/// </summary>
public partial class FilterSelectionPage : ContentPage
{
    // Mirrors the App.xaml light palette so this modal renders correctly
    // without depending on resource-dictionary lookup timing.
    private static readonly Color PrimaryDark = Color.FromHex("#3730A3");
    private static readonly Color PrimarySoft = Color.FromHex("#EEF2FF");
    private static readonly Color TextPrimary = Color.FromHex("#0F172A");

    /// <summary>Value chosen by the user; null when cancelled or closed without a choice.</summary>
    public string? SelectedValue { get; private set; }

    public FilterSelectionPage(string title, IReadOnlyList<string> options, string? current)
    {
        Title = title;
        InitializeComponent();
        TitleLabel.Text = title;

        foreach (var option in options)
        {
            var isActive = string.Equals(option, current, StringComparison.Ordinal);

            var optionButton = new Button
            {
                Text = option,
                FontSize = 14,
                TextColor = isActive ? PrimaryDark : TextPrimary,
                BackgroundColor = isActive ? PrimarySoft : Colors.Transparent,
                CornerRadius = 10,
                Padding = new Thickness(14, 10, 14, 10),
                MinimumHeightRequest = 40,
            };
            optionButton.Clicked += (sender, args) =>
            {
                if (sender is Button b)
                {
                    SelectedValue = b.Text;
                }

                _ = Navigation.PopModalAsync();
            };

            OptionsList.Children.Add(optionButton);
        }
    }

    private void OnCancelClicked(object? sender, EventArgs e) =>
        _ = Navigation.PopModalAsync();
}
