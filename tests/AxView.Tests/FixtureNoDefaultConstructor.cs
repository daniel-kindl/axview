using Avalonia.Controls;

namespace AxView.Tests;

/// <summary>A code-behind class with no parameterless constructor. The renderer cannot create it.</summary>
/// <param name="tag">The value for <see cref="Control.Tag"/>.</param>
public sealed class FixtureNoDefaultConstructor(string tag) : UserControl
{
    /// <summary>Gets the value that the constructor received.</summary>
    public string ConstructorTag { get; } = tag;
}
