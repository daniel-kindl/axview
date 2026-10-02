using System.Reflection;

using AxView.Host.Rendering;
using AxView.Protocol;

namespace AxView.Host.Startup;

/// <summary>The view project, the app project, and the referenced projects of the preview, each with its assembly.</summary>
/// <remarks>
/// The host options name the assembly of the view project and of the app.
/// The assembly of a referenced project comes from its project file.
/// A referenced project whose assembly does not load has no renderer. Its code folder still starts a rebuild.
/// </remarks>
internal sealed class ProjectSet
{
    private readonly string viewDirectory;
    private readonly string? appDirectory;
    private readonly IReadOnlyList<(string Directory, Assembly Assembly)> referencedProjects;

    private ProjectSet(
        Assembly viewAssembly,
        string viewDirectory,
        Assembly? appAssembly,
        string? appDirectory,
        IReadOnlyList<string> referencedDirectories,
        IReadOnlyList<(string Directory, Assembly Assembly)> referencedProjects)
    {
        ViewAssembly = viewAssembly;
        this.viewDirectory = viewDirectory;
        AppAssembly = appAssembly;
        this.appDirectory = appDirectory;
        this.referencedProjects = referencedProjects;
        CodeDirectories = [viewDirectory, .. appDirectory is null ? [] : new[] { appDirectory }, .. referencedDirectories];
    }

    /// <summary>Gets the assembly of the view project.</summary>
    public Assembly ViewAssembly { get; }

    /// <summary>Gets the assembly of the target app. Null when the options name none, or when it does not load.</summary>
    public Assembly? AppAssembly { get; }

    /// <summary>Gets the folders whose C# files start a rebuild: the view project, the app project, and the referenced projects.</summary>
    public IReadOnlyList<string> CodeDirectories { get; }

    /// <summary>Loads the assemblies of the projects that the options name.</summary>
    /// <param name="options">The options of the host.</param>
    /// <exception cref="FileNotFoundException">The assembly of the view project does not load.</exception>
    public static ProjectSet Load(HostOptions options)
    {
        IReadOnlyList<string> referencedDirectories = options.ReferencedProjectDirectories;
        return new ProjectSet(
            Assembly.Load(options.AssemblyName),
            options.ProjectDirectory,
            TryLoad(options.AppAssemblyName),
            options.AppProjectDirectory,
            referencedDirectories,
            [.. LoadReferencedProjects(referencedDirectories)]);
    }

    /// <summary>Makes the renderer of the view project.</summary>
    /// <param name="services">The design services that create the ViewModels.</param>
    public PreviewRenderer CreateViewRenderer(DesignServices services) => new(ViewAssembly, viewDirectory, services);

    /// <summary>Makes a renderer for each project that has an assembly.</summary>
    /// <param name="services">The design services that create the ViewModels.</param>
    /// <returns>The renderers of the view project, of the app project, and of the referenced projects, in this order.</returns>
    public IReadOnlyList<PreviewRenderer> CreateRenderers(DesignServices services)
    {
        List<PreviewRenderer> renderers = [CreateViewRenderer(services)];
        if (appDirectory is not null && AppAssembly is not null)
        {
            renderers.Add(new PreviewRenderer(AppAssembly, appDirectory, services));
        }

        renderers.AddRange(referencedProjects.Select(p => new PreviewRenderer(p.Assembly, p.Directory, services)));
        return renderers;
    }

    private static IEnumerable<(string Directory, Assembly Assembly)> LoadReferencedProjects(IEnumerable<string> directories)
    {
        foreach (string directory in directories)
        {
            string? project = Directory.EnumerateFiles(directory, "*.csproj").FirstOrDefault();
            Assembly? assembly = project is null ? null : TryLoad(ProjectAssembly.ReadName(project));
            if (assembly is not null)
            {
                yield return (directory, assembly);
            }
        }
    }

    // A project that the app output does not contain has no assembly to load.
    private static Assembly? TryLoad(string? assemblyName)
    {
        if (assemblyName is null)
        {
            return null;
        }

        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
