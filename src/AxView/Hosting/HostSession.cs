using AxView.Building;
using AxView.Loader;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Hosting;

/// <summary>Runs the host for one file. Starts the host again in these conditions:</summary>
/// <list type="bullet">
/// <item>The user opens a file of another project.</item>
/// <item>A code file of the project changes. The session builds the app before the next host.</item>
/// </list>
/// <remarks>
/// If the launcher cannot prepare the file of the other project, the host starts again on the previous file.
/// If the build fails, the host starts again on the old app output.
/// The host then shows the setup error.
/// </remarks>
/// <param name="resolver">Finds the preview target of each file.</param>
/// <param name="hostProcess">Runs each host.</param>
/// <param name="outputBuilder">Builds the app before a host starts again on a changed project.</param>
/// <param name="log">Receives a line for each host that the session starts.</param>
/// <param name="settingsFile">The settings file that keeps the window, the theme, and the preview size of each preview host.</param>
internal sealed class HostSession(
    PreviewTargetResolver resolver,
    IHostProcess hostProcess,
    IOutputBuilder outputBuilder,
    TextWriter log,
    string settingsFile)
{
    /// <summary>Makes a session that runs the installed host and takes the XAML loader from NuGet.</summary>
    /// <param name="settingsFile">The settings file that keeps the window, the theme, and the preview size of each preview host.</param>
    public static HostSession CreateDefault(string settingsFile)
    {
        PreviewTargetResolver resolver = new(new NuGetLoaderSource());
        return new HostSession(
            resolver, DotnetHostProcess.ForInstalledHost(), new DotnetOutputBuilder(), Console.Error, settingsFile);
    }

    /// <param name="axamlPath">The first file to show.</param>
    /// <param name="outputDir">The app output to use, or null to find it.</param>
    /// <param name="snapshotPath">If set, the host saves one PNG and exits.</param>
    /// <param name="opened">Called with each file that the session opens.</param>
    /// <param name="snapshotAllDirectory">If set, the host saves a PNG of each file of the project in this folder and exits.</param>
    /// <param name="viewModelName">If set, the full name of the ViewModel type for the DataContext of a snapshot.</param>
    /// <returns>The exit code of the last host.</returns>
    /// <exception cref="PreviewSetupException">The launcher cannot prepare the first file.</exception>
    public async Task<int> RunAsync(string axamlPath, string? outputDir, string? snapshotPath, Action<string> opened, string? snapshotAllDirectory = null,
        string? viewModelName = null)
    {
        string handOffFile = Path.Combine(Path.GetTempPath(), $"axview-hand-off-{Guid.NewGuid():N}.json");
        PreviewTarget target = Open(axamlPath, outputDir, opened);
        string? setupError = null;

        // A snapshot must not depend on the last window of the user.
        string? hostSettingsFile = snapshotPath is null && snapshotAllDirectory is null ? settingsFile : null;
        try
        {
            while (true)
            {
                await log.WriteLineAsync($"axview: {target.AssemblyName} in '{target.Output.Directory}'");
                HostOptions options = target.ToHostOptions(snapshotPath, handOffFile, snapshotAllDirectory) with
                {
                    SetupError = setupError,
                    SettingsFile = hostSettingsFile,
                    ViewModelName = viewModelName,
                };
                int exitCode = await hostProcess.RunAsync(target, options);
                bool isHandOff = exitCode is (int)ExitReason.SwitchProject or (int)ExitReason.Rebuild;
                HandOff? handOff = isHandOff ? HandOff.Load(handOffFile) : null;
                if (handOff is null)
                {
                    return exitCode;
                }

                File.Delete(handOffFile);
                setupError = null;
                try
                {
                    if (exitCode == (int)ExitReason.Rebuild)
                    {
                        // The file of the hand-off is in the same project. On a build error, the next host shows it.
                        target = target with { AxamlPath = handOff.NextFile };
                        await log.WriteLineAsync($"axview: build of the app in '{target.Output.Directory}'");
                        await outputBuilder.BuildAsync(target.Output);
                        target = Open(handOff.NextFile, outputDir, opened);
                    }
                    else
                    {
                        // The given output belongs to the first project only.
                        outputDir = null;
                        target = Open(handOff.NextFile, outputDir, opened);
                    }
                }
                catch (PreviewSetupException ex)
                {
                    setupError = exitCode == (int)ExitReason.Rebuild ? ex.Message : $"Cannot open the file: {ex.Message}";
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
