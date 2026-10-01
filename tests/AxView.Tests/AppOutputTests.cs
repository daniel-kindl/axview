namespace AxView.Tests;

public sealed class AppOutputTests
{
    [Fact]
    public void FromRuntimeConfig_TakesTheAppNameFromTheFileName()
    {
        string runtimeConfig = Path.Combine(Path.GetTempPath(), "out", "My.App.runtimeconfig.json");

        AppOutput output = AppOutput.FromRuntimeConfig(runtimeConfig);

        Assert.Equal("My.App", output.AppName);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "out"), output.Directory);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "out", "My.App.deps.json"), output.DepsFile);
    }

    [Fact]
    public void ReadAvaloniaVersion_ReadsThePackageVersionFromDeps()
    {
        using TempDir dir = new();
        _ = dir.Write("App.deps.json", """
            {
              "libraries": {
                "Avalonia.Desktop/11.3.22": { "type": "package" },
                "Avalonia/11.3.22": { "type": "package" },
                "App/1.0.0": { "type": "project" }
              }
            }
            """);

        Assert.Equal("11.3.22", Output(dir).ReadAvaloniaVersion());
    }

    [Fact]
    public void ReadAvaloniaVersion_ThrowsWhenAvaloniaIsMissing()
    {
        using TempDir dir = new();
        _ = dir.Write("App.deps.json", """{ "libraries": { "App/1.0.0": {} } }""");

        _ = Assert.Throws<PreviewSetupException>(() => Output(dir).ReadAvaloniaVersion());
    }

    private static AppOutput Output(TempDir dir) => AppOutput.FromRuntimeConfig(Path.Combine(dir.Path, "App.runtimeconfig.json"));
}
