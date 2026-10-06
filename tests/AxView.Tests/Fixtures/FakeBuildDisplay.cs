using AxView.Building;

namespace AxView.Tests.Fixtures;

/// <summary>A build display that records each build progress that it shows. It shows nothing.</summary>
internal sealed class FakeBuildDisplay : IBuildDisplay
{
    private readonly List<FakeBuildProgress> shown = [];

    /// <summary>The build progress of each build, in order.</summary>
    public IReadOnlyList<FakeBuildProgress> Shown => shown;

    /// <inheritdoc />
    public IBuildProgress Show(string appName)
    {
        FakeBuildProgress progress = new(appName);
        shown.Add(progress);
        return progress;
    }
}
