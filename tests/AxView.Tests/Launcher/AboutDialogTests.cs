using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;

using AxView.Picker;
using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class AboutDialogTests
{
    [AvaloniaFact]
    public void AboutDialog_ShowsTheVersionTheBuildDateAndTheAvaloniaVersion()
    {
        AboutInfo info = new("1.2.3", new DateOnly(2026, 10, 2), "11.3.22");

        AboutDialog dialog = new(info);

        string[] texts = [.. dialog.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty)];
        Assert.Contains(texts, t => t.Contains("1.2.3", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("2026-10-02", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("11.3.22", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void PickerWindow_HasAnAboutButton()
    {
        using TempDir dir = new();

        PickerWindow window = new(new RecentFiles(Path.Combine(dir.Path, "settings.json")));

        Assert.Contains(window.GetLogicalDescendants().OfType<Button>(), b => Equals(ToolTip.GetTip(b), "About AxView"));
    }
}
