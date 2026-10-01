using System.Diagnostics;

namespace AxView;

/// <summary>Runs the host for one file. Starts the host again when the user opens a file of another project.</summary>
internal static class HostSession
{
    /// <summary>The host exits with this code to ask for another project. The host defines the same value.</summary>
    private const int SwitchProjectExitCode = 3;

    /// <param name="axamlPath">The first file to show.</param>
    /// <param name="outputDir">The app output to use, or null to find it.</param>
    /// <param name="snapshotPath">If set, the host saves one PNG and exits.</param>
    /// <param name="opened">Called with each file that the session opens.</param>
    /// <returns>The exit code of the last host.</returns>
    /// <exception cref="PreviewSetupException">The launcher cannot prepare a file.</exception>
    public static async Task<int> RunAsync(string axamlPath, string? outputDir, string? snapshotPath, Action<string> opened)
    {
        string hostPath = Path.Combine(AppContext.BaseDirectory, "host", "AxView.Host.dll");
        string switchFile = Path.Combine(Path.GetTempPath(), $"axview-switch-{Guid.NewGuid():N}.txt");
        string path = axamlPath;
        try
        {
            while (true)
            {
                PreviewTarget target = PreviewTarget.Resolve(path, outputDir);
                opened(target.AxamlPath);
                Console.Error.WriteLine($"axview: {target.AssemblyName} in '{target.Output.Directory}'");

                int exitCode = await RunHostAsync(HostCommand.Arguments(target, hostPath, snapshotPath, switchFile));
                if (exitCode != SwitchProjectExitCode || !File.Exists(switchFile))
                {
                    return exitCode;
                }

                path = File.ReadAllText(switchFile).Trim();
                File.Delete(switchFile);

                // The given output belongs to the first project only.
                outputDir = null;
            }
        }
        finally
        {
            File.Delete(switchFile);
        }
    }

    private static async Task<int> RunHostAsync(IReadOnlyList<string> arguments)
    {
        ProcessStartInfo info = new(DotnetHost.Path()) { UseShellExecute = false };
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using Process host = Process.Start(info) ?? throw new PreviewSetupException("Cannot start the preview host.");
        await host.WaitForExitAsync();
        return host.ExitCode;
    }
}
