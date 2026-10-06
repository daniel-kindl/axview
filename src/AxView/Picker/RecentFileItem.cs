using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using AxView.Protocol;

namespace AxView.Picker;

/// <summary>One row of the recent file list: a file icon, the file name, then its folder.</summary>
/// <remarks>The file name comes first, so a long folder path does not hide it. The tooltip shows the full path.</remarks>
internal sealed class RecentFileItem : DockPanel
{
    /// <summary>Creates the row for one file.</summary>
    /// <param name="path">The full path of an AXAML file.</param>
    public RecentFileItem(string path)
    {
        PathIcon icon = ChromeControls.CreateIcon(ChromeIcon.View);
        icon.Margin = new Thickness(0, 0, ChromeStyle.SpacingMedium, 0);
        icon.VerticalAlignment = VerticalAlignment.Center;
        ChromeStyle.SetBrush(icon, PathIcon.ForegroundProperty, ChromeStyle.MutedTextBrushKey);

        // The end of the folder path is the part that tells files apart.
        TextBlock folder = new()
        {
            Text = Path.GetDirectoryName(path),
            FontSize = ChromeStyle.FontSizeSmall,
            TextTrimming = TextTrimming.LeadingCharacterEllipsis,
        };
        ChromeStyle.SetBrush(folder, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        TextBlock name = new() { Text = Path.GetFileNameWithoutExtension(path), TextTrimming = TextTrimming.CharacterEllipsis };
        Children.Add(icon);
        Children.Add(new StackPanel { Spacing = 1, Children = { name, folder } });
        ToolTip.SetTip(this, path);
    }
}
