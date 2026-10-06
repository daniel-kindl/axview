using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;

using AxView.Building;
using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class BuildWindowTests
{
    [AvaloniaFact]
    public void BuildWindow_ShowsTheAppName()
    {
        using BuildWindow window = new("Desktop");

        Assert.Contains(Texts(window), text => text.Contains("Desktop", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void Report_ShowsTheLastLineOfTheBuild()
    {
        using BuildWindow window = new("Desktop");

        window.Report("  Core -> Core.dll");
        window.Report("  Desktop -> Desktop.dll");
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("Desktop -> Desktop.dll", Texts(window));
        Assert.DoesNotContain("Core -> Core.dll", Texts(window));
    }

    [AvaloniaFact]
    public void CancelButton_CancelsTheBuild()
    {
        using BuildWindow window = new("Desktop");
        Button cancel = window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Cancel"));

        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.True(window.Cancellation.IsCancellationRequested);
    }

    [AvaloniaFact]
    public void Close_ByTheUserCancelsTheBuild()
    {
        using BuildWindow window = new("Desktop");
        window.Show();

        window.Close();

        Assert.True(window.Cancellation.IsCancellationRequested);
    }

    [AvaloniaFact]
    public void Dispose_ClosesTheWindowAndDoesNotCancelTheBuild()
    {
        using BuildWindow window = new("Desktop");
        window.Show();

        window.Dispose();

        Assert.False(window.IsVisible);
        Assert.False(window.Cancellation.IsCancellationRequested);
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(75, "1:15")]
    [InlineData(3725, "62:05")]
    public void FormatElapsed_GivesMinutesAndSeconds(int seconds, string expected) =>
        Assert.Equal(expected, BuildWindow.FormatElapsed(TimeSpan.FromSeconds(seconds)));

    [AvaloniaFact]
    public void BuildWindow_ShowsTheAppIcon_AndTheLastLineInMonospace()
    {
        using BuildWindow window = new("Desktop");

        window.Report("  Desktop -> Desktop.dll");
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(window.GetLogicalDescendants().OfType<Control>(), control => control.Classes.Contains(AppIcon.FaceClass));
        TextBlock lastLine = window.GetLogicalDescendants().OfType<TextBlock>().Single(text => text.Text == "Desktop -> Desktop.dll");
        Assert.Equal(ChromeStyle.Monospace, lastLine.FontFamily);
    }

    [AvaloniaFact]
    public void BuildWindow_TakesEachBrushFromTheChromeStyle()
    {
        using BuildWindow window = new("Desktop") { RequestedThemeVariant = ThemeVariant.Dark };

        ChromeBrushes.AssertFromChromeStyle(window, ThemeVariant.Dark);
    }

    private static string[] Texts(Window window) =>
        [.. window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty)];
}
