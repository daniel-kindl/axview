using AxView.Building;
using AxView.Resolution;

namespace AxView.Tests.Launcher;

public sealed class DotnetOutputBuilderTests
{
    [Fact]
    public void Arguments_BuildTheProjectInTheConfigurationOfTheOutput()
    {
        string project = Path.Combine(Path.GetTempPath(), "src", "Desktop", "Desktop.csproj");
        string outputDir = Path.Combine(Path.GetTempPath(), "src", "Desktop", "bin", "Release", "net9.0");
        AppOutput output = AppOutput.FromRuntimeConfig(Path.Combine(outputDir, "Desktop.runtimeconfig.json"));

        IReadOnlyList<string> args = DotnetOutputBuilder.Arguments(project, output);

        Assert.Equal(["build", project, "--nologo", "--verbosity", "minimal", "--configuration", "Release"], args);
    }

    [Fact]
    public void Arguments_UseTheDefaultConfigurationForAnOutputOutsideTheBinFolder()
    {
        string project = Path.Combine(Path.GetTempPath(), "src", "Desktop", "Desktop.csproj");
        string outputDir = Path.Combine(Path.GetTempPath(), "artifacts", "Desktop");
        AppOutput output = AppOutput.FromRuntimeConfig(Path.Combine(outputDir, "Desktop.runtimeconfig.json"));

        IReadOnlyList<string> args = DotnetOutputBuilder.Arguments(project, output);

        Assert.Equal(["build", project, "--nologo", "--verbosity", "minimal"], args);
    }
}
