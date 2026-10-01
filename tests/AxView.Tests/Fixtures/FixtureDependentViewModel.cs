namespace AxView.Tests.Fixtures;

/// <summary>A ViewModel with no parameterless constructor. The renderer cannot create it.</summary>
/// <param name="greeting">The value for <see cref="Greeting"/>.</param>
public sealed class FixtureDependentViewModel(string greeting)
{
    /// <summary>Gets the value that the constructor received.</summary>
    public string Greeting { get; } = greeting;
}
