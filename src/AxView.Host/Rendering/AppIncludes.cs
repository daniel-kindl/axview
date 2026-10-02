using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

using AxView.Host.Catalog;

namespace AxView.Host.Rendering;

/// <summary>
/// Loads the changed include files of the app styles or of a loaded root from disk.
/// Puts the result in place of the compiled include.
/// </summary>
/// <remarks>
/// The XAML compiler puts the content of each <c>StyleInclude</c> and <c>ResourceInclude</c> in place of the include.
/// The runtime loader also does this for a view. Thus the runtime objects do not know their source file.
/// This class walks the XAML of the root file and of each include file side by side with the runtime objects:
/// <list type="bullet">
/// <item>The root file is App.axaml, a control, a <c>Styles</c>, a <c>Style</c>, or a <c>ResourceDictionary</c>.</item>
/// <item>The walk looks only at the <c>Styles</c> and <c>Resources</c> of a control root, not into its children.</item>
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
    private const string StyleRoot = "Style";
    private const string ResourceDictionaryRoot = "ResourceDictionary";
    private const string SourceAttribute = "Source";
    private static readonly XName keyAttribute = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    private readonly PreviewRenderer renderer;
    private readonly List<PreviewRenderer> projects;
    private readonly string rootPath;

    // The loaded root of the file. For App.axaml, the styles of the app.
    private readonly object root;

    // The resources of the app. Null when the file is not App.axaml.
    private readonly IResourceDictionary? appResources;
    private readonly Dictionary<string, string> buildTexts;
    private readonly ConditionalWeakTable<object, string> loadedTexts = [];
    private readonly List<string> errors = [];
    private bool isChanged;

    /// <summary>Reads App.axaml and each include file. Keeps their text as the text of the build.</summary>
    /// <param name="renderer">The renderer of the project. It loads the changed files.</param>
    /// <param name="appPath">The App.axaml file of the project.</param>
    /// <param name="styles">The styles of the app. They must agree with the build of App.axaml.</param>
    /// <param name="resources">The resources of the app. They must agree with the build of App.axaml.</param>
    /// <param name="otherProjects">The renderers of the other projects that an include can point into.</param>
    /// <param name="buildTexts">
    /// The text of the build of each file, by full path. Give the same dictionary to each copy of the app styles.
    /// Then a copy that starts after a change of a file knows that its include of the file is old.
    /// </param>
    public AppIncludes(
        PreviewRenderer renderer,
        string appPath,
        Styles styles,
        IResourceDictionary resources,
        IEnumerable<PreviewRenderer>? otherProjects = null,
        Dictionary<string, string>? buildTexts = null)
        : this(renderer, appPath, (object)styles, resources, otherProjects, buildTexts)
    {
    }

    /// <summary>Reads a project file and each include file. Keeps their text as the text of the build.</summary>
    /// <param name="renderer">The renderer of the project. It loads the changed files.</param>
    /// <param name="path">The file of the root.</param>
    /// <param name="root">
    /// The root that the file loads: a control, a <c>Styles</c>, a <c>Style</c>, or a <c>ResourceDictionary</c>.
    /// It must agree with the build of the file.
    /// </param>
    /// <param name="otherProjects">The renderers of the other projects that an include can point into.</param>
    /// <param name="buildTexts">The text of the build of each file, by full path.</param>
    public AppIncludes(
        PreviewRenderer renderer,
        string path,
        object root,
        IEnumerable<PreviewRenderer>? otherProjects = null,
        Dictionary<string, string>? buildTexts = null)
        : this(renderer, path, root, null, otherProjects, buildTexts)
    {
    }

    private AppIncludes(
        PreviewRenderer renderer,
        string path,
        object root,
        IResourceDictionary? appResources,
        IEnumerable<PreviewRenderer>? otherProjects,
        Dictionary<string, string>? buildTexts)
    {
        this.renderer = renderer;
        this.buildTexts = buildTexts ?? new Dictionary<string, string>(StringComparer.Ordinal);
        projects = [renderer, .. otherProjects ?? []];
        rootPath = Path.GetFullPath(path);
        this.root = root;
        this.appResources = appResources;
        _ = Update();
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
        XElement? element = Parse(rootPath, Read(rootPath));
        if (element is not null && appResources is not null && root is Styles appStyles)
        {
            WalkOwner(element, rootPath, appStyles, appResources);
        }
        else if (element is not null)
        {
            WalkRoot(element, rootPath, root);
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

    private void WalkFile(string path, string text, object loaded)
    {
        XElement? element = Parse(path, text);
        if (element is not null)
        {
            WalkRoot(element, path, loaded);
        }
    }

    private void WalkRoot(XElement element, string file, object loaded)
    {
        string name = element.Name.LocalName;
        if (name == StylesRoot && loaded is Styles runtimeStyles)
        {
            WalkStylesRoot(element, file, runtimeStyles);
        }
        else if (name == ResourceDictionaryRoot && loaded is IResourceDictionary dictionary)
        {
            WalkResources(element, file, dictionary);
        }
        else if (name == StyleRoot && loaded is Style style)
        {
            WalkResources(ResourceContent(Property(element, "Style.Resources")), file, style.Resources);
        }
        else if (loaded is StyledElement owner)
        {
            WalkOwner(element, file, owner.Styles, owner.Resources);
        }
    }

    // The root of App.axaml or of a control. Its styles and resources are property elements with the name of the root.
    private void WalkOwner(XElement element, string file, Styles ownerStyles, IResourceDictionary ownerResources)
    {
        string name = element.Name.LocalName;
        WalkStyles(Property(element, $"{name}.Styles"), file, ownerStyles);
        WalkResources(ResourceContent(Property(element, $"{name}.Resources")), file, ownerResources);
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
