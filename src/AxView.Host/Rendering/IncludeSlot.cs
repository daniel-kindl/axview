namespace AxView.Host.Rendering;

/// <summary>The place of one include in its parent collection. See <see cref="AppIncludes"/>.</summary>
/// <param name="Get">Gets the object in the place.</param>
/// <param name="Set">Puts a loaded object in the place. Throws when the object has the wrong type.</param>
internal sealed record IncludeSlot(Func<object> Get, Action<object> Set);
