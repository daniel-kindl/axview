using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>One row of the file list: the kind icon, the title, and the folder.</summary>
/// <remarks>The row has an accent bar at the left edge. <see cref="CreateSelectionStyles"/> shows it for the selected row.</remarks>
internal sealed class FileRow : DockPanel
{
    private const string SelectionBarClass = "axview-selection-bar";
    private const double SelectionBarWidth = 3;

    /// <summary>Shows the entry.</summary>
    /// <param name="entry">The entry of the file list.</param>
    public FileRow(FileListEntry entry)
    {
        Border selectionBar = new()
        {
            Width = SelectionBarWidth,
            Margin = new Thickness(0, 0, ChromeStyle.SpacingMedium, 0),
            CornerRadius = new CornerRadius(SelectionBarWidth / 2),
            Classes = { SelectionBarClass },
        };
        ChromeStyle.SetBrush(selectionBar, Border.BackgroundProperty, ChromeStyle.AccentBrushKey);
        PathIcon icon = ChromeControls.CreateIcon(IconOf(entry.Kind));
        icon.Margin = new Thickness(0, 0, ChromeStyle.SpacingMedium, 0);
        icon.VerticalAlignment = VerticalAlignment.Center;
        ChromeStyle.SetBrush(icon, PathIcon.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        TextBlock detail = new() { Text = entry.Detail, FontSize = ChromeStyle.FontSizeSmall, TextTrimming = TextTrimming.CharacterEllipsis };
        ChromeStyle.SetBrush(detail, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);

        // The title first, so a long folder path does not hide the file name.
        StackPanel lines = new()
        {
            Spacing = 1,
            Children = { new TextBlock { Text = entry.Title, TextTrimming = TextTrimming.CharacterEllipsis }, detail },
        };
        Children.Add(selectionBar);
        Children.Add(icon);
        Children.Add(lines);
        if (entry.RelativePath is not null)
        {
            ToolTip.SetTip(this, entry.RelativePath);
        }
    }

    /// <summary>Gets the icon of a kind of entry.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The icon.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a defined value.</exception>
    public static ChromeIcon IconOf(FileKind kind) =>
        kind switch
        {
            FileKind.View => ChromeIcon.View,
            FileKind.Styles => ChromeIcon.Styles,
            FileKind.Resources => ChromeIcon.Resources,
            FileKind.Gallery => ChromeIcon.Gallery,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "The kind is not a defined value."),
        };

    /// <summary>Creates the styles that show the accent bar only for the selected row. Add them to the list.</summary>
    /// <returns>New styles. The second style wins for a selected row, because it comes later.</returns>
    /// <remarks>The styles set the opacity, not the visibility. Thus the row keeps its layout when the selection changes.</remarks>
    public static Styles CreateSelectionStyles() =>
    [
        new Style(selector => selector.OfType<Border>().Class(SelectionBarClass))
        {
            Setters = { new Setter(OpacityProperty, 0.0) },
        },
        new Style(selector => selector.OfType<ListBoxItem>().Class(":selected").Descendant().OfType<Border>().Class(SelectionBarClass))
        {
            Setters = { new Setter(OpacityProperty, 1.0) },
        },
    ];
}
