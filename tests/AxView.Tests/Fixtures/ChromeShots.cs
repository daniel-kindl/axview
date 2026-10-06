using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Threading;

namespace AxView.Tests.Fixtures;

/// <summary>Writes a PNG of an AxView window, so an agent can look at the chrome.</summary>
/// <remarks>
/// The PNGs go to the <c>chrome-shots</c> folder next to the test assembly. Git ignores it.
/// The name of a PNG is the shot name and the look, for example <c>picker-recent-FluentDark.png</c>.
/// </remarks>
internal static class ChromeShots
{
    /// <summary>Gets the folder of the PNGs.</summary>
    public static string Folder { get; } = Path.Combine(AppContext.BaseDirectory, "chrome-shots");

    /// <summary>Gets each chrome look, as theory data.</summary>
    public static TheoryData<ChromeLook> Looks => [.. Enum.GetValues<ChromeLook>()];

    /// <summary>Creates the window in the look, shows it, writes a PNG of it, and closes it.</summary>
    /// <param name="createWindow">Creates the window. The theme must exist before the window, so the controls get their templates.</param>
    /// <param name="name">The shot name, the first part of the file name.</param>
    /// <param name="look">The chrome theme and the theme variant.</param>
    /// <returns>The PNG and the client size of the window.</returns>
    public static ChromeShot Save(Func<Window> createWindow, string name, ChromeLook look)
    {
        Application app = Application.Current ?? throw new InvalidOperationException("The headless app is not running.");
        bool isSimple = look is ChromeLook.SimpleLight or ChromeLook.SimpleDark;
        IStyle theme = isSimple ? new SimpleTheme() : new FluentTheme();
        app.Styles.Add(theme);
        app.RequestedThemeVariant = look is ChromeLook.FluentDark or ChromeLook.SimpleDark ? ThemeVariant.Dark : ThemeVariant.Light;
        try
        {
            Window window = createWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            string png = Path.Combine(Folder, $"{name}-{look}.png");
            Capture(window, png);
            Size clientSize = window.ClientSize;
            window.Close();
            return new ChromeShot(png, clientSize);
        }
        finally
        {
            // The other tests run with no theme.
            _ = app.Styles.Remove(theme);
            app.RequestedThemeVariant = null;
        }
    }

    private static void Capture(Window window, string png)
    {
        using WriteableBitmap frame = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("The headless platform gave no frame. It must use Skia.");
        _ = Directory.CreateDirectory(Folder);
        frame.Save(png, new PngBitmapEncoderOptions());
    }
}
