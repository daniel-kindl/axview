namespace AxView.Protocol;

/// <summary>The exit codes of the host and the launcher.</summary>
/// <remarks>Scripts and CI read these values. Do not change a value. Add new reasons at the end.</remarks>
internal enum ExitReason
{
    /// <summary>The preview or the snapshot is good.</summary>
    Ok = 0,

    /// <summary>The snapshot shows a render error.</summary>
    RenderFailed = 1,

    /// <summary>The host options are not valid.</summary>
    BadArguments = 2,

    /// <summary>The user opened a file of another project. The host wrote a <see cref="HandOff"/>.</summary>
    SwitchProject = 3,

    /// <summary>The launcher cannot prepare the preview, for example because the app is not built.</summary>
    SetupFailed = 4,

    /// <summary>A code file of the project changed. The host wrote a <see cref="HandOff"/> with the shown file.</summary>
    Rebuild = 5,
}
