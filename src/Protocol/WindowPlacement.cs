namespace AxView.Protocol;

/// <summary>The state and the normal bounds of the preview window.</summary>
/// <remarks>The bounds are the bounds of the normal window, also when <paramref name="Mode"/> is not normal.</remarks>
/// <param name="Mode">The window state.</param>
/// <param name="X">The left edge of the window, in screen pixels.</param>
/// <param name="Y">The top edge of the window, in screen pixels.</param>
/// <param name="Width">The width of the client area, in device-independent pixels.</param>
/// <param name="Height">The height of the client area, in device-independent pixels.</param>
internal sealed record WindowPlacement(WindowMode Mode, int X, int Y, double Width, double Height);
