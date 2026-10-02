extern alias host;

using AxView.Tests.Fixtures;

using host::AxView.Host.Catalog;
using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class ViewModelCatalogTests
{
    [Fact]
    public void List_ContainsTheViewModelsWithAParameterlessConstructor()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly, DesignServices.None);

        Assert.Contains(typeof(FixtureViewModel), types);
        Assert.Contains(typeof(FixtureOtherViewModel), types);
    }

    [Fact]
    public void List_SkipsAViewModelThatTheServicesCannotCreate()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly, DesignServices.None);

        Assert.DoesNotContain(typeof(FixtureDependentViewModel), types);
    }

    [Fact]
    public void List_ContainsAViewModelThatTheDesignServicesCanCreate()
    {
        DesignServices services = new(new FixtureServices());

        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly, services);

        Assert.Contains(typeof(FixtureDependentViewModel), types);
        Assert.DoesNotContain(typeof(FixtureUnresolvableViewModel), types);
    }

    [Fact]
    public void List_SkipsATypeWhoseNameDoesNotEndWithViewModel()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly, DesignServices.None);

        Assert.DoesNotContain(typeof(FixtureControl), types);
    }

    [Fact]
    public void List_SortsTheTypesByName()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly, DesignServices.None);

        Assert.Equal(types.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase), types);
    }
}
