namespace AxView.Host;

/// <summary>Lists the AXAML files that belong to one project.</summary>
/// <remarks>
/// A folder with its own .csproj belongs to another project. Build output and hidden folders are not source.
/// </remarks>
internal static class AxamlCatalog
{
    private static readonly HashSet<string> skippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules" };

    public static IReadOnlyList<string> List(string projectDirectory)
    {
        string root = Path.GetFullPath(projectDirectory);
        List<string> files = [];
        Collect(root, files);
        return [.. files
            .OrderBy(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'), StringComparer.OrdinalIgnoreCase)];
    }

    private static void Collect(string directory, List<string> files)
    {
        files.AddRange(Directory.EnumerateFiles(directory, "*.axaml"));
        foreach (string child in Directory.EnumerateDirectories(directory))
        {
            string name = Path.GetFileName(child);
            bool isSource = !name.StartsWith('.')
                && !skippedFolders.Contains(name)
                && !Directory.EnumerateFiles(child, "*.csproj").Any();
            if (isSource)
            {
                Collect(child, files);
            }
        }
    }
}
