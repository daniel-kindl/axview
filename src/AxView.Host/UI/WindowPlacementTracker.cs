using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>
/// Remembers the state and the normal bounds of a window.
/// A maximized window thus opens with its normal bounds when the user restores it.
/// </summary>
/// <remarks>
/// The platform can report the maximized bounds before the maximized state.
/// Thus the tracker keeps new bounds only after the window stays normal for the settle time.
/// </remarks>
internal sealed class WindowPlacementTracker
{
    private static readonly TimeSpan defaultSettleTime = TimeSpan.FromMilliseconds(500);

    private readonly Window window;
    private readonly DispatcherTimer settleTimer;
    private PixelPoint normalPosition;
    private Size normalSize;

    /// <exception cref="ArgumentException">The window has no width or no height.</exception>
    public WindowPlacementTracker(Window window)
        : this(window, defaultSettleTime)
    {
    }

    /// <param name="window">The window to track.</param>
    /// <param name="settleTime">The time that the window must stay normal before the tracker keeps its new bounds.</param>
    /// <exception cref="ArgumentException">The window has no width or no height.</exception>
    public WindowPlacementTracker(Window window, TimeSpan settleTime)
    {
        if (double.IsNaN(window.Width) || double.IsNaN(window.Height))
        {
            throw new ArgumentException("The window must have a width and a height.", nameof(window));
        }

        this.window = window;
        normalPosition = window.Position;
        normalSize = new Size(window.Width, window.Height);
        settleTimer = new DispatcherTimer { Interval = settleTime };
        settleTimer.Tick += (_, _) => RememberNormalBounds();
        window.PositionChanged += (_, _) => RestartSettleTimer();
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == TopLevel.ClientSizeProperty)
            {
                RestartSettleTimer();
            }
            else if (e.Property == Window.WindowStateProperty)
            {
                // The bounds since the last settle can be the maximized bounds. Do not keep them.
                settleTimer.Stop();
            }
        };
    }

    /// <summary>Gives a window the state and the bounds of a placement.</summary>
    /// <remarks>The window keeps its own bounds when they are not valid or not on a screen.</remarks>
    public static void Apply(Window window, WindowPlacement placement)
    {
        PixelPoint point = new(placement.X, placement.Y);
        if (window.Screens.ScreenFromPoint(point) is not null)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = point;
        }

        if ((placement.Width > 0) && (placement.Height > 0))
        {
            window.Width = placement.Width;
            window.Height = placement.Height;
        }

        window.WindowState = placement.Mode switch
        {
            WindowMode.Maximized => WindowState.Maximized,
            WindowMode.FullScreen => WindowState.FullScreen,
            _ => WindowState.Normal,
        };
    }

    /// <summary>Gives the state of the window and its normal bounds.</summary>
    public WindowPlacement GetPlacement()
    {
        if (window.WindowState == WindowState.Normal)
        {
            RememberNormalBounds();
        }

        WindowMode mode = window.WindowState switch
        {
            WindowState.Maximized => WindowMode.Maximized,
            WindowState.FullScreen => WindowMode.FullScreen,
            _ => WindowMode.Normal,
        };
        return new WindowPlacement(mode, normalPosition.X, normalPosition.Y, normalSize.Width, normalSize.Height);
    }

    private void RestartSettleTimer()
    {
        settleTimer.Stop();
        settleTimer.Start();
    }

    private void RememberNormalBounds()
    {
        settleTimer.Stop();
        if (window.WindowState == WindowState.Normal)
        {
            normalPosition = window.Position;
            normalSize = window.ClientSize;
        }
    }
}
