namespace AxView.Protocol;

/// <summary>The state of the preview window that the settings file remembers.</summary>
/// <remarks>A minimized window is saved as <see cref="Normal"/>, so it does not open minimized.</remarks>
internal enum WindowMode
{
    /// <summary>The window has its own position and size.</summary>
    Normal,

    /// <summary>The window fills the working area of its screen.</summary>
    Maximized,

    /// <summary>The window fills its screen and has no frame.</summary>
    FullScreen,
}
