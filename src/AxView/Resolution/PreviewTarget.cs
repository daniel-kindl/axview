using AxView.Protocol;

namespace AxView.Resolution;

/// <summary>Everything the host needs to render one AXAML file.</summary>
/// <param name="AxamlPath">The file to show.</param>
/// <param name="ProjectDirectory">The folder of the project that contains the file.</param>
/// <param name="AssemblyName">The assembly that the project builds.</param>
/// <param name="Output">The app output that the host runs in.</param>
/// <param name="LoaderPath">The XAML loader assembly.</param>
/// <param name="AppProjectDirectory">The folder of the app project of a library view. Null for an app view, or when no app project is found.</param>
/// <param name="ReferencedProjectDirectories">The folders of the projects that the view project and the app project reference.</param>
/// <param name="ChromeThemePath">The Simple theme assembly for the chrome. Null when the app ships the Fluent or the Simple theme.</param>
internal sealed record PreviewTarget(
    string AxamlPath,
    string ProjectDirectory,
    string AssemblyName,
    AppOutput Output,
    string LoaderPath,
    string? AppProjectDirectory,
    IReadOnlyList<string> ReferencedProjectDirectories,
    string? ChromeThemePath = null)
{
    /// <summary>Makes the options that tell the host to show this target.</summary>
    /// <param name="snapshotPath">If set, the host saves one PNG to this path and exits.</param>
    /// <param name="handOffFile">If set, the host writes a hand-off here before a project switch.</param>
    /// <param name="snapshotAllDirectory">If set, the host saves a PNG of each file of the project in this folder and exits.</param>
    public HostOptions ToHostOptions(string? snapshotPath, string? handOffFile, string? snapshotAllDirectory = null) =>
        new(AxamlPath, ProjectDirectory, AssemblyName, Output.AppName, LoaderPath, snapshotPath, handOffFile)
        {
            AppProjectDirectory = AppProjectDirectory,
            SnapshotAllDirectory = snapshotAllDirectory,
            ReferencedProjects = ReferencedProjectDirectories.Count > 0 ? string.Join(Path.PathSeparator, ReferencedProjectDirectories) : null,
            ChromeThemePath = ChromeThemePath,
        };
}
