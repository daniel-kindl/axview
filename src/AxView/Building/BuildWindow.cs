using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

using AxView.Protocol;

namespace AxView.Building;

/// <summary>Shows the build of the app while no host runs: the app name, the time, and the last line of the build output.</summary>
/// <remarks>The Cancel button and a close by the user cancel the build.</remarks>
internal sealed class BuildWindow : Window, IBuildProgress
{
    private static readonly TimeSpan timerInterval = TimeSpan.FromSeconds(1);

    private readonly CancellationTokenSource cancellation = new();
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private readonly DispatcherTimer timer;
    private readonly TextBlock elapsed;
    private readonly TextBlock lastLine;
    private bool isDisposed;

    /// <param name="appName">The assembly name of the app that the build makes.</param>
    public BuildWindow(string appName)
    {
        Title = "AxView - Building";
        Icon = AppIcon.Get();
        Width = 520;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        TextBlock heading = new() { Text = $"Building {appName}", FontSize = 18 };
        elapsed = new TextBlock { Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Right };
        ProgressBar bar = new() { IsIndeterminate = true };
        lastLine = new TextBlock { Opacity = 0.7, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
        Button cancelButton = new() { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Right };
        cancelButton.Click += (_, _) => cancellation.Cancel();

        Grid header = new() { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { heading, elapsed } };
        Grid.SetColumn(elapsed, 1);
        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children = { header, bar, lastLine, cancelButton },
        };

        timer = new DispatcherTimer(timerInterval, DispatcherPriority.Normal, (_, _) => ShowElapsed());
        timer.Start();
        ShowElapsed();
    }

    /// <inheritdoc />
    public CancellationToken Cancellation => cancellation.Token;

    /// <inheritdoc />
    /// <remarks>The build calls this method on a worker thread.</remarks>
    public void Report(string value) => Dispatcher.UIThread.Post(() => lastLine.Text = value.Trim());

    /// <summary>Closes the window. The build is not canceled.</summary>
    public void Dispose()
    {
        isDisposed = true;
        timer.Stop();
        Close();
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        timer.Stop();

        // Only the user closes the window during the build.
        if (!isDisposed)
        {
            cancellation.Cancel();
        }

        base.OnClosed(e);
    }

    private void ShowElapsed() => elapsed.Text = $"{(int)stopwatch.Elapsed.TotalSeconds} s";
}
