extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class ErrorPanelTests
{
    private const string Error = "Line 3: Unable to resolve type Unknown.";

    [AvaloniaFact]
    public void Show_NoError_HidesThePanel()
    {
        ErrorPanel panel = new();
        panel.Show(Error, isLastGoodRenderShown: false);

        panel.Show(null, isLastGoodRenderShown: false);

        Assert.False(panel.IsVisible);
    }

    [AvaloniaFact]
    public void Show_AnError_ShowsTheSelectableTextInMonospace()
    {
        ErrorPanel panel = new();

        panel.Show(Error, isLastGoodRenderShown: false);

        SelectableTextBlock text = panel.GetLogicalDescendants().OfType<SelectableTextBlock>().Single();
        Assert.True(panel.IsVisible);
        Assert.Equal(Error, text.Text);
        Assert.Contains("monospace", text.FontFamily.ToString(), StringComparison.Ordinal);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Show_TellsIfTheLastGoodRenderShows(bool isLastGoodRenderShown)
    {
        ErrorPanel panel = new();

        panel.Show(Error, isLastGoodRenderShown);

        Assert.Equal(isLastGoodRenderShown, TextOf(panel, ErrorPanel.LastGoodRenderNote).IsVisible);
    }

    [AvaloniaFact]
    public void Collapse_HidesTheText_AndExpandShowsItAgain()
    {
        ErrorPanel panel = new();
        Window window = new() { Content = panel };
        window.Show();
        panel.Show(Error, isLastGoodRenderShown: true);
        Button collapse = ButtonOf(panel, "Collapse the error");
        SelectableTextBlock text = panel.GetLogicalDescendants().OfType<SelectableTextBlock>().Single();

        collapse.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        bool isTextShownCollapsed = IsShown(text);
        bool isNoteShownCollapsed = IsShown(TextOf(panel, ErrorPanel.LastGoodRenderNote));
        object? collapsedTip = ToolTip.GetTip(collapse);
        collapse.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        bool isTextShownExpanded = IsShown(text);
        window.Close();

        Assert.False(isTextShownCollapsed);
        Assert.False(isNoteShownCollapsed);
        Assert.Equal("Expand the error", collapsedTip);
        Assert.True(isTextShownExpanded);
        Assert.Equal("Collapse the error", ToolTip.GetTip(collapse));
    }

    [AvaloniaFact]
    public async Task Copy_PutsTheErrorOnTheClipboardAsync()
    {
        ErrorPanel panel = new();
        Window window = new() { Content = panel };
        window.Show();
        panel.Show(Error, isLastGoodRenderShown: false);

        ButtonOf(panel, "Copy the error").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        IClipboard clipboard = window.Clipboard ?? throw new InvalidOperationException("The headless window has no clipboard.");
        string? copied = await clipboard.TryGetTextAsync();
        window.Close();

        Assert.Equal(Error, copied);
    }

    // The test app has no theme. Thus a ScrollViewer has no template, and its content has no visual parent.
    private static bool IsShown(Control control) =>
        control.IsVisible && control.GetLogicalAncestors().OfType<Control>().All(ancestor => ancestor.IsVisible);

    private static Button ButtonOf(ErrorPanel panel, string tip) =>
        panel.GetLogicalDescendants().OfType<Button>().Single(button => Equals(ToolTip.GetTip(button), tip));

    private static TextBlock TextOf(ErrorPanel panel, string text) =>
        panel.GetLogicalDescendants().OfType<TextBlock>().Single(block => block.Text == text);
}
