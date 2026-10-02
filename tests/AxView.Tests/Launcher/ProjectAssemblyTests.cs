using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class ProjectAssemblyTests
{
    [Fact]
    public void ReadName_UsesAssemblyNameProperty()
    {
        using TempDir dir = new();
        string project = dir.Write("App/App.csproj",
            "<Project><PropertyGroup><AssemblyName>My.Ui</AssemblyName></PropertyGroup></Project>");

        Assert.Equal("My.Ui", ProjectAssembly.ReadName(project));
    }

    [Fact]
    public void ReadName_FallsBackToProjectFileName()
    {
        using TempDir dir = new();
        string project = dir.Write("App/My.App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        Assert.Equal("My.App", ProjectAssembly.ReadName(project));
    }
}
