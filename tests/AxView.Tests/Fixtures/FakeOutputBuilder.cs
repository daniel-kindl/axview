using AxView.Building;
using AxView.Resolution;

namespace AxView.Tests.Fixtures;

/// <summary>An output builder that records each build. It builds nothing.</summary>
/// <param name="error">If set, each build fails with this message.</param>
internal sealed class FakeOutputBuilder(string? error = null) : IOutputBuilder
{
    /// <summary>The line that each build reports.</summary>
    public const string BuildLine = "Ui -> Ui.dll";

    private readonly List<(AppOutput Output, CancellationToken Cancellation)> builds = [];

    /// <summary>The app outputs that callers built, in order.</summary>
    public IReadOnlyList<AppOutput> Builds => [.. builds.Select(build => build.Output)];

    /// <summary>The cancellation token of each build, in order.</summary>
    public IReadOnlyList<CancellationToken> Cancellations => [.. builds.Select(build => build.Cancellation)];

    /// <inheritdoc />
    public Task BuildAsync(AppOutput output, IProgress<string> progress, CancellationToken cancellation)
    {
        builds.Add((output, cancellation));
        progress.Report(BuildLine);
        return error is null ? Task.CompletedTask : Task.FromException(new PreviewSetupException(error));
    }
}
