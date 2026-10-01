using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(AxView.Tests.Fixtures.HeadlessTestApp))]

namespace AxView.Tests.Fixtures;

/// <summary>The Avalonia app for the [AvaloniaFact] tests. It runs on the headless platform, with no theme.</summary>
public sealed class HeadlessTestApp : Application
{
    /// <summary>Configures the app for Avalonia.Headless.XUnit.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
