using Avalonia;
using Avalonia.Controls;

namespace AxView.Tests.Fixtures;

/// <summary>A control that throws in layout. Fixture AXAML refers to it with a <c>using:</c> namespace.</summary>
public sealed class FixtureThrowingControl : Control
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => throw new InvalidOperationException("Layout failed.");
}
