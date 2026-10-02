namespace AxView.Tests.Fixtures;

/// <summary>A ViewModel whose constructor needs a service that <see cref="FixtureServices"/> does not give.</summary>
/// <param name="count">A value that no service gives.</param>
public sealed class FixtureUnresolvableViewModel(int count)
{
    /// <summary>Gets the value that the constructor received.</summary>
    public int Count { get; } = count;
}
