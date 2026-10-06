extern alias host;

using System.Reflection;
using System.Xml;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class XamlLocationTests
{
    [Fact]
    public void Find_GivesTheLineAndColumnOfAnXmlException()
    {
        XmlException ex = new("Bad XML.", null, 3, 7);

        Assert.Equal(new XamlLocation(3, 7), XamlLocation.Find(ex));
    }

    [Fact]
    public void Find_LooksInTheInnerExceptions()
    {
        TargetInvocationException ex = new(new InvalidOperationException("Load failed.", new XmlException("Bad XML.", null, 2, 5)));

        Assert.Equal(new XamlLocation(2, 5), XamlLocation.Find(ex));
    }

    [Fact]
    public void Find_GivesNullWithoutALine() => Assert.Null(XamlLocation.Find(new InvalidOperationException("No line.")));
}
