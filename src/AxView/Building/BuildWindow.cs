using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using AxView.Protocol;

namespace AxView.Building;

/// <summary>Shows the build of the app while no host runs: the app name, the time, and the last line of the build output.</summary>
/// <remarks>The Cancel button and a close by the user cancel the build.</remarks>
internal sealed class BuildWindow : Window, IBuildProgress
{
    private const double HeaderIconSize = 32;

    private static readonly TimeSpan timerInterval = TimeSpan.FromSeconds(1);

    [SuppressMessage(
        "Usage",
        "CA2213",
        Justification = "The build reads the token after Dispose. A source without a timer holds no resources.")]
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

        Resources.MergedDictionaries.Add(ChromeStyle.CreateResources());

        Control face = AppIcon.CreateFace(HeaderIconSize);
        face.Margin = new Thickness(0, 0, ChromeStyle.SpacingLarge, 0);
        TextBlock heading = new()
        {
            Text = $"Building {appName}",
            FontSize = ChromeStyle.FontSizeHeading,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        elapsed = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(ChromeStyle.SpacingLarge, 0, 0, 0) };
        ChromeStyle.SetBrush(elapsed, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        DockPanel.SetDock(face, Dock.Left);
        DockPanel.SetDock(elapsed, Dock.Right);
        DockPanel header = new() { Children = { face, elapsed, heading } };

        ProgressBar bar = new() { IsIndeterminate = true };
        lastLine = new TextBlock
        {
            FontFamily = ChromeStyle.Monospace,
            FontSize = ChromeStyle.FontSizeNormal,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        ChromeStyle.SetBrush(lastLine, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        Button cancelButton = new() { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Right };
        cancelButton.Click += (_, _) => cancellation.Cancel();

        Content = new StackPanel
        {
            Margin = new Thickness(ChromeStyle.SpacingExtraLarge),
            Spacing = ChromeStyle.SpacingLarge,
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

    /// <summary>Gives the time of the build as minutes and seconds, for example <c>1:15</c>.</summary>
    /// <param name="elapsed">The time since the build started.</param>
    /// <returns>The text.</returns>
    public static string FormatElapsed(TimeSpan elapsed) =>
        string.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}");

    private void ShowElapsed() => elapsed.Text = FormatElapsed(stopwatch.Elapsed);
}
