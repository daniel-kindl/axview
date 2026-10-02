using System.Xml;
using System.Xml.Linq;

using Avalonia.Markup.Xaml;

namespace AxView.Host.Rendering;

/// <summary>Finds the include files of a shown file that changed after the build. The loader uses them in place of the compiled includes.</summary>
/// <remarks>
/// The loader puts the compiled content of each <c>StyleInclude</c> and <c>ResourceInclude</c> in place of the include.
/// A <c>StaticResource</c> gets its value during the load. Thus the changed files must be part of the load.
/// <list type="bullet">
/// <item>The search looks at each include element of the file, also in child controls.</item>
/// <item>A file changed when its text is different from the text of the build.</item>
/// <item>A file also changed when one of its own include files changed. Its compiled content has the old include.</item>
/// <item>A changed file that does not load is not part of the load. The search keeps its error.</item>
/// </list>
/// </remarks>
/// <param name="projects">The renderers of the projects that an include can point into. The first is the view project.</param>
/// <param name="buildTexts">
/// The text of the build of each file, by full path. The first read of a file that is not in it adds its text.
/// </param>
internal sealed class ChangedIncludes(IReadOnlyList<PreviewRenderer> projects, Dictionary<string, string> buildTexts) : IChangedIncludes
{
    private readonly IncludeFiles files = new(projects);

    // The text of each include file that the last search read, by full path.
    private readonly Dictionary<string, string> readTexts = new(StringComparer.Ordinal);

    // For each include file of the last search, true when it is part of the load.
    private readonly Dictionary<string, bool> isChangedFiles = new(StringComparer.Ordinal);

    // The changed files of the last search. A loader document reads its text only once. Thus make new documents for each load.
    private readonly List<(PreviewRenderer Project, string Path, string Text)> changedFiles = [];

    /// <summary>Gets the errors of the include files of the last search, one per line. Null when there is none.</summary>
    public string? Error => files.Error;

    /// <inheritdoc/>
    public IReadOnlyList<RuntimeXamlLoaderDocument> FindChanged(string path, string xaml)
    {
        files.ClearErrors();
        readTexts.Clear();
        isChangedFiles.Clear();
        changedFiles.Clear();
        string fullPath = Path.GetFullPath(path);
        isChangedFiles[fullPath] = false;
        XElement? root = ParseRoot(xaml);
        if (root is not null)
        {
            _ = HasChangedIncludes(root, fullPath);
        }

        return CreateDocuments();
    }

    /// <summary>Tells if an include file of the last search has another text on disk now.</summary>
    /// <remarks>A file that the host cannot read now also gives true. Then the next search reports the error.</remarks>
    public bool IsOutdated() => readTexts.Any(file => ReadOrNull(file.Key) != file.Value);

    // The loader reports the errors of the shown file itself.
    private static XElement? ParseRoot(string xaml)
    {
        try
        {
            return XDocument.Parse(xaml).Root;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static string? ReadOrNull(string path)
    {
        try
        {
            return PreviewRenderer.ReadWithRetry(path);
        }
        catch (IOException)
        {
            return null;
        }
    }

    // Each include must be visited. Thus do not stop at the first changed include.
    private bool HasChangedIncludes(XElement root, string file) =>
        root.Descendants().Where(IncludeFiles.IsInclude).Select(include => IsChanged(include, file)).ToList().Contains(true);

    private bool IsChanged(XElement include, string file)
    {
        (PreviewRenderer Project, string Path)? source = files.FindSource(include, file);
        if (source is null)
        {
            return false;
        }

        (PreviewRenderer project, string path) = source.Value;
        if (isChangedFiles.TryGetValue(path, out bool isKnownChanged))
        {
            return isKnownChanged;
        }

        // A file that includes itself must not cause an endless search.
        isChangedFiles[path] = false;
        string? text = files.Read(path);
        if (text is null)
        {
            return false;
        }

        readTexts[path] = text;
        _ = buildTexts.TryAdd(path, text);
        XElement? element = files.Parse(path, text);
        bool isChanged = element is not null && (HasChangedIncludes(element, path) || text != buildTexts[path]);
        if (!isChanged || !CanLoad(project, path, text))
        {
            return false;
        }

        changedFiles.Add((project, path, text));
        isChangedFiles[path] = true;
        return true;
    }

    private List<RuntimeXamlLoaderDocument> CreateDocuments() => [.. changedFiles.Select(f => f.Project.Prepare(f.Path, f.Text))];

    // The changed files that the file includes are already in the list.
    private bool CanLoad(PreviewRenderer project, string path, string text)
    {
        try
        {
            _ = project.Load(path, text, includes: CreateDocuments());
            return true;
        }
        catch (Exception ex)
        {
            // User XAML and its code can throw any exception. Keep the compiled include.
            files.AddError(path, ex);
            return false;
        }
    }
}
