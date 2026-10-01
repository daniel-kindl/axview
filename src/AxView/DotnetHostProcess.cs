using System.Diagnostics;

using AxView.Protocol;

namespace AxView;

/// <summary>Runs the host with <c>dotnet exec</c> inside the runtime of the target app.</summary>
/// <param name="hostPath">The path of AxView.Host.dll.</param>
internal sealed class DotnetHostProcess(string hostPath) : IHostProcess
{
    /// <summary>Makes the host process for the host that the launcher ships in its <c>host</c> folder.</summary>
    public static DotnetHostProcess ForInstalledHost() =>
        new(Path.Combine(AppContext.BaseDirectory, "host", "AxView.Host.dll"));

    /// <summary>Makes the dotnet command line.</summary>
    /// <remarks>The target's runtimeconfig and deps file make the host run on the target's runtime and Avalonia.</remarks>
    public static IReadOnlyList<string> Arguments(string hostPath, PreviewTarget target, HostOptions options) =>
    [
        "exec",
        "--runtimeconfig", target.Output.RuntimeConfig,
        "--depsfile", target.Output.DepsFile,
        hostPath,
        .. options.ToArguments(),
    ];

    /// <inheritdoc />
    public async Task<int> RunAsync(PreviewTarget target, HostOptions options)
    {
        ProcessStartInfo info = new(DotnetHost.Path()) { UseShellExecute = false };
        foreach (string argument in Arguments(hostPath, target, options))
        {
            info.ArgumentList.Add(argument);
        }

        using Process host = Process.Start(info) ?? throw new PreviewSetupException("Cannot start the preview host.");
        await host.WaitForExitAsync();
        return host.ExitCode;
    }
}
