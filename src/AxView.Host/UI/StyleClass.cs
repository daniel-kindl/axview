namespace AxView.Host.UI;

/// <summary>A style class of the app on a control type, for example <c>Button.primary</c>.</summary>
/// <param name="TypeName">The name of the control type, for example <c>Button</c>.</param>
/// <param name="Classes">The classes, with a full stop between them, for example <c>primary.large</c>.</param>
internal sealed record StyleClass(string TypeName, string Classes);
