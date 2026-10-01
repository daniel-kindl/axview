namespace AxView.Tests;

public sealed class LoaderLocatorTests
{
    [Fact]
    public void ReadAvaloniaVersion_ReadsThePackageVersionFromDeps()
    {
        using TempDir dir = new();
        string deps = dir.Write("App.deps.json", """
            {
              "libraries": {
                "Avalonia.Desktop/11.3.22": { "type": "package" },
                "Avalonia/11.3.22": { "type": "package" },
                "App/1.0.0": { "type": "project" }
              }
            }
            """);

        Assert.Equal("11.3.22", LoaderLocator.ReadAvaloniaVersion(deps));
    }

    [Fact]
    public void ReadAvaloniaVersion_ThrowsWhenAvaloniaIsMissing()
    {
        using TempDir dir = new();
        string deps = dir.Write("App.deps.json", """{ "libraries": { "App/1.0.0": {} } }""");

        _ = Assert.Throws<PreviewSetupException>(() => LoaderLocator.ReadAvaloniaVersion(deps));
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("netstandard2.0")]
    public void FindInCache_ReturnsTheLoaderOfTheExactVersion(string tfm)
    {
        using TempDir dir = new();
        string loader = dir.Write($"avalonia.markup.xaml.loader/11.3.22/lib/{tfm}/Avalonia.Markup.Xaml.Loader.dll");
        _ = dir.Write("avalonia.markup.xaml.loader/11.3.20/lib/net8.0/Avalonia.Markup.Xaml.Loader.dll");

        Assert.Equal(loader, LoaderLocator.FindInCache(dir.Path, "11.3.22"));
    }

    [Fact]
    public void FindInCache_PrefersNet8OverNetStandard()
    {
        using TempDir dir = new();
        _ = dir.Write("avalonia.markup.xaml.loader/11.3.22/lib/netstandard2.0/Avalonia.Markup.Xaml.Loader.dll");
        string net8 = dir.Write("avalonia.markup.xaml.loader/11.3.22/lib/net8.0/Avalonia.Markup.Xaml.Loader.dll");

        Assert.Equal(net8, LoaderLocator.FindInCache(dir.Path, "11.3.22"));
    }

    [Fact]
    public void FindInCache_ReturnsNullWhenTheVersionIsNotCached()
    {
        using TempDir dir = new();

        Assert.Null(LoaderLocator.FindInCache(dir.Path, "11.3.22"));
    }
}
