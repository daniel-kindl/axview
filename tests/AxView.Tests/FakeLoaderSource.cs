namespace AxView.Tests;

/// <summary>A loader source that returns a fixed path and records each requested version. It never restores.</summary>
internal sealed class FakeLoaderSource : ILoaderSource
{
    /// <summary>The path that <see cref="Find"/> returns.</summary>
    public static readonly string LoaderPath = Path.Combine(Path.GetTempPath(), "fake", "Avalonia.Markup.Xaml.Loader.dll");

    private readonly List<string> requestedVersions = [];

    /// <summary>The Avalonia versions that callers asked for, in order.</summary>
    public IReadOnlyList<string> RequestedVersions => requestedVersions;

    /// <inheritdoc />
    public string Find(string avaloniaVersion)
    {
        requestedVersions.Add(avaloniaVersion);
        return LoaderPath;
    }
}
