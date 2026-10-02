namespace AxView.Host.Catalog;

/// <summary>Tells which folders and files of a project are source.</summary>
/// <remarks>Build output and hidden folders are not source.</remarks>
internal static class ProjectSources
{
    private const string CodeExtension = ".cs";

    private static readonly HashSet<string> skippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules" };

    /// <summary>Tells if a folder with this name can contain source.</summary>
    /// <param name="name">The name of the folder, not its path.</param>
    public static bool IsSourceFolder(string name) => !name.StartsWith('.') && !skippedFolders.Contains(name);

    /// <summary>Tells if the file is a C# file of the project, and not in build output.</summary>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <param name="path">The full path of the file.</param>
    public static bool IsCodeFile(string projectDirectory, string path)
    {
        if (!path.EndsWith(CodeExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string relative = Path.GetRelativePath(projectDirectory, path);
        string[] folders = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[..^1];
        return folders.All(IsSourceFolder);
    }
}
