using Avalonia;

namespace AxView.Tests.Fixtures;

/// <summary>A PNG of a window that <see cref="ChromeShots"/> wrote.</summary>
/// <param name="Png">The path of the PNG.</param>
/// <param name="ClientSize">The client size of the window, in device-independent pixels.</param>
internal sealed record ChromeShot(string Png, Size ClientSize);
