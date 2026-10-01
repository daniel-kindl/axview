using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;

using AxView.Picker;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class PickerWindowTests
{
    [AvaloniaFact]
    public void RecentFileItem_ShowsTheFileNameFirstAndTheFolderSecond()
    {
        string path = Path.GetFullPath(Path.Combine("Project", "Views", "MainView.axaml"));

        RecentFileItem item = new(path);

        string?[] texts = [.. item.Children.OfType<TextBlock>().Select(t => t.Text)];
        Assert.Equal(["MainView", Path.GetDirectoryName(path)], texts);
        Assert.Equal(path, ToolTip.GetTip(item));
    }

    [AvaloniaFact]
    public void PickerWindow_ShowsAHintWhenNoFileIsRecent()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "recent.json"));

        PickerWindow window = new(recent);

        Assert.True(VisibleText(window, PickerWindow.NoRecentFilesText));
        Assert.False(window.GetLogicalDescendants().OfType<ListBox>().Single().IsVisible);
    }

    [AvaloniaFact]
    public void PickerWindow_HidesTheHintWhenAFileIsRecent()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "recent.json"));
        recent.Add(dir.Write("Views/MainView.axaml"));

        PickerWindow window = new(recent);

        Assert.False(VisibleText(window, PickerWindow.NoRecentFilesText));
    }

    private static bool VisibleText(Window window, string text) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsVisible);
}
