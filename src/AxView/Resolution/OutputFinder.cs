namespace AxView.Resolution;

/// <summary>Finds an executable Avalonia app output that contains a given assembly.</summary>
/// <remarks>
/// A view often lives in a class library. A library output has no runtimeconfig and no platform
/// backend, so the finder searches the repository for an app that ships the library.
/// </remarks>
internal static class OutputFinder
{
    private const string PlatformMarker = "Avalonia.Desktop.dll";

    public static AppOutput Find(string projectPath, string assemblyName)
    {
        string root = FindSearchRoot(Path.GetDirectoryName(Path.GetFullPath(projectPath))!);
        EnumerationOptions options = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };

        AppOutput? best = null;
        DateTime bestTime = DateTime.MinValue;
        foreach (string dll in Directory.EnumerateFiles(root, assemblyName + ".dll", options))
        {
            AppOutput? candidate = TryRead(Path.GetDirectoryName(dll)!);
            DateTime time = File.GetLastWriteTimeUtc(dll);
            if (candidate is not null && time > bestTime)
            {
                best = candidate;
                bestTime = time;
            }
        }

        return best ?? throw new PreviewSetupException(
            $"No built Avalonia app under '{root}' contains '{assemblyName}.dll'. " +
            "Build the app project, or pass --output <dir>.");
    }

    public static AppOutput FromDirectory(string directory, string assemblyName)
    {
        string full = Path.GetFullPath(directory);
        if (!File.Exists(Path.Combine(full, assemblyName + ".dll")))
        {
            throw new PreviewSetupException($"'{full}' does not contain '{assemblyName}.dll'.");
        }

        return TryRead(full) ?? throw new PreviewSetupException(
            $"'{full}' is not an executable Avalonia app output. It needs a runtimeconfig, a deps file, and {PlatformMarker}.");
    }

    private static AppOutput? TryRead(string directory)
    {
        if (!File.Exists(Path.Combine(directory, PlatformMarker)))
        {
            return null;
        }

        foreach (string runtimeConfig in Directory.EnumerateFiles(directory, "*.runtimeconfig.json"))
        {
            AppOutput candidate = AppOutput.FromRuntimeConfig(runtimeConfig);
            if (File.Exists(candidate.DepsFile))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string FindSearchRoot(string projectDir)
    {
        for (DirectoryInfo? dir = new(projectDir); dir is not null; dir = dir.Parent)
        {
            bool isRoot = Directory.Exists(Path.Combine(dir.FullName, ".git"))
                || Directory.EnumerateFiles(dir.FullName, "*.sln").Any()
                || Directory.EnumerateFiles(dir.FullName, "*.slnx").Any();
            if (isRoot)
            {
                return dir.FullName;
            }
        }

        return Directory.GetParent(projectDir)?.FullName ?? projectDir;
    }
}
