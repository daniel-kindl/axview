using Avalonia.Controls;
using Avalonia.Media;

namespace AxView.Picker;

/// <summary>One row of the recent file list: the file name, then its folder.</summary>
/// <remarks>The file name comes first, so a long folder path does not hide it. The tooltip shows the full path.</remarks>
internal sealed class RecentFileItem : StackPanel
{
    /// <summary>Creates the row for one file.</summary>
    /// <param name="path">The full path of an AXAML file.</param>
    public RecentFileItem(string path)
    {
        Spacing = 1;
        Children.Add(new TextBlock { Text = Path.GetFileNameWithoutExtension(path), TextTrimming = TextTrimming.CharacterEllipsis });
        // The end of the folder path is the part that tells files apart.
        Children.Add(new TextBlock
        {
            Text = Path.GetDirectoryName(path),
            FontSize = 11,
            Opacity = 0.6,
            TextTrimming = TextTrimming.LeadingCharacterEllipsis,
        });
        ToolTip.SetTip(this, path);
    }
}
