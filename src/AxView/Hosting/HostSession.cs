using AxView.Loader;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Hosting;

/// <summary>Runs the host for one file. Starts the host again when the user opens a file of another project.</summary>
/// <param name="resolver">Finds the preview target of each file.</param>
/// <param name="hostProcess">Runs each host.</param>
/// <param name="log">Receives a line for each host that the session starts.</param>
internal sealed class HostSession(PreviewTargetResolver resolver, IHostProcess hostProcess, TextWriter log)
{
    /// <summary>Makes a session that runs the installed host and takes the XAML loader from NuGet.</summary>
    public static HostSession CreateDefault() =>
        new(new PreviewTargetResolver(new NuGetLoaderSource()), DotnetHostProcess.ForInstalledHost(), Console.Error);

    /// <param name="axamlPath">The first file to show.</param>
    /// <param name="outputDir">The app output to use, or null to find it.</param>
    /// <param name="snapshotPath">If set, the host saves one PNG and exits.</param>
    /// <param name="opened">Called with each file that the session opens.</param>
    /// <returns>The exit code of the last host.</returns>
    /// <exception cref="PreviewSetupException">The launcher cannot prepare a file.</exception>
    public async Task<int> RunAsync(string axamlPath, string? outputDir, string? snapshotPath, Action<string> opened)
    {
        string handOffFile = Path.Combine(Path.GetTempPath(), $"axview-hand-off-{Guid.NewGuid():N}.json");
        string path = axamlPath;
        try
        {
            while (true)
            {
                PreviewTarget target = resolver.Resolve(path, outputDir);
                opened(target.AxamlPath);
                await log.WriteLineAsync($"axview: {target.AssemblyName} in '{target.Output.Directory}'");

                int exitCode = await hostProcess.RunAsync(target, target.ToHostOptions(snapshotPath, handOffFile));
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
}
