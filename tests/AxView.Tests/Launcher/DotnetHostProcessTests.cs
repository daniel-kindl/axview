using AxView.Hosting;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Tests.Launcher;

public sealed class DotnetHostProcessTests
{
    [Theory]
    [InlineData("11.0.0", "host")]
    [InlineData("11.3.22", "host")]
    [InlineData("12.0.0-rc1", "host12")]
    [InlineData("12.1.3", "host12")]
    public void HostPath_SelectsTheHostBuildOfTheAvaloniaMajorVersion(string avaloniaVersion, string folder)
    {
        string toolDirectory = Path.Combine(Path.GetTempPath(), "tool");

        string hostPath = DotnetHostProcess.HostPath(toolDirectory, avaloniaVersion);

        Assert.Equal(Path.Combine(toolDirectory, folder, "AxView.Host.dll"), hostPath);
    }

    [Fact]
    public void HostPath_RejectsAVersionWithoutAMajorNumber() =>
        Assert.Throws<PreviewSetupException>(() => DotnetHostProcess.HostPath("/tool", "next"));

    [Fact]
    public void Arguments_RunTheHostInsideTheTargetRuntime()
    {
        string outDir = Path.Combine(Path.GetTempPath(), "out");
        AppOutput output = AppOutput.FromRuntimeConfig(Path.Combine(outDir, "Desktop.runtimeconfig.json"));
        PreviewTarget target = new("/src/Ui/Views/Main.axaml", "/src/Ui", "Ui", output, "/nuget/loader.dll", null, []);
        HostOptions options = target.ToHostOptions(snapshotPath: null, handOffFile: null);

        IReadOnlyList<string> args = DotnetHostProcess.Arguments("/tool/host/AxView.Host.dll", output, options);

        Assert.Equal(
            [
                "exec",
                "--runtimeconfig", output.RuntimeConfig,
                "--depsfile", output.DepsFile,
                "/tool/host/AxView.Host.dll",
                .. options.ToArguments(),
            ],
            args);
    }
}
