extern alias host;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class WindowPlacementTrackerTests
{
    [AvaloniaFact]
    public void Apply_RestoresThePositionTheSizeAndTheMode()
    {
        Window window = new();

        WindowPlacementTracker.Apply(window, new WindowPlacement(WindowMode.Maximized, 30, 40, 900, 600));

        Assert.Equal(new PixelPoint(30, 40), window.Position);
        Assert.Equal(900, window.Width);
        Assert.Equal(600, window.Height);
        Assert.Equal(WindowState.Maximized, window.WindowState);
    }

    [AvaloniaFact]
    public void GetPlacement_GivesTheBoundsOfANormalWindow()
    {
        Window window = new() { Width = 900, Height = 600 };
        window.Show();
        WindowPlacementTracker tracker = new(window);

        window.Position = new PixelPoint(30, 40);

        Assert.Equal(new WindowPlacement(WindowMode.Normal, 30, 40, 900, 600), tracker.GetPlacement());
    }

    [AvaloniaFact]
    public void GetPlacement_KeepsTheNormalBoundsOfAMaximizedWindow()
    {
        Window window = new() { Width = 900, Height = 600 };
        window.Show();
        window.Position = new PixelPoint(30, 40);
        WindowPlacementTracker tracker = new(window);

        // The platform moves and resizes the window before it reports the new state.
        window.Position = new PixelPoint(0, 0);
        window.Width = 1920;
        window.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new WindowPlacement(WindowMode.Maximized, 30, 40, 900, 600), tracker.GetPlacement());
    }

    [AvaloniaFact]
    public void GetPlacement_KeepsTheBoundsOfAMoveBeforeTheMaximize()
    {
        Window window = new() { Width = 900, Height = 600 };
        window.Show();
        WindowPlacementTracker tracker = new(window, TimeSpan.FromMilliseconds(1));

        window.Position = new PixelPoint(50, 60);
        WaitForTheBoundsToSettle();
        window.Position = new PixelPoint(0, 0);
        window.WindowState = WindowState.Maximized;

        Assert.Equal(new WindowPlacement(WindowMode.Maximized, 50, 60, 900, 600), tracker.GetPlacement());
    }

    [AvaloniaFact]
    public void GetPlacement_GivesNormalForAMinimizedWindow()
    {
        Window window = new() { Width = 900, Height = 600 };
        window.Show();
        WindowPlacementTracker tracker = new(window);

        window.WindowState = WindowState.Minimized;

        Assert.Equal(WindowMode.Normal, tracker.GetPlacement().Mode);
    }

    [AvaloniaFact]
    public void GetPlacement_GivesFullScreenForAFullScreenWindow()
    {
        Window window = new() { Width = 900, Height = 600 };
        window.Show();
        WindowPlacementTracker tracker = new(window);

        window.WindowState = WindowState.FullScreen;

        Assert.Equal(WindowMode.FullScreen, tracker.GetPlacement().Mode);
    }

    // Run the dispatcher loop, so that the settle timer can tick.
    private static void WaitForTheBoundsToSettle()
    {
        DispatcherFrame frame = new();
        _ = DispatcherTimer.RunOnce(() => frame.Continue = false, TimeSpan.FromMilliseconds(100));
        Dispatcher.UIThread.PushFrame(frame);
    }
}
