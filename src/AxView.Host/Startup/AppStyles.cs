using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

using AxView.Host.Rendering;

namespace AxView.Host.Startup;

/// <summary>Owns the import of the app styles and each live copy. An include reload updates all live copies.</summary>
/// <remarks>
/// A style object can have only one owner. Thus each owner gets its own copy:
/// the preview scope, <c>Application.Current</c>, the chrome fallback, and each snapshot window.
/// A copy stays live while its owner keeps its resources. A closed snapshot window does not stay in memory.
/// </remarks>
/// <param name="firstImport">The import that ran the startup of the app. The first copy is this import.</param>
/// <param name="import">Gives a new import of the app styles for each further copy.</param>
/// <param name="renderers">
/// The renderers of the project set, in order of preference: the view project, the app project, and the referenced projects.
/// </param>
internal sealed class AppStyles(TargetStyles firstImport, Func<TargetStyles> import, IReadOnlyList<PreviewRenderer> renderers)
{
    private readonly (PreviewRenderer Renderer, string Path)? appFile = FindAppFile(renderers);
    private readonly ConditionalWeakTable<IResourceDictionary, AppIncludes> liveCopies = [];
    private TargetStyles? unclaimedImport = firstImport;

    /// <summary>Gets the folders whose AXAML files can be includes of the app. Empty when no project has an app file.</summary>
    public IEnumerable<string> ProjectDirectories =>
        appFile is null ? [] : renderers.Select(r => r.ProjectDirectory).Distinct();

    /// <summary>Imports the app styles of the projects. Runs the startup of the app only for the first import.</summary>
    /// <param name="projects">The projects of the preview.</param>
    /// <param name="services">The design services of the renderers.</param>
    /// <param name="notes">Gets a note about the first import.</param>
    /// <param name="runStartup">True to call OnFrameworkInitializationCompleted of the app.</param>
    public static AppStyles Import(ProjectSet projects, DesignServices services, List<string> notes, bool runStartup) =>
        new(
            TargetAppImporter.Import(projects, notes, runStartup),
            () => TargetAppImporter.Import(projects, []),
            projects.CreateRenderers(services));

    /// <summary>Gives a live copy of the app styles. The caller becomes its owner.</summary>
    public TargetStyles CreateCopy()
    {
        TargetStyles copy = unclaimedImport ?? import();
        unclaimedImport = null;
        if (appFile is not null)
        {
            AppIncludes includes = new(appFile.Value.Renderer, appFile.Value.Path, copy.Styles, copy.Resources, renderers);
            liveCopies.Add(copy.Resources, includes);
        }

        return copy;
    }

    /// <summary>Gives the app a live copy of the app resources only.</summary>
    /// <remarks>Code that reads Application.Current finds them. For example, a StaticResource of a style root does.</remarks>
    /// <param name="app">The app that gets the resources.</param>
    public void AddResourcesTo(Application app) => app.Styles.Add(new Styles { Resources = CreateCopy().Resources });

    /// <summary>Gives the app a live copy of the app styles, resources, and data templates.</summary>
    /// <param name="app">The app that gets the copy.</param>
    public void AddTo(Application app) => CreateCopy().ApplyTo(app);

    /// <summary>Loads each changed include file into each live copy.</summary>
    /// <returns>The result for all copies. Each error occurs only once.</returns>
    public IncludeUpdate Update()
    {
        bool isChanged = false;
        List<string> errors = [];
        foreach (KeyValuePair<IResourceDictionary, AppIncludes> copy in liveCopies)
        {
            IncludeUpdate update = copy.Value.Update();
            isChanged |= update.IsChanged;
            if (update.Error is not null && !errors.Contains(update.Error))
            {
                errors.Add(update.Error);
            }
        }

        return new IncludeUpdate(isChanged, errors.Count == 0 ? null : string.Join(Environment.NewLine, errors));
    }

    private static (PreviewRenderer Renderer, string Path)? FindAppFile(IEnumerable<PreviewRenderer> renderers)
    {
        foreach (PreviewRenderer renderer in renderers)
        {
            string? path = AppIncludes.FindAppFile(renderer.ProjectDirectory);
            if (path is not null)
            {
                return (renderer, path);
            }
        }

        return null;
    }
}
