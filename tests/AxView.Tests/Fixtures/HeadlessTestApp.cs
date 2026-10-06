using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(AxView.Tests.Fixtures.HeadlessTestApp))]

namespace AxView.Tests.Fixtures;

/// <summary>The Avalonia app for the [AvaloniaFact] tests. It runs on the headless platform, with no theme.</summary>
/// <remarks>Skia draws the frames, so a test can capture a window as a PNG. The Inter font gives the same text on each OS.</remarks>
public sealed class HeadlessTestApp : Application
{
    /// <summary>Configures the app for Avalonia.Headless.XUnit.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
