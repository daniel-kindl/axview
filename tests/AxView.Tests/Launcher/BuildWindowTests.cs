using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using AxView.Building;

namespace AxView.Tests.Launcher;

public sealed class BuildWindowTests
{
    [AvaloniaFact]
    public void BuildWindow_ShowsTheAppName()
    {
        BuildWindow window = new("Desktop");

        Assert.Contains(Texts(window), text => text.Contains("Desktop", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void Report_ShowsTheLastLineOfTheBuild()
    {
        BuildWindow window = new("Desktop");

        window.Report("  Core -> Core.dll");
        window.Report("  Desktop -> Desktop.dll");
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("Desktop -> Desktop.dll", Texts(window));
        Assert.DoesNotContain("Core -> Core.dll", Texts(window));
    }

    [AvaloniaFact]
    public void CancelButton_CancelsTheBuild()
    {
        BuildWindow window = new("Desktop");
        Button cancel = window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, "Cancel"));

        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.True(window.Cancellation.IsCancellationRequested);
    }

    [AvaloniaFact]
    public void Close_ByTheUserCancelsTheBuild()
    {
        BuildWindow window = new("Desktop");
        window.Show();

        window.Close();

        Assert.True(window.Cancellation.IsCancellationRequested);
    }

    [AvaloniaFact]
    public void Dispose_ClosesTheWindowAndDoesNotCancelTheBuild()
    {
        BuildWindow window = new("Desktop");
        window.Show();

        window.Dispose();

        Assert.False(window.IsVisible);
        Assert.False(window.Cancellation.IsCancellationRequested);
    }

    private static string[] Texts(Window window) =>
        [.. window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty)];
}
