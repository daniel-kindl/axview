namespace AxView.Tests.Fixtures;

/// <summary>A ViewModel with a parameterless constructor. Fixture AXAML names it as the DataContext type.</summary>
public sealed class FixtureViewModel
{
    /// <summary>Gets a text for a binding in the fixture AXAML.</summary>
    public string Greeting { get; } = "Hello from the ViewModel";
}
