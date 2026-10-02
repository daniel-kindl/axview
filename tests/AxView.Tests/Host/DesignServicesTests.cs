extern alias host;

using Avalonia.Controls;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class DesignServicesTests
{
    [Fact]
    public void Load_CallsTheAxViewDesignFactoryOfTheAssembly()
    {
        int callsBefore = AxViewDesign.CallCount;

        DesignServices services = DesignServices.Load(typeof(DesignServicesTests).Assembly);

        Assert.Equal(callsBefore + 1, AxViewDesign.CallCount);
        Assert.True(services.CanCreate(typeof(FixtureDependentViewModel)));
    }

    [Fact]
    public void Load_GivesNoServicesForAnAssemblyWithoutAFactory()
    {
        DesignServices services = DesignServices.Load(typeof(Control).Assembly);

        Assert.False(services.CanCreate(typeof(FixtureDependentViewModel)));
    }

    [Fact]
    public void Create_GivesTheConstructorParametersFromTheServices()
    {
        DesignServices services = new(new FixtureServices());

        object created = services.Create(typeof(FixtureDependentViewModel));

        Assert.Equal(FixtureServices.Greeting, Assert.IsType<FixtureDependentViewModel>(created).Greeting);
    }

    [Fact]
    public void Create_UsesTheParameterlessConstructorWithoutServices()
    {
        object created = DesignServices.None.Create(typeof(FixtureViewModel));

        _ = Assert.IsType<FixtureViewModel>(created);
    }

    [Fact]
    public void CanCreate_IsFalseWhenAParameterHasNoService()
    {
        DesignServices services = new(new FixtureServices());

        Assert.False(services.CanCreate(typeof(FixtureUnresolvableViewModel)));
    }

    [Fact]
    public void Create_ThrowsWhenAParameterHasNoService()
    {
        DesignServices services = new(new FixtureServices());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => services.Create(typeof(FixtureUnresolvableViewModel)));

        Assert.Contains(nameof(FixtureUnresolvableViewModel), ex.Message, StringComparison.Ordinal);
    }
}
