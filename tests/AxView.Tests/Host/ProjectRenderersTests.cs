extern alias host;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class ProjectRenderersTests
{
    [Fact]
    public void Create_MakesARendererForEachProjectWhoseAssemblyLoads()
    {
        using TempDir loadable = new();
        using TempDir unloadable = new();
        using TempDir withoutProject = new();
        _ = loadable.Write("Library.csproj", "<Project><PropertyGroup><AssemblyName>Avalonia.Controls</AssemblyName></PropertyGroup></Project>");
        _ = unloadable.Write("NoSuchAssembly.csproj", "<Project />");

        IEnumerable<PreviewRenderer> renderers =
            ProjectRenderers.Create([loadable.Path, unloadable.Path, withoutProject.Path], DesignServices.None);

        PreviewRenderer renderer = Assert.Single(renderers);
        Assert.Equal("Avalonia.Controls", renderer.AssemblyName);
        Assert.Equal(loadable.Path, renderer.ProjectDirectory);
    }
}
