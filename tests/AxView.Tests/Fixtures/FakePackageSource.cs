using AxView.Packages;

namespace AxView.Tests.Fixtures;

/// <summary>A package source that returns a fixed path for each package and records each request. It never restores.</summary>
internal sealed class FakePackageSource : IPackageSource
{
    private readonly List<string> requests = [];

    /// <summary>The path that <see cref="Find"/> returns for the XAML loader.</summary>
    public static string LoaderPath => PathOf("Avalonia.Markup.Xaml.Loader");

    /// <summary>The path that <see cref="Find"/> returns for the Simple theme.</summary>
    public static string SimpleThemePath => PathOf("Avalonia.Themes.Simple");

    /// <summary>The requests of the callers, in order. Each request is <c>package/version</c>.</summary>
    public IReadOnlyList<string> Requests => requests;

    /// <inheritdoc />
    public string Find(string packageId, string avaloniaVersion)
    {
        requests.Add($"{packageId}/{avaloniaVersion}");
        return PathOf(packageId);
    }

    private static string PathOf(string packageId) => Path.Combine(Path.GetTempPath(), "fake", packageId + ".dll");
}
