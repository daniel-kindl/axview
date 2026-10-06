namespace AxView.Host.Rendering;

/// <summary>One Avalonia log event that <see cref="LogCapture"/> caught.</summary>
/// <param name="Level">The log level, for example <c>Warning</c>.</param>
/// <param name="Area">The log area, for example <c>Binding</c>.</param>
/// <param name="Source">The type and the name of the source, for example <c>TextBlock#lblTitle</c>, or null.</param>
/// <param name="Message">The message with the property values in it.</param>
internal sealed record LogEntry(string Level, string Area, string? Source, string Message);
