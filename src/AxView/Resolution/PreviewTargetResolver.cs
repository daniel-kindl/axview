using AxView.Packages;
using AxView.Protocol;

namespace AxView.Resolution;

/// <summary>Finds the preview target of an AXAML file: its project, the app output, the XAML loader, and the chrome theme.</summary>
/// <param name="packageSource">Gives the Avalonia packages that agree with the Avalonia version of the app output.</param>
internal sealed class PreviewTargetResolver(IPackageSource packageSource)
{
    private const string LoaderPackage = "Avalonia.Markup.Xaml.Loader";
    private const string ChromeThemePackage = "Avalonia.Themes.Simple";

    /// <param name="axamlPath">The file to preview.</param>
    /// <param name="outputDir">The app output to use, or null to find the most recent one.</param>
    /// <exception cref="PreviewSetupException">The launcher cannot prepare the file.</exception>
    public PreviewTarget Resolve(string axamlPath, string? outputDir)
    {
        string fullPath = Path.GetFullPath(axamlPath);
        if (!File.Exists(fullPath))
        {
            throw new PreviewSetupException($"File not found: '{fullPath}'.");
        }

        string project = ProjectFinder.FindProject(fullPath);
        string assemblyName = ProjectAssembly.ReadName(project);
        AppOutput output = outputDir is null
            ? OutputFinder.Find(project, assemblyName)
            : OutputFinder.FromDirectory(outputDir, assemblyName);
        string version = output.ReadAvaloniaVersion();
        string? appProjectDirectory = FindAppProjectDirectory(output, assemblyName);
        string loaderPath = packageSource.Find(LoaderPackage, version);

        // Without its own theme, the chrome must use a second instance of the app styles.
        string? chromeThemePath = output.ReferencesStandardTheme() ? null : packageSource.Find(ChromeThemePackage, version);

        return new PreviewTarget(
            fullPath,
            Path.GetDirectoryName(project)!,
            assemblyName,
            output,
            loaderPath,
            appProjectDirectory,
            FindReferencedProjectDirectories(project, appProjectDirectory),
            chromeThemePath);
    }

    // A change of the code of a referenced project needs a rebuild. Its AXAML files can be includes of the app.
    private static IReadOnlyList<string> FindReferencedProjectDirectories(string project, string? appProjectDirectory)
    {
        List<string> projects = [project];
        if (appProjectDirectory is not null)
        {
            projects.AddRange(Directory.EnumerateFiles(appProjectDirectory, "*.csproj"));
        }

        return ProjectReferences.FindDirectories(projects);
    }

    // The app of a library view is in another project. Without that project, only the build reports the problem.
    private static string? FindAppProjectDirectory(AppOutput output, string assemblyName)
    {
        if (output.AppName == assemblyName)
        {
            return null;
        }

        try
        {
            return Path.GetDirectoryName(ProjectFinder.FindAppProject(output));
        }
        catch (PreviewSetupException)
        {
            return null;
        }
    }
}
