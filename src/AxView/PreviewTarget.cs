namespace AxView;

/// <summary>Everything the host needs to render one AXAML file.</summary>
internal sealed record PreviewTarget(
    string AxamlPath,
    string ProjectDirectory,
    string AssemblyName,
    AppOutput Output,
    string LoaderPath)
{
    public static PreviewTarget Resolve(string axamlPath, string? outputDir)
    {
        string fullPath = Path.GetFullPath(axamlPath);
        if (!File.Exists(fullPath))
        {
            throw new PreviewSetupException($"File not found: '{fullPath}'.");
        }

        string project = ProjectFinder.FindProject(fullPath);
        string assemblyName = ProjectFinder.ReadAssemblyName(project);
        AppOutput output = outputDir is null
            ? OutputFinder.Find(project, assemblyName)
            : OutputFinder.FromDirectory(outputDir, assemblyName);
        string version = LoaderLocator.ReadAvaloniaVersion(output.DepsFile);

        return new PreviewTarget(
            fullPath,
            Path.GetDirectoryName(project)!,
            assemblyName,
            output,
            LoaderLocator.Ensure(version));
    }
}
