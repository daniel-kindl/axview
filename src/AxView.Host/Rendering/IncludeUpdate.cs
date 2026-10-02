namespace AxView.Host.Rendering;

/// <summary>The result of one <see cref="AppIncludes.Update"/>.</summary>
/// <param name="IsChanged">True when the update replaced one or more includes.</param>
/// <param name="Error">The errors of the include files that did not load, or null when all loaded.</param>
internal sealed record IncludeUpdate(bool IsChanged, string? Error);
