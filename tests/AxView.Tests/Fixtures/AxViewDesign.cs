namespace AxView.Tests.Fixtures;

/// <summary>The design services factory of the fixture project. The host finds it by its name.</summary>
public static class AxViewDesign
{
    /// <summary>Gets the number of calls to <see cref="CreateServices"/>.</summary>
    public static int CallCount { get; private set; }

    /// <summary>Creates the services for the ViewModels of the fixture project.</summary>
    public static IServiceProvider CreateServices()
    {
        CallCount++;
        return new FixtureServices();
    }
}
