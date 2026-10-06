using AxView.Packages;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class NuGetPackageSourceTests
{
    private const string LoaderPackage = "Avalonia.Markup.Xaml.Loader";

    [Theory]
    [InlineData("net8.0")]
    [InlineData("netstandard2.0")]
    public void FindInCache_ReturnsTheAssemblyOfTheExactVersion(string tfm)
    {
        using TempDir dir = new();
        string loader = dir.Write($"avalonia.markup.xaml.loader/11.3.22/lib/{tfm}/Avalonia.Markup.Xaml.Loader.dll");
        _ = dir.Write("avalonia.markup.xaml.loader/11.3.20/lib/net8.0/Avalonia.Markup.Xaml.Loader.dll");

        Assert.Equal(loader, NuGetPackageSource.FindInCache(dir.Path, LoaderPackage, "11.3.22"));
    }

    [Fact]
    public void FindInCache_PrefersNet8OverNetStandard()
    {
        using TempDir dir = new();
        _ = dir.Write("avalonia.markup.xaml.loader/11.3.22/lib/netstandard2.0/Avalonia.Markup.Xaml.Loader.dll");
        string net8 = dir.Write("avalonia.markup.xaml.loader/11.3.22/lib/net8.0/Avalonia.Markup.Xaml.Loader.dll");

        Assert.Equal(net8, NuGetPackageSource.FindInCache(dir.Path, LoaderPackage, "11.3.22"));
    }

    [Fact]
    public void FindInCache_ReturnsTheAssemblyOfTheGivenPackage()
    {
        using TempDir dir = new();
        _ = dir.Write("avalonia.markup.xaml.loader/11.3.22/lib/net8.0/Avalonia.Markup.Xaml.Loader.dll");
        string theme = dir.Write("avalonia.themes.simple/11.3.22/lib/net8.0/Avalonia.Themes.Simple.dll");

        Assert.Equal(theme, NuGetPackageSource.FindInCache(dir.Path, "Avalonia.Themes.Simple", "11.3.22"));
    }

    [Fact]
    public void FindInCache_ReturnsNullWhenTheVersionIsNotCached()
    {
        using TempDir dir = new();

        Assert.Null(NuGetPackageSource.FindInCache(dir.Path, LoaderPackage, "11.3.22"));
    }
}
