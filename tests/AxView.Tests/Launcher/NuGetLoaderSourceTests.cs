using AxView.Loader;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class NuGetLoaderSourceTests
{
    [Theory]
    [InlineData("net8.0")]
    [InlineData("netstandard2.0")]
    public void FindInCache_ReturnsTheLoaderOfTheExactVersion(string tfm)
    {
        using TempDir dir = new();
        string loader = dir.Write($"avalonia.markup.xaml.loader/11.3.22/lib/{tfm}/Avalonia.Markup.Xaml.Loader.dll");
        _ = dir.Write("avalonia.markup.xaml.loader/11.3.20/lib/net8.0/Avalonia.Markup.Xaml.Loader.dll");

        Assert.Equal(loader, NuGetLoaderSource.FindInCache(dir.Path, "11.3.22"));
    }

    [Fact]
    public void FindInCache_PrefersNet8OverNetStandard()
    {
        using TempDir dir = new();
        _ = dir.Write("avalonia.markup.xaml.loader/11.3.22/lib/netstandard2.0/Avalonia.Markup.Xaml.Loader.dll");
        string net8 = dir.Write("avalonia.markup.xaml.loader/11.3.22/lib/net8.0/Avalonia.Markup.Xaml.Loader.dll");

        Assert.Equal(net8, NuGetLoaderSource.FindInCache(dir.Path, "11.3.22"));
    }

    [Fact]
    public void FindInCache_ReturnsNullWhenTheVersionIsNotCached()
    {
        using TempDir dir = new();

        Assert.Null(NuGetLoaderSource.FindInCache(dir.Path, "11.3.22"));
    }
}
