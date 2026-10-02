extern alias host;

using Avalonia.Headless.XUnit;

using AxView.Tests.Fixtures;

using host::AxView.Host.Startup;

namespace AxView.Tests.Host;

public sealed class TargetAppImporterTests
{
    [AvaloniaFact]
    public void ImportFrom_DoesNotRunTheAppStartupByDefault()
    {
        int before = StartupFixtureApp.StartupCount;

        _ = TargetAppImporter.ImportFrom(typeof(StartupFixtureApp), runStartup: false, []);

        Assert.Equal(before, StartupFixtureApp.StartupCount);
    }

    [AvaloniaFact]
    public void ImportFrom_RunsTheAppStartupWithNoLifetimeWhenAsked()
    {
        int before = StartupFixtureApp.StartupCount;

        _ = TargetAppImporter.ImportFrom(typeof(StartupFixtureApp), runStartup: true, []);

        Assert.Equal(before + 1, StartupFixtureApp.StartupCount);
        Assert.Null(StartupFixtureApp.LastLifetime);
    }
}
