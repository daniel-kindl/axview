extern alias host;

using host::AxView.Host;

namespace AxView.Tests;

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
}
