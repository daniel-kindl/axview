namespace AxView.Tests.Fixtures;

/// <summary>A second ViewModel with a parameterless constructor. Tests select it in place of the ViewModel from the XAML.</summary>
public sealed class FixtureOtherViewModel
{
    /// <summary>Gets a text for a binding in the fixture AXAML.</summary>
    public string Greeting { get; } = "Hello from the other ViewModel";
}
