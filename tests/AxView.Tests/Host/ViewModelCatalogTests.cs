extern alias host;

using AxView.Tests.Fixtures;

using host::AxView.Host.Catalog;

namespace AxView.Tests.Host;

public sealed class ViewModelCatalogTests
{
    [Fact]
    public void List_ContainsTheViewModelsWithAParameterlessConstructor()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly);

        Assert.Contains(typeof(FixtureViewModel), types);
        Assert.Contains(typeof(FixtureOtherViewModel), types);
    }

    [Fact]
    public void List_SkipsAViewModelWithNoParameterlessConstructor()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly);

        Assert.DoesNotContain(typeof(FixtureDependentViewModel), types);
    }

    [Fact]
    public void List_SkipsATypeWhoseNameDoesNotEndWithViewModel()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly);

        Assert.DoesNotContain(typeof(FixtureControl), types);
    }

    [Fact]
    public void List_SortsTheTypesByName()
    {
        IReadOnlyList<Type> types = ViewModelCatalog.List(typeof(ViewModelCatalogTests).Assembly);

        Assert.Equal(types.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase), types);
    }
}
