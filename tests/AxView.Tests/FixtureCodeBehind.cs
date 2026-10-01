using Avalonia.Controls;

namespace AxView.Tests;

/// <summary>A code-behind class in the test assembly. Fixture AXAML names it in <c>x:Class</c>.</summary>
public sealed class FixtureCodeBehind : UserControl
{
    /// <summary>The value that the constructor gives to <see cref="Control.Tag"/>.</summary>
    public const string ConstructedTag = "constructed";

    /// <summary>Sets <see cref="Control.Tag"/>, so a test can see that the constructor ran.</summary>
    public FixtureCodeBehind()
    {
        Tag = ConstructedTag;
    }
}
