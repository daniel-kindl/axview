using AxView.Building;

namespace AxView.Tests.Fixtures;

/// <summary>A build progress that records the lines of the build.</summary>
/// <param name="appName">The app that the build makes.</param>
internal sealed class FakeBuildProgress(string appName) : IBuildProgress
{
    private readonly List<string> lines = [];

    /// <summary>The app that the build makes.</summary>
    public string AppName => appName;

    /// <summary>The lines that the build reported, in order.</summary>
    public IReadOnlyList<string> Lines => lines;

    /// <summary>True after the session closed the progress.</summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public CancellationToken Cancellation { get; } = new CancellationTokenSource().Token;

    /// <inheritdoc />
    public void Report(string value) => lines.Add(value);

    /// <inheritdoc />
    public void Dispose() => IsDisposed = true;
}
