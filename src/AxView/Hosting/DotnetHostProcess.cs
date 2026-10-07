using System.Diagnostics;

using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Hosting;

/// <summary>Runs the host with <c>dotnet exec</c> inside the runtime of the target app.</summary>
/// <param name="toolDirectory">The folder that contains the host folders.</param>
/// <param name="shadowRoot">The folder of the <see cref="OutputShadow"/> copies. The host runs in a copy, so that a build can replace the app output.</param>
/// <remarks>
/// Each host folder contains one host build. The build agrees with the Avalonia major version of the app:
/// <list type="bullet">
/// <item><description><c>host</c>: Avalonia 11.</description></item>
/// <item><description><c>host12</c>: Avalonia 12 and later.</description></item>
/// </list>
/// </remarks>
internal sealed class DotnetHostProcess(string toolDirectory, string shadowRoot) : IHostProcess
{
    private const string HostAssembly = "AxView.Host.dll";
    private const string Avalonia11Folder = "host";
    private const string Avalonia12Folder = "host12";
    private const int Avalonia12Major = 12;

    /// <summary>Makes the host process for the host builds that the launcher ships next to it.</summary>
    public static DotnetHostProcess ForInstalledHost() => new(AppContext.BaseDirectory, OutputShadow.DefaultRoot);

    /// <summary>Gives the path of the host build for an Avalonia version.</summary>
    /// <param name="toolDirectory">The folder that contains the host folders.</param>
    /// <param name="avaloniaVersion">The Avalonia package version of the app, for example <c>12.1.3</c>.</param>
    /// <exception cref="PreviewSetupException">The version does not start with a major number.</exception>
    public static string HostPath(string toolDirectory, string avaloniaVersion)
    {
        string majorText = avaloniaVersion.Split('.')[0];
        if (!int.TryParse(majorText, out int major))
        {
            throw new PreviewSetupException($"The Avalonia version '{avaloniaVersion}' does not start with a major number.");
        }

        string folder = major >= Avalonia12Major ? Avalonia12Folder : Avalonia11Folder;
        return Path.Combine(toolDirectory, folder, HostAssembly);
    }

    /// <summary>Makes the dotnet command line.</summary>
    /// <remarks>The runtimeconfig and deps file of the output make the host run on the target's runtime and Avalonia.</remarks>
    /// <param name="hostPath">The host build.</param>
    /// <param name="output">The app output that the host runs in.</param>
    /// <param name="options">The options of the host.</param>
    public static IReadOnlyList<string> Arguments(string hostPath, AppOutput output, HostOptions options) =>
    [
        "exec",
        "--runtimeconfig", output.RuntimeConfig,
        "--depsfile", output.DepsFile,
        hostPath,
        .. options.ToArguments(),
    ];

    /// <inheritdoc />
    public async Task<int> RunAsync(PreviewTarget target, HostOptions options)
    {
        string hostPath = HostPath(toolDirectory, target.Output.ReadAvaloniaVersion());
        using OutputShadow shadow = OutputShadow.Create(target.Output, shadowRoot);
        ProcessStartInfo info = new(DotnetHost.Path()) { UseShellExecute = false };
        foreach (string argument in Arguments(hostPath, shadow.Output, options))
        {
            info.ArgumentList.Add(argument);
        }

        using Process host = Process.Start(info) ?? throw new PreviewSetupException("Cannot start the preview host.");
        await host.WaitForExitAsync();
        return host.ExitCode;
    }
}
