using AxView.Protocol;

namespace AxView;

/// <summary>Everything the host needs to render one AXAML file.</summary>
internal sealed record PreviewTarget(
    string AxamlPath,
    string ProjectDirectory,
    string AssemblyName,
    AppOutput Output,
    string LoaderPath)
{
    /// <summary>Makes the options that tell the host to show this target.</summary>
    /// <param name="snapshotPath">If set, the host saves one PNG to this path and exits.</param>
    /// <param name="handOffFile">If set, the host writes a hand-off here before a project switch.</param>
    public HostOptions ToHostOptions(string? snapshotPath, string? handOffFile) =>
        new(AxamlPath, ProjectDirectory, AssemblyName, Output.AppName, LoaderPath, snapshotPath, handOffFile);
}
