namespace AxView.Host.Rendering;

/// <summary>Converts between a project file and its avares URI. Relative URIs in the XAML resolve against it.</summary>
internal static class AvaresUri
{
    private const string Scheme = "avares";

    /// <summary>Builds the avares URI of a project file.</summary>
    /// <param name="assemblyName">The assembly that the project builds.</param>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <param name="axamlPath">A file of the project.</param>
    public static Uri For(string assemblyName, string projectDirectory, string axamlPath)
    {
        string relative = Path.GetRelativePath(Path.GetFullPath(projectDirectory), Path.GetFullPath(axamlPath));
        string escaped = string.Join('/', relative.Split(Path.DirectorySeparatorChar).Select(Uri.EscapeDataString));
        return new Uri($"avares://{assemblyName}/{escaped}");
    }

    /// <summary>Gives the project file of an avares URI.</summary>
    /// <param name="uri">An absolute URI.</param>
    /// <param name="assemblyName">The assembly that the project builds.</param>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <returns>The full path of the file, or null when the URI points into another assembly.</returns>
    public static string? ToPath(Uri uri, string assemblyName, string projectDirectory)
    {
        // The default URI parser changes the host to lower case.
        bool isLocal = uri.Scheme == Scheme && string.Equals(uri.Host, assemblyName, StringComparison.OrdinalIgnoreCase);
        if (!isLocal)
        {
            return null;
        }

        string[] segments = [.. uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString)];
        return Path.Combine([Path.GetFullPath(projectDirectory), .. segments]);
    }
}
