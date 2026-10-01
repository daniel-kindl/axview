extern alias host;

using host::AxView.Host;

namespace AxView.Tests;

public sealed class XmlnsQualifierTests
{
    private static readonly Dictionary<string, string> Owners = new()
    {
        ["My.App.Views"] = "My.App",
        ["Icons"] = "Icon.Lib",
    };

    [Fact]
    public void Qualify_AddsTheOwningAssemblyToAUsingNamespace()
    {
        string xaml = """<UserControl xmlns:v="using:My.App.Views" />""";

        string result = XmlnsQualifier.Qualify(xaml, Owners.GetValueOrDefault);

        Assert.Equal("""<UserControl xmlns:v="clr-namespace:My.App.Views;assembly=My.App" />""", result);
    }

    [Fact]
    public void Qualify_AddsTheOwningAssemblyToAClrNamespace()
    {
        string xaml = "<UserControl xmlns:i='clr-namespace:Icons' />";

        string result = XmlnsQualifier.Qualify(xaml, Owners.GetValueOrDefault);

        Assert.Equal("<UserControl xmlns:i='clr-namespace:Icons;assembly=Icon.Lib' />", result);
    }

    [Fact]
    public void Qualify_KeepsANamespaceThatAlreadyNamesAnAssembly()
    {
        string xaml = """<UserControl xmlns:v="clr-namespace:My.App.Views;assembly=Other" />""";

        Assert.Equal(xaml, XmlnsQualifier.Qualify(xaml, Owners.GetValueOrDefault));
    }

    [Fact]
    public void Qualify_KeepsANamespaceWithNoKnownOwner()
    {
        string xaml = """<UserControl xmlns="https://github.com/avaloniaui" xmlns:u="using:Unknown" />""";

        Assert.Equal(xaml, XmlnsQualifier.Qualify(xaml, Owners.GetValueOrDefault));
    }
}
