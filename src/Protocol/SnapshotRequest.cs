namespace AxView.Protocol;

/// <summary>The snapshot options that the user gives on the launcher command line.</summary>
/// <remarks><see cref="HostOptions.ValidateMode"/> checks that the options select one host mode.</remarks>
/// <param name="Path">If set, the host saves one PNG to this path and exits.</param>
/// <param name="AllDirectory">If set, the host saves one PNG for each AXAML file of the project in this folder and exits.</param>
/// <param name="ViewModelName">If set, the full name of the ViewModel type for the DataContext of a snapshot.</param>
/// <param name="Size">If set, the size of each snapshot.</param>
/// <param name="Theme">If set, the theme variant of each snapshot.</param>
/// <param name="TreePath">If set, the host saves the visual tree of the snapshot to this JSON file.</param>
/// <param name="ReportPath">If set, the host saves the result, the load time, the error, and the log of the snapshot to this JSON file.</param>
internal sealed record SnapshotRequest(
    string? Path = null,
    string? AllDirectory = null,
    string? ViewModelName = null,
    PreviewSize? Size = null,
    PreviewTheme? Theme = null,
    string? TreePath = null,
    string? ReportPath = null)
{
    /// <summary>No snapshot. The host shows the interactive preview window.</summary>
    public static SnapshotRequest None { get; } = new();

    /// <summary>Gets a value that tells if the request selects a snapshot mode.</summary>
    public bool IsSnapshot => Path is not null || AllDirectory is not null;
}
