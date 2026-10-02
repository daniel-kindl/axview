namespace AxView.Protocol;

/// <summary>The kind of work that one host does.</summary>
internal enum HostMode
{
    /// <summary>The host shows an interactive preview window.</summary>
    Preview,

    /// <summary>The host saves one PNG of the file and exits.</summary>
    Snapshot,

    /// <summary>The host saves one PNG of each AXAML file of the project, and a summary file. Then it exits.</summary>
    SnapshotAll,
}
