namespace AxView.Host.Rendering;

/// <summary>Builds the avares URI of a project file. Relative URIs in the XAML resolve against it.</summary>
internal static class AvaresUri
{
    public static Uri For(string assemblyName, string projectDirectory, string axamlPath)
    {
        string relative = Path.GetRelativePath(Path.GetFullPath(projectDirectory), Path.GetFullPath(axamlPath));
        string escaped = string.Join('/', relative.Split(Path.DirectorySeparatorChar).Select(Uri.EscapeDataString));
        return new Uri($"avares://{assemblyName}/{escaped}");
    }
}
