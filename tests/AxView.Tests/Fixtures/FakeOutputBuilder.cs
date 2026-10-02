using AxView.Building;
using AxView.Resolution;

namespace AxView.Tests.Fixtures;

/// <summary>An output builder that records each build. It builds nothing.</summary>
/// <param name="error">If set, each build fails with this message.</param>
internal sealed class FakeOutputBuilder(string? error = null) : IOutputBuilder
{
    private readonly List<AppOutput> builds = [];

    /// <summary>The app outputs that callers built, in order.</summary>
    public IReadOnlyList<AppOutput> Builds => builds;

    /// <inheritdoc />
    public Task BuildAsync(AppOutput output)
    {
        builds.Add(output);
        return error is null ? Task.CompletedTask : Task.FromException(new PreviewSetupException(error));
    }
}
