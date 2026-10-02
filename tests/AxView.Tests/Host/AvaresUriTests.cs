extern alias host;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class AvaresUriTests
{
    [Fact]
    public void For_PointsAtTheFileInsideTheAssembly()
    {
        using TempDir dir = new();
        string project = Path.Combine(dir.Path, "App");
        string axaml = dir.Write("App/Views/Main View.axaml");

        Uri uri = AvaresUri.For("My.App", project, axaml);

        // Avalonia registers the avares scheme, which keeps the case of the host. The default parser does not.
        Assert.Equal("avares://my.app/Views/Main%20View.axaml", uri.AbsoluteUri, ignoreCase: true);
    }

    [Fact]
    public void ToPath_GivesTheProjectFileOfALocalUri()
    {
        string project = Path.Combine(Path.GetTempPath(), "App");

        string? path = AvaresUri.ToPath(new Uri("avares://My.App/Styles/Main%20Styles.axaml"), "My.App", project);

        Assert.Equal(Path.Combine(project, "Styles", "Main Styles.axaml"), path);
    }

    [Fact]
    public void ToPath_UriOfAnotherAssembly_GivesNull()
    {
        string project = Path.Combine(Path.GetTempPath(), "App");

        Assert.Null(AvaresUri.ToPath(new Uri("avares://Other/Styles/Main.axaml"), "My.App", project));
    }
}
