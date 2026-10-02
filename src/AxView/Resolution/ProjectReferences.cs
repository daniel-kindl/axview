using System.Xml;
using System.Xml.Linq;

namespace AxView.Resolution;

/// <summary>Finds the projects that other projects reference.</summary>
/// <remarks>
/// A project file lists each reference as <c>&lt;ProjectReference Include="../Core/Core.csproj" /&gt;</c>.
/// The path is relative to the folder of the project. It can contain a backslash.
/// </remarks>
internal static class ProjectReferences
{
    private const string ReferenceElement = "ProjectReference";
    private const string IncludeAttribute = "Include";

    /// <summary>Gives the folders of all projects that the given projects reference, directly or not.</summary>
    /// <param name="projectPaths">The project files to start from. Their folders are not in the result.</param>
    /// <returns>Each folder once, in the order of the search. A reference to a missing file is skipped.</returns>
    public static IReadOnlyList<string> FindDirectories(IEnumerable<string> projectPaths)
    {
        HashSet<string> visited = [.. projectPaths.Select(Path.GetFullPath)];
        Queue<string> pending = new(visited);
        List<string> directories = [];
        while (pending.Count > 0)
        {
            foreach (string reference in ReadReferences(pending.Dequeue()))
            {
                if (visited.Add(reference))
                {
                    pending.Enqueue(reference);
                    directories.Add(Path.GetDirectoryName(reference)!);
                }
            }
        }

        return directories;
    }

    private static IEnumerable<string> ReadReferences(string projectPath)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(projectPath);
        }
        catch (Exception ex) when (ex is XmlException or IOException)
        {
            // A broken or missing project file has no references for us. The build reports the problem.
            return [];
        }

        string directory = Path.GetDirectoryName(projectPath)!;
        return document.Descendants()
            .Where(e => e.Name.LocalName == ReferenceElement)
            .Select(e => e.Attribute(IncludeAttribute)?.Value)
            .OfType<string>()
            .Select(include => Path.GetFullPath(Path.Combine(directory, include.Replace('\\', '/'))))
            .Where(File.Exists);
    }
}
