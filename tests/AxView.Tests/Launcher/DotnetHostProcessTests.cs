using AxView.Hosting;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Tests.Launcher;

public sealed class DotnetHostProcessTests
{
    [Fact]
    public void Arguments_RunTheHostInsideTheTargetRuntime()
    {
        string outDir = Path.Combine(Path.GetTempPath(), "out");
        AppOutput output = AppOutput.FromRuntimeConfig(Path.Combine(outDir, "Desktop.runtimeconfig.json"));
        PreviewTarget target = new("/src/Ui/Views/Main.axaml", "/src/Ui", "Ui", output, "/nuget/loader.dll");
        HostOptions options = target.ToHostOptions(snapshotPath: null, handOffFile: null);

        IReadOnlyList<string> args = DotnetHostProcess.Arguments("/tool/host/AxView.Host.dll", target, options);

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
