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
        string?[] expected = ["MainView", Path.GetDirectoryName(path)];
        Assert.Equal(expected, texts);
        Assert.Equal(path, ToolTip.GetTip(item));
    }

    [AvaloniaFact]
    public void PickerWindow_ShowsAHintWhenNoFileIsRecent()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "settings.json"));

        PickerWindow window = new(recent);

        Assert.True(VisibleText(window, PickerWindow.NoRecentFilesText));
        Assert.False(window.GetLogicalDescendants().OfType<ListBox>().Single().IsVisible);
    }

    [AvaloniaFact]
    public void PickerWindow_HidesTheHintWhenAFileIsRecent()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "settings.json"));
        recent.Add(dir.Write("Views/MainView.axaml"));

        PickerWindow window = new(recent);

        Assert.False(VisibleText(window, PickerWindow.NoRecentFilesText));
    }

    [AvaloniaFact]
    public void PickerWindow_SelectsTheLastOpenFile()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "settings.json"));
        recent.Add(dir.Write("Views/Old.axaml"));
        string last = dir.Write("Views/Last.axaml");
        recent.Add(last);

        PickerWindow window = new(recent);

        Assert.Equal(last, window.GetLogicalDescendants().OfType<ListBox>().Single().SelectedItem);
    }

    private static bool VisibleText(Window window, string text) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsVisible);
}
