using System.Diagnostics;

using AxView.Protocol;

namespace AxView;

/// <summary>Runs the host for one file. Starts the host again when the user opens a file of another project.</summary>
internal static class HostSession
{
    /// <param name="axamlPath">The first file to show.</param>
    /// <param name="outputDir">The app output to use, or null to find it.</param>
    /// <param name="snapshotPath">If set, the host saves one PNG and exits.</param>
    /// <param name="opened">Called with each file that the session opens.</param>
    /// <returns>The exit code of the last host.</returns>
    /// <exception cref="PreviewSetupException">The launcher cannot prepare a file.</exception>
    public static async Task<int> RunAsync(string axamlPath, string? outputDir, string? snapshotPath, Action<string> opened)
    {
        string hostPath = Path.Combine(AppContext.BaseDirectory, "host", "AxView.Host.dll");
        string handOffFile = Path.Combine(Path.GetTempPath(), $"axview-hand-off-{Guid.NewGuid():N}.json");
        string path = axamlPath;
        try
        {
            while (true)
            {
                PreviewTarget target = PreviewTarget.Resolve(path, outputDir);
                opened(target.AxamlPath);
                Console.Error.WriteLine($"axview: {target.AssemblyName} in '{target.Output.Directory}'");

                int exitCode = await RunHostAsync(target, hostPath, target.ToHostOptions(snapshotPath, handOffFile));
                HandOff? handOff = exitCode == (int)ExitReason.SwitchProject ? HandOff.Load(handOffFile) : null;
                if (handOff is null)
                {
                    return exitCode;
                }

                path = handOff.NextFile;
                File.Delete(handOffFile);

                // The given output belongs to the first project only.
                outputDir = null;
            }
        }
        finally
        {
            File.Delete(handOffFile);
        }
    }

    // dotnet exec with the target's runtimeconfig and deps file runs the host on the target's runtime and Avalonia.
    private static async Task<int> RunHostAsync(PreviewTarget target, string hostPath, HostOptions options)
    {
        ProcessStartInfo info = new(DotnetHost.Path()) { UseShellExecute = false };
        List<string> arguments =
        [
            "exec",
            "--runtimeconfig", target.Output.RuntimeConfig,
            "--depsfile", target.Output.DepsFile,
            hostPath,
            .. options.ToArguments(),
        ];
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using Process host = Process.Start(info) ?? throw new PreviewSetupException("Cannot start the preview host.");
        await host.WaitForExitAsync();
        return host.ExitCode;
    }
}
