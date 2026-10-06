using Avalonia.Styling;

namespace AxView.Host.UI;

/// <summary>A control theme of the app with a key, for example <c>Theme="{StaticResource PillButton}"</c>.</summary>
/// <param name="Key">The resource key, for example <c>PillButton</c>.</param>
/// <param name="Theme">The control theme.</param>
internal sealed record KeyedControlTheme(string Key, ControlTheme Theme);
