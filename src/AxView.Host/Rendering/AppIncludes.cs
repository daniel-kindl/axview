using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

using Avalonia.Controls;
using Avalonia.Styling;

using AxView.Host.Catalog;

namespace AxView.Host.Rendering;

/// <summary>
/// Loads the changed include files of the app styles and resources from disk. Puts the result in place of the compiled include.
/// </summary>
/// <remarks>
/// The XAML compiler puts the content of each <c>StyleInclude</c> and <c>ResourceInclude</c> in place of the include.
/// Thus the runtime objects do not know their source file.
/// This class walks the XAML of App.axaml and of each include file side by side with the runtime objects:
/// <list type="bullet">
/// <item>The children of <c>Application.Styles</c> and of a <c>Styles</c> root agree with the runtime styles by position.</item>
/// <item>The children of <c>ResourceDictionary.MergedDictionaries</c> agree with the merged dictionaries by position.</item>
/// <item>The children of <c>ResourceDictionary.ThemeDictionaries</c> agree with the theme dictionaries by <c>x:Key</c>.</item>
/// </list>
/// When the counts are different, the walk skips that branch.
/// The first text of each file is the text of the build.
/// A file loads again when its text is different from the text of the object that is in its place.
/// An include can point into another project of the app. Each project has its own renderer.
/// The walk finds the project of an include by its avares URI. It resolves a relative URI against the project of its file.
/// </remarks>
internal sealed class AppIncludes
{
    private const string ApplicationRoot = "Application";
    private const string StylesRoot = "Styles";
    private const string ResourceDictionaryRoot = "ResourceDictionary";
    private const string SourceAttribute = "Source";
    private static readonly XName keyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    private readonly PreviewRenderer renderer;
    private readonly List<PreviewRenderer> projects;
    private readonly string appPath;
    private readonly Styles styles;
    private readonly IResourceDictionary resources;
    private readonly Dictionary<string, string> buildTexts = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<object, string> loadedTexts = [];
    private readonly List<string> errors = [];
    private bool isChanged;

    /// <summary>Reads App.axaml and each include file. Keeps their text as the text of the build.</summary>
    /// <param name="renderer">The renderer of the project. It loads the changed files.</param>
    /// <param name="appPath">The App.axaml file of the project.</param>
    /// <param name="styles">The styles of the app. They must agree with the build of App.axaml.</param>
    /// <param name="resources">The resources of the app. They must agree with the build of App.axaml.</param>
    /// <param name="otherProjects">The renderers of the other projects that an include can point into.</param>
    public AppIncludes(
        PreviewRenderer renderer,
        string appPath,
        Styles styles,
        IResourceDictionary resources,
        IEnumerable<PreviewRenderer>? otherProjects = null)
    {
        this.renderer = renderer;
        projects = [renderer, .. otherProjects ?? []];
        this.appPath = Path.GetFullPath(appPath);
        this.styles = styles;
        this.resources = resources;
        _ = Update();
    }

    /// <summary>Gets the folder of the project that contains the app file.</summary>
    public string ProjectDirectory => renderer.ProjectDirectory;

    /// <summary>Gets the folders of all projects that an include can point into. The folder of the app file is first.</summary>
    public IEnumerable<string> ProjectDirectories => projects.Select(p => p.ProjectDirectory).Distinct();

    /// <summary>Finds the app file in the project of the first renderer that has one, and reads its includes.</summary>
    /// <param name="renderers">
    /// The renderers of the projects that can contain the app file, in order of preference.
    /// For a library view, these are the renderers of the view project and of the app project.
    /// </param>
    /// <param name="styles">The styles of the app. They must agree with the build of App.axaml.</param>
    /// <param name="resources">The resources of the app. They must agree with the build of App.axaml.</param>
    /// <param name="libraries">The renderers of the referenced projects. An include can point into them.</param>
    /// <returns>The includes of the app file, or null when no project has an app file.</returns>
    public static AppIncludes? Find(
        IEnumerable<PreviewRenderer> renderers,
        Styles styles,
        IResourceDictionary resources,
        IEnumerable<PreviewRenderer>? libraries = null)
    {
        List<PreviewRenderer> candidates = [.. renderers];
        foreach (PreviewRenderer renderer in candidates)
        {
            string? appPath = FindAppFile(renderer.ProjectDirectory);
            if (appPath is not null)
            {
                return new AppIncludes(renderer, appPath, styles, resources, candidates.Concat(libraries ?? []));
            }
        }

        return null;
    }

    /// <summary>Gives the AXAML file of the project with an <c>Application</c> root.</summary>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <returns>The full path of the file, or null when the project has no such file.</returns>
    public static string? FindAppFile(string projectDirectory) => AxamlCatalog.List(projectDirectory).FirstOrDefault(IsApplication);

    /// <summary>Loads each include file whose text changed, and puts the result in place of the old include.</summary>
    /// <remarks>A file that does not load keeps its old include.</remarks>
    public IncludeUpdate Update()
    {
        isChanged = false;
        errors.Clear();
        XElement? app = Parse(appPath, Read(appPath));
        if (app is not null)
        {
            WalkStyles(Property(app, "Application.Styles"), appPath, styles);
            WalkResources(ResourceContent(Property(app, "Application.Resources")), appPath, resources);
        }

        return new IncludeUpdate(isChanged, errors.Count == 0 ? null : string.Join(Environment.NewLine, errors));
    }

    private static bool IsApplication(string path)
    {
        try
        {
            using XmlReader reader = XmlReader.Create(path);
            return reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == ApplicationRoot;
        }
        catch (Exception ex) when (ex is XmlException or IOException)
        {
            // A broken or locked file is not the app file of a project that builds.
            return false;
        }
    }

    private static XElement? Property(XElement element, string name) =>
        element.Elements().FirstOrDefault(e => e.Name.LocalName == name);

    // The value of a resources property is one ResourceDictionary element, or the content of an implicit dictionary.
    private static XElement? ResourceContent(XElement? property)
    {
        List<XElement> children = [.. property?.Elements() ?? []];
        return children is [{ Name.LocalName: ResourceDictionaryRoot } dictionary] ? dictionary : property;
    }

    private static T As<T>(object loaded) =>
        loaded is T value ? value : throw new NotSupportedException($"The root is a {loaded.GetType().Name}, not a {typeof(T).Name}.");

    private void WalkStyles(XElement? element, string file, Styles list)
    {
        List<XElement> children = [.. element?.Elements().Where(e => !e.Name.LocalName.Contains('.')) ?? []];
        if (children.Count != list.Count)
        {
            return;
        }

        for (int i = 0; i < children.Count; i++)
        {
            int index = i;
            XElement child = children[index];
            if (child.Name.LocalName == "StyleInclude")
            {
                Include(child, file, new IncludeSlot(() => list[index], loaded => list[index] = As<IStyle>(loaded)));
            }
            else if (child.Name.LocalName == StylesRoot && list[index] is Styles inline)
            {
                WalkStylesRoot(child, file, inline);
            }
        }
    }

    private void WalkStylesRoot(XElement root, string file, Styles runtime)
    {
        WalkStyles(root, file, runtime);
        WalkResources(ResourceContent(Property(root, "Styles.Resources")), file, runtime.Resources);
    }

    private void WalkResources(XElement? element, string file, IResourceDictionary dictionary)
    {
        if (element is null)
        {
            return;
        }

        IList<IResourceProvider> merged = dictionary.MergedDictionaries;
        List<XElement> mergedChildren = [.. Property(element, "ResourceDictionary.MergedDictionaries")?.Elements() ?? []];
        if (mergedChildren.Count == merged.Count)
        {
            for (int i = 0; i < mergedChildren.Count; i++)
            {
                int index = i;
                WalkResource(mergedChildren[index], file, new IncludeSlot(() => merged[index], loaded => merged[index] = As<IResourceProvider>(loaded)));
            }
        }

        IDictionary<ThemeVariant, IThemeVariantProvider> themes = dictionary.ThemeDictionaries;
        foreach (XElement child in Property(element, "ResourceDictionary.ThemeDictionaries")?.Elements() ?? [])
        {
            string? key = child.Attribute(keyAttribute)?.Value;
            ThemeVariant? variant = themes.Keys.FirstOrDefault(v => v.Key.ToString() == key);
            if (variant is not null)
            {
                WalkResource(child, file, new IncludeSlot(() => themes[variant], loaded => themes[variant] = As<IThemeVariantProvider>(loaded)));
            }
        }
    }

    private void WalkResource(XElement child, string file, IncludeSlot slot)
    {
        if (child.Name.LocalName is "ResourceInclude" or "MergeResourceInclude")
        {
            Include(child, file, slot);
        }
        else if (child.Name.LocalName == ResourceDictionaryRoot && slot.Get() is IResourceDictionary inline)
        {
            WalkResources(child, file, inline);
        }
    }

    private void Include(XElement element, string file, IncludeSlot slot)
    {
        (PreviewRenderer Project, string Path)? source = FindSource(element, file);
        string? text = source is null ? null : Read(source.Value.Path);
        if (source is null || text is null)
        {
            return;
        }

        (PreviewRenderer project, string path) = source.Value;

        _ = buildTexts.TryAdd(path, text);
        object shown = slot.Get();
        string shownText = loadedTexts.TryGetValue(shown, out string? loadedText) ? loadedText : buildTexts[path];
        if (text != shownText)
        {
            try
            {
                shown = project.Load(path, text);
                slot.Set(shown);
            }
            catch (Exception ex)
            {
                // User XAML and its code can throw any exception. Keep the old include.
                AddError(path, ex);
                return;
            }

            loadedTexts.AddOrUpdate(shown, text);
            isChanged = true;
        }

        WalkFile(path, text, shown);
    }

    private void WalkFile(string path, string text, object root)
    {
        XElement? element = Parse(path, text);
        if (element?.Name.LocalName == StylesRoot && root is Styles runtimeStyles)
        {
            WalkStylesRoot(element, path, runtimeStyles);
        }
        else if (element?.Name.LocalName == ResourceDictionaryRoot && root is IResourceDictionary dictionary)
        {
            WalkResources(element, path, dictionary);
        }
    }

    private (PreviewRenderer Project, string Path)? FindSource(XElement element, string file)
    {
        string? source = element.Attribute(SourceAttribute)?.Value;
        PreviewRenderer fileProject = ProjectOf(file);
        Uri baseUri = AvaresUri.For(fileProject.AssemblyName, fileProject.ProjectDirectory, file);
        if (source is null || !Uri.TryCreate(baseUri, source, out Uri? uri))
        {
            return null;
        }

        foreach (PreviewRenderer project in projects)
        {
            string? path = AvaresUri.ToPath(uri, project.AssemblyName, project.ProjectDirectory);
            if (path is not null)
            {
                return (project, path);
            }
        }

        return null;
    }

    // The project with the longest folder that contains the file. A referenced project can sit inside another project.
    private PreviewRenderer ProjectOf(string file) =>
        projects
            .Where(p => file.StartsWith(Path.GetFullPath(p.ProjectDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.ProjectDirectory.Length)
            .FirstOrDefault() ?? renderer;

    private string? Read(string path)
    {
        try
        {
            return PreviewRenderer.ReadWithRetry(path);
        }
        catch (IOException ex)
        {
            AddError(path, ex);
            return null;
        }
    }

    private XElement? Parse(string path, string? text)
    {
        if (text is null)
        {
            return null;
        }

        try
        {
            return XDocument.Parse(text).Root;
        }
        catch (XmlException ex)
        {
            AddError(path, ex);
            return null;
        }
    }

    private void AddError(string path, Exception ex) =>
        errors.Add($"{Path.GetRelativePath(ProjectOf(path).ProjectDirectory, path)}: {ExceptionText.Describe(ex)}");
}
