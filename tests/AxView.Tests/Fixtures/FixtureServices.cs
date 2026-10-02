namespace AxView.Tests.Fixtures;

/// <summary>A service provider that gives only a string. Tests use it as the design services of the target app.</summary>
public sealed class FixtureServices : IServiceProvider
{
    /// <summary>The string that the provider gives.</summary>
    public const string Greeting = "Hello from the services";

    /// <inheritdoc/>
    public object? GetService(Type serviceType) => serviceType == typeof(string) ? Greeting : null;
}
