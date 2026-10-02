using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

using AxView.Host.Catalog;
using AxView.Host.Rendering;

namespace AxView.Host.Startup;

/// <summary>Owns the import of the app styles and each live copy. A reload of the app file or of an include updates all live copies.</summary>
/// <remarks>
/// A style object can have only one owner. Thus each owner gets its own copy:
/// the preview scope, <c>Application.Current</c>, the chrome fallback, and each snapshot window.
/// A copy stays live while its owner keeps its resources. A closed snapshot window does not stay in memory.
/// A changed app file loads into a new instance of the app type.
/// The styles, resources, data templates, and theme variant of the copies change.
/// </remarks>
/// <param name="firstImport">The import that ran the startup of the app. The first copy is this import.</param>
/// <param name="import">Gives a new import of the app styles for each further copy.</param>
/// <param name="renderers">
/// The renderers of the project set, in order of preference: the view project, the app project, and the referenced projects.
/// </param>
/// <param name="appType">The type of the target app. Null when there is none. Then a change of the app file does not load.</param>
internal sealed class AppStyles(
    TargetStyles firstImport, Func<TargetStyles> import, IReadOnlyList<PreviewRenderer> renderers, Type? appType = null)
{
    private readonly (PreviewRenderer Renderer, string Path)? appFile = FindAppFile(renderers);
    private readonly ConditionalWeakTable<IResourceDictionary, LiveCopy> liveCopies = [];
    private readonly Dictionary<string, string> buildTexts = ReadBuildTexts(renderers);
    private TargetStyles? unclaimedImport = firstImport;

    // The text of the app file that the copies show. Null until the first copy.
    private string? appText;

    /// <summary>Gets the folders whose AXAML files can be includes of the app or of a view.</summary>
    public IEnumerable<string> ProjectDirectories => renderers.Select(r => r.ProjectDirectory).Distinct();

    /// <summary>Imports the app styles of the projects. Runs the startup of the app only for the first import.</summary>
    /// <param name="projects">The projects of the preview.</param>
    /// <param name="services">The design services of the renderers.</param>
    /// <param name="notes">Gets a note about the first import.</param>
    /// <param name="runStartup">True to call OnFrameworkInitializationCompleted of the app.</param>
    public static AppStyles Import(ProjectSet projects, DesignServices services, List<string> notes, bool runStartup) =>
        new(
            TargetAppImporter.Import(projects, notes, runStartup),
            () => TargetAppImporter.Import(projects, []),
            projects.CreateRenderers(services),
            TargetAppImporter.FindAppType(projects));

    /// <summary>Gives a live copy of the app styles. The caller becomes its owner.</summary>
    /// <remarks>After a change of the app file, the copy shows the changed app file and the changed includes.</remarks>
    public TargetStyles CreateCopy()
    {
        TargetStyles copy = unclaimedImport ?? ImportCopy();
        unclaimedImport = null;
        if (appFile is not null)
        {
            (PreviewRenderer renderer, string path) = appFile.Value;
            if (appText is null)
            {
                appText = PreviewRenderer.ReadWithRetry(path);
                buildTexts[path] = appText;
            }

            AppIncludes includes = new(renderer, path, copy.Styles, copy.Resources, renderers, buildTexts);
            liveCopies.Add(copy.Resources, new LiveCopy(copy, includes));
        }

        return copy;
    }

    /// <summary>Loads the changed include files of a loaded root from disk. Puts the result in place of the compiled include.</summary>
    /// <param name="path">The file of the root.</param>
    /// <param name="root">The root that the file loads. See <see cref="AppIncludes"/>.</param>
    /// <returns>The includes of the root. Call <see cref="AppIncludes.Update"/> after a change of an AXAML file.</returns>
    public AppIncludes CreateIncludes(string path, object root) => new(renderers[0], path, root, renderers, buildTexts);

    /// <summary>Gives the app a live copy of the app resources only.</summary>
    /// <remarks>Code that reads Application.Current finds them. For example, a StaticResource of a style root does.</remarks>
    /// <param name="app">The app that gets the resources.</param>
    public void AddResourcesTo(Application app) => app.Styles.Add(new Styles { Resources = CreateCopy().Resources });

    /// <summary>Gives the app a live copy of the app styles, resources, and data templates.</summary>
    /// <param name="app">The app that gets the copy.</param>
    public void AddTo(Application app) => CreateCopy().ApplyTo(app);

    /// <summary>Loads a changed app file and each changed include file into each live copy.</summary>
    /// <remarks>An app file that does not load keeps the old styles in all copies.</remarks>
    /// <returns>The result for all copies. Each error occurs only once.</returns>
    public IncludeUpdate Update()
    {
        List<string> errors = [];
        bool isChanged = LoadAppFile(errors);
        foreach (KeyValuePair<IResourceDictionary, LiveCopy> copy in liveCopies)
        {
            IncludeUpdate update = copy.Value.Includes.Update();
            isChanged |= update.IsChanged;
            if (update.Error is not null && !errors.Contains(update.Error))
            {
                errors.Add(update.Error);
            }
        }

        return new IncludeUpdate(isChanged, errors.Count == 0 ? null : string.Join(Environment.NewLine, errors));
    }

    // A copy after a change of the app file shows the changed text, not the text of the build.
    private TargetStyles ImportCopy()
    {
        if (appFile is null || appType is null || appText is null || appText == buildTexts[appFile.Value.Path])
        {
            return import();
        }

        return TargetAppImporter.ImportXaml(appType, appFile.Value.Renderer, appFile.Value.Path, appText);
    }

    // Loads all copies before it changes one. Thus a failure keeps the same styles in all copies.
    private bool LoadAppFile(List<string> errors)
    {
        if (appFile is null || appType is null || appText is null)
        {
            return false;
        }

        (PreviewRenderer renderer, string path) = appFile.Value;
        string text;
        try
        {
            text = PreviewRenderer.ReadWithRetry(path);
        }
        catch (IOException)
        {
            // The include update of each copy reads the app file too, and reports the error.
            return false;
        }

        if (text == appText)
        {
            return false;
        }

        List<(TargetStyles Copy, TargetStyles Loaded)> loads = [];
        try
        {
            foreach (KeyValuePair<IResourceDictionary, LiveCopy> copy in liveCopies)
            {
                loads.Add((copy.Value.Styles, TargetAppImporter.ImportXaml(appType, renderer, path, text)));
            }
        }
        catch (Exception ex)
        {
            // User XAML and its code can throw any exception. Keep the old styles.
            errors.Add($"{Path.GetRelativePath(renderer.ProjectDirectory, path)}: {ExceptionText.Describe(ex)}");
            return false;
        }

        foreach ((TargetStyles copy, TargetStyles loaded) in loads)
        {
            copy.ReplaceWith(loaded);
        }

        appText = text;
        return true;
    }

    // The host starts after the build. Thus the text of each file at the start is the text of the build.
    private static Dictionary<string, string> ReadBuildTexts(IEnumerable<PreviewRenderer> renderers)
    {
        Dictionary<string, string> texts = new(StringComparer.Ordinal);
        foreach (string path in renderers.SelectMany(r => AxamlCatalog.List(r.ProjectDirectory)))
        {
            try
            {
                _ = texts.TryAdd(path, PreviewRenderer.ReadWithRetry(path));
            }
            catch (IOException)
            {
                // The include walk reads the file again on its first use, and reports the error.
            }
        }

        return texts;
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
