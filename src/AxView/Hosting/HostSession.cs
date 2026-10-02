using AxView.Loader;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Hosting;

/// <summary>Runs the host for one file. Starts the host again when the user opens a file of another project.</summary>
/// <remarks>
/// If the launcher cannot prepare the file of the other project, the host starts again on the previous file.
/// The host then shows the setup error.
/// </remarks>
/// <param name="resolver">Finds the preview target of each file.</param>
/// <param name="hostProcess">Runs each host.</param>
/// <param name="log">Receives a line for each host that the session starts.</param>
/// <param name="settingsFile">The settings file that keeps the window, the theme, and the preview size of each preview host.</param>
internal sealed class HostSession(PreviewTargetResolver resolver, IHostProcess hostProcess, TextWriter log, string settingsFile)
{
    /// <summary>Makes a session that runs the installed host and takes the XAML loader from NuGet.</summary>
    /// <param name="settingsFile">The settings file that keeps the window, the theme, and the preview size of each preview host.</param>
    public static HostSession CreateDefault(string settingsFile)
    {
        PreviewTargetResolver resolver = new(new NuGetLoaderSource());
        return new HostSession(resolver, DotnetHostProcess.ForInstalledHost(), Console.Error, settingsFile);
    }

    /// <param name="axamlPath">The first file to show.</param>
    /// <param name="outputDir">The app output to use, or null to find it.</param>
    /// <param name="snapshotPath">If set, the host saves one PNG and exits.</param>
    /// <param name="opened">Called with each file that the session opens.</param>
    /// <returns>The exit code of the last host.</returns>
    /// <exception cref="PreviewSetupException">The launcher cannot prepare the first file.</exception>
    public async Task<int> RunAsync(string axamlPath, string? outputDir, string? snapshotPath, Action<string> opened)
    {
        string handOffFile = Path.Combine(Path.GetTempPath(), $"axview-hand-off-{Guid.NewGuid():N}.json");
        PreviewTarget target = Open(axamlPath, outputDir, opened);
        string? setupError = null;

        // A snapshot must not depend on the last window of the user.
        string? hostSettingsFile = snapshotPath is null ? settingsFile : null;
        try
        {
            while (true)
            {
                await log.WriteLineAsync($"axview: {target.AssemblyName} in '{target.Output.Directory}'");
                HostOptions options = target.ToHostOptions(snapshotPath, handOffFile) with
                {
                    SetupError = setupError,
                    SettingsFile = hostSettingsFile,
                };
                int exitCode = await hostProcess.RunAsync(target, options);
                HandOff? handOff = exitCode == (int)ExitReason.SwitchProject ? HandOff.Load(handOffFile) : null;
                if (handOff is null)
                {
                    return exitCode;
                }

                File.Delete(handOffFile);
                setupError = null;
                try
                {
                    // The given output belongs to the first project only.
                    target = Open(handOff.NextFile, outputDir: null, opened);
                }
                catch (PreviewSetupException ex)
                {
                    setupError = ex.Message;
                    await log.WriteLineAsync($"axview: {ex.Message}");
                }
            }
        }
        finally
        {
            File.Delete(handOffFile);
        }
    }

    private PreviewTarget Open(string axamlPath, string? outputDir, Action<string> opened)
    {
        PreviewTarget target = resolver.Resolve(axamlPath, outputDir);
        opened(target.AxamlPath);
        return target;
    }
}
