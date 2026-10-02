using System.Xml;
using System.Xml.Linq;

namespace AxView.Host.Rendering;

/// <summary>Finds and reads the file of an include element. Keeps the errors of the reads.</summary>
/// <remarks>
/// An include can point into another project of the app. Each project has its own renderer.
/// This class finds the project of an include by its avares URI. It resolves a relative URI against the project of its file.
/// </remarks>
/// <param name="projects">The renderers of the projects that an include can point into. The first is the default project.</param>
internal sealed class IncludeFiles(IReadOnlyList<PreviewRenderer> projects)
{
    private const string SourceAttribute = "Source";

    private readonly List<string> errors = [];

    /// <summary>Gets the errors since the last <see cref="ClearErrors"/>, one per line. Null when there is none.</summary>
    public string? Error => errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);

    /// <summary>Tells if the element is a <c>StyleInclude</c>, a <c>ResourceInclude</c>, or a <c>MergeResourceInclude</c>.</summary>
    /// <param name="element">An element of an AXAML file.</param>
    public static bool IsInclude(XElement element) =>
        element.Name.LocalName is "StyleInclude" or "ResourceInclude" or "MergeResourceInclude";

    /// <summary>Removes all errors.</summary>
    public void ClearErrors() => errors.Clear();

    /// <summary>Gives the project and the full path of the file that an include element points to.</summary>
    /// <param name="include">The include element.</param>
    /// <param name="file">The file that contains the element.</param>
    /// <returns>Null when the element has no source, or when the source is in no project.</returns>
    public (PreviewRenderer Project, string Path)? FindSource(XElement include, string file)
    {
        string? source = include.Attribute(SourceAttribute)?.Value;
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

    /// <summary>Reads a file. Keeps the error when the read fails.</summary>
    /// <param name="path">The file.</param>
    /// <returns>The text of the file, or null when the read fails.</returns>
    public string? Read(string path)
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

    /// <summary>Parses the text of a file. Keeps the error when the text is not XML.</summary>
    /// <param name="path">The file of the text.</param>
    /// <param name="text">The text, or null when the read failed.</param>
    /// <returns>The root element, or null when there is no text or the text is not XML.</returns>
    public XElement? Parse(string path, string? text)
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

    /// <summary>Keeps an error of a file. The error starts with the path of the file in its project.</summary>
    /// <param name="path">The file.</param>
    /// <param name="ex">The cause of the error.</param>
    public void AddError(string path, Exception ex) =>
        errors.Add($"{Path.GetRelativePath(ProjectOf(path).ProjectDirectory, path)}: {ExceptionText.Describe(ex)}");

    // The project with the longest folder that contains the file. A referenced project can sit inside another project.
    private PreviewRenderer ProjectOf(string file) =>
        projects
            .Where(p => file.StartsWith(Path.GetFullPath(p.ProjectDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.ProjectDirectory.Length)
            .FirstOrDefault() ?? projects[0];
}
