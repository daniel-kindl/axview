extern alias host;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class ProjectSetTests
{
    private const string ViewAssembly = "AxView.Tests";
    private const string LibraryAssembly = "Avalonia.Controls";
    private const string AppAssembly = "Avalonia.Base";

    [Fact]
    public void Load_GivesTheCodeFoldersOfTheViewTheAppAndTheReferencedProjects()
    {
        using TempDir view = new();
        using TempDir app = new();
        using TempDir library = new();
        using TempDir unloadable = new();
        _ = unloadable.Write("NoSuchAssembly.csproj", "<Project />");

        ProjectSet projects = ProjectSet.Load(Options(view, app, AppAssembly, library, unloadable));

        Assert.Equal([view.Path, app.Path, library.Path, unloadable.Path], projects.CodeDirectories);
    }

    [Fact]
    public void Load_AppView_GivesOnlyTheViewFolder()
    {
        using TempDir view = new();

        ProjectSet projects = ProjectSet.Load(Options(view));

        Assert.Equal([view.Path], projects.CodeDirectories);
        Assert.Equal(ViewAssembly, projects.ViewAssembly.GetName().Name);
    }

    [Fact]
    public void Load_LoadsTheAppAssembly()
    {
        using TempDir view = new();

        ProjectSet projects = ProjectSet.Load(Options(view, appAssembly: AppAssembly));

        Assert.Equal(AppAssembly, projects.AppAssembly?.GetName().Name);
    }

    [Fact]
    public void Load_AppAssemblyThatDoesNotLoad_GivesNoAppAssembly()
    {
        using TempDir view = new();

        ProjectSet projects = ProjectSet.Load(Options(view, appAssembly: "NoSuchAssembly"));

        Assert.Null(projects.AppAssembly);
    }

    [Fact]
    public void CreateRenderers_GivesTheViewTheAppAndEachLoadableReferencedProject()
    {
        using TempDir view = new();
        using TempDir app = new();
        using TempDir library = new();
        using TempDir unloadable = new();
        using TempDir withoutProject = new();
        _ = library.Write("Library.csproj", $"<Project><PropertyGroup><AssemblyName>{LibraryAssembly}</AssemblyName></PropertyGroup></Project>");
        _ = unloadable.Write("NoSuchAssembly.csproj", "<Project />");
        ProjectSet projects = ProjectSet.Load(Options(view, app, AppAssembly, library, unloadable, withoutProject));

        IReadOnlyList<PreviewRenderer> renderers = projects.CreateRenderers(DesignServices.None);

        Assert.Equal([ViewAssembly, AppAssembly, LibraryAssembly], renderers.Select(r => r.AssemblyName));
        Assert.Equal([view.Path, app.Path, library.Path], renderers.Select(r => r.ProjectDirectory));
    }

    [Fact]
    public void CreateRenderers_AppAssemblyWithoutAppProject_GivesOnlyTheView()
    {
        using TempDir view = new();
        ProjectSet projects = ProjectSet.Load(Options(view, appAssembly: AppAssembly));

        IReadOnlyList<PreviewRenderer> renderers = projects.CreateRenderers(DesignServices.None);

        PreviewRenderer renderer = Assert.Single(renderers);
        Assert.Equal(view.Path, renderer.ProjectDirectory);
    }

    [Fact]
    public void CreateViewRenderer_GivesTheRendererOfTheViewProject()
    {
        using TempDir view = new();
        ProjectSet projects = ProjectSet.Load(Options(view));

        PreviewRenderer renderer = projects.CreateViewRenderer(DesignServices.None);

        Assert.Equal(ViewAssembly, renderer.AssemblyName);
        Assert.Equal(view.Path, renderer.ProjectDirectory);
    }

    private static HostOptions Options(
        TempDir view, TempDir? app = null, string? appAssembly = null, params TempDir[] referenced) =>
        new(Path.Combine(view.Path, "Main.axaml"), view.Path, ViewAssembly, appAssembly, "Loader.dll", null, null)
        {
            AppProjectDirectory = app?.Path,
            ReferencedProjects = referenced.Length == 0 ? null : string.Join(Path.PathSeparator, referenced.Select(r => r.Path)),
        };
}
