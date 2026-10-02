using Avalonia;

namespace AxView.Tests.Fixtures;

/// <summary>A target app that counts its startup calls. The importer must call Initialize, and OnFrameworkInitializationCompleted only on request.</summary>
public sealed class StartupFixtureApp : Application
{
    /// <summary>Gets the number of calls to <see cref="OnFrameworkInitializationCompleted"/>.</summary>
    public static int StartupCount { get; private set; }

    /// <summary>Gets the lifetime that the last startup call saw.</summary>
    public static object? LastLifetime { get; private set; }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        StartupCount++;
        LastLifetime = ApplicationLifetime;
        base.OnFrameworkInitializationCompleted();
    }
}
