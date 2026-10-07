namespace AxView.Protocol;

/// <summary>Tells which folders and files of a project are source.</summary>
/// <remarks>Build output and hidden folders are not source.</remarks>
internal static class ProjectSources
{
    private const string CodeExtension = ".cs";
    private const string AxamlExtension = ".axaml";

    private static readonly HashSet<string> skippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules" };

    /// <summary>Tells if a folder with this name can contain source.</summary>
    /// <param name="name">The name of the folder, not its path.</param>
    public static bool IsSourceFolder(string name) => !name.StartsWith('.') && !skippedFolders.Contains(name);

    /// <summary>Tells if the file is a C# file of the project, and not in build output.</summary>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <param name="path">The full path of the file.</param>
    public static bool IsCodeFile(string projectDirectory, string path) => IsSourceFile(projectDirectory, path, CodeExtension);

    /// <summary>Tells if the file is an AXAML file of the project, and not in build output.</summary>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <param name="path">The full path of the file.</param>
    public static bool IsAxamlFile(string projectDirectory, string path) => IsSourceFile(projectDirectory, path, AxamlExtension);

    /// <summary>Gives the source files of the project that match the pattern.</summary>
    /// <remarks>A folder with its own .csproj belongs to another project.</remarks>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <param name="searchPattern">The file name pattern, for example <c>*.axaml</c>.</param>
    public static IEnumerable<string> EnumerateFiles(string projectDirectory, string searchPattern)
    {
        IEnumerable<string> files = Directory.EnumerateFiles(projectDirectory, searchPattern);
        IEnumerable<string> childFiles = Directory.EnumerateDirectories(projectDirectory)
            .Where(child => IsSourceFolder(Path.GetFileName(child)) && !Directory.EnumerateFiles(child, "*.csproj").Any())
            .SelectMany(child => EnumerateFiles(child, searchPattern));
        return files.Concat(childFiles);
    }

    /// <summary>Gives the last write time of the newest C# file of the project, in UTC.</summary>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <returns><see cref="DateTime.MinValue"/> when the project has no C# file.</returns>
    public static DateTime GetLastCodeWriteTime(string projectDirectory) =>
        EnumerateFiles(projectDirectory, "*" + CodeExtension).Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();

    private static bool IsSourceFile(string projectDirectory, string path, string extension)
    {
        if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string relative = Path.GetRelativePath(projectDirectory, path);
        string[] folders = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[..^1];
        return folders.All(IsSourceFolder);
    }
}
