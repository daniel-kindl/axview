using AxView.Protocol;

namespace AxView.Host.Catalog;

/// <summary>Lists the AXAML files that belong to one project.</summary>
/// <remarks>
/// A folder with its own .csproj belongs to another project. Build output and hidden folders are not source.
/// </remarks>
internal static class AxamlCatalog
{
    public static IReadOnlyList<string> List(string projectDirectory)
    {
        string root = Path.GetFullPath(projectDirectory);
        IEnumerable<string> files = ProjectSources.EnumerateFiles(root, "*.axaml");
        return [.. files
            .OrderBy(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'), StringComparer.OrdinalIgnoreCase)];
    }
}
