namespace AxView;

/// <summary>Builds the command line that runs the host inside the target app's runtime.</summary>
internal static class HostCommand
{
    /// <param name="target">The file and app to preview.</param>
    /// <param name="hostPath">The path of AxView.Host.dll.</param>
    /// <param name="snapshotPath">If set, the host saves one PNG to this path and exits.</param>
    /// <param name="switchFile">If set, the host writes a file of another project here, then exits to switch.</param>
    public static IReadOnlyList<string> Arguments(
        PreviewTarget target, string hostPath, string? snapshotPath = null, string? switchFile = null) =>
    [
        "exec",
        "--runtimeconfig", target.Output.RuntimeConfig,
        "--depsfile", target.Output.DepsFile,
        hostPath,
        "--axaml", target.AxamlPath,
        "--project", target.ProjectDirectory,
        "--assembly", target.AssemblyName,
        "--app-assembly", target.Output.AppName,
        "--loader", target.LoaderPath,
        .. snapshotPath is null ? Array.Empty<string>() : ["--snapshot", snapshotPath],
        .. switchFile is null ? Array.Empty<string>() : ["--switch-file", switchFile],
    ];
}
