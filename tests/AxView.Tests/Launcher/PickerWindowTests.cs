using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;

using AxView.Picker;
using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class PickerWindowTests
{
    [AvaloniaFact]
    public void RecentFileItem_ShowsTheFileNameFirstAndTheFolderSecond()
    {
        string path = Path.GetFullPath(Path.Combine("Project", "Views", "MainView.axaml"));

        RecentFileItem item = new(path);

        string?[] texts = [.. item.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text)];
        string?[] expected = ["MainView", Path.GetDirectoryName(path)];
        Assert.Equal(expected, texts);
        Assert.Equal(path, ToolTip.GetTip(item));
        _ = Assert.Single(item.GetLogicalDescendants().OfType<PathIcon>());
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

    [AvaloniaFact]
    public void PickerWindow_ShowsTheHeader_WithTheAppIcon_TheName_AndTheSubtitle()
    {
        using TempDir dir = new();

        PickerWindow window = new(new RecentFiles(Path.Combine(dir.Path, "settings.json")));

        // The layout also contains the name. The header is the innermost panel.
        DockPanel header = window.GetLogicalDescendants().OfType<DockPanel>()
            .Last(panel => panel.GetLogicalDescendants().OfType<TextBlock>().Any(text => text.Text == "AxView"));
        List<string?> texts = [.. header.GetLogicalDescendants().OfType<TextBlock>().Select(text => text.Text)];
        Assert.Equal(["Ax", "AxView", PickerWindow.SubtitleText], texts);
        Button about = header.GetLogicalDescendants().OfType<Button>().Single();
        Assert.Equal("About AxView", ToolTip.GetTip(about));
        _ = Assert.IsType<PathIcon>(about.Content);
    }

    [AvaloniaFact]
    public void PickerWindow_ShowsOpenAsThePrimaryButton()
    {
        using TempDir dir = new();

        PickerWindow window = new(new RecentFiles(Path.Combine(dir.Path, "settings.json")));

        Button open = window.GetLogicalDescendants().OfType<Button>().Single(button => button.Content is "Open AXAML file...");
        Assert.Contains("accent", open.Classes);
    }

    [AvaloniaFact]
    public void ShowMessage_ShowsTheMessageAsAnErrorCard()
    {
        using TempDir dir = new();
        PickerWindow window = new(new RecentFiles(Path.Combine(dir.Path, "settings.json"))) { RequestedThemeVariant = ThemeVariant.Light };

        window.ShowMessage("The build failed.");

        TextBlock message = window.GetLogicalDescendants().OfType<TextBlock>().Single(text => text.Text == "The build failed.");
        Border card = message.GetLogicalAncestors().OfType<Border>().First();
        Assert.True(card.IsVisible);
        Color border = Assert.IsType<ISolidColorBrush>(card.BorderBrush, exactMatch: false).Color;
        Assert.Equal(ChromeBrushes.ColorOf(ChromeStyle.ErrorBrushKey, ThemeVariant.Light), border);
        Assert.False(message.IsSet(TextBlock.ForegroundProperty));
        _ = Assert.Single(card.GetLogicalDescendants().OfType<PathIcon>());
    }

    private static bool VisibleText(Window window, string text) =>
        window.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsVisible);
}
