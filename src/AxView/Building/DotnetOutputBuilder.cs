using System.Diagnostics;

using AxView.Hosting;
using AxView.Resolution;

namespace AxView.Building;

/// <summary>Builds the app project of an output with <c>dotnet build</c>.</summary>
/// <remarks>The build uses the configuration of the output, if the output is in the <c>bin</c> folder of the project.</remarks>
internal sealed class DotnetOutputBuilder : IOutputBuilder
{
    private const string BinFolder = "bin";
    private const string ErrorMarker = ": error ";

    /// <summary>Makes the dotnet command line.</summary>
    /// <param name="projectPath">The app project.</param>
    /// <param name="output">The app output that the project builds.</param>
    public static IReadOnlyList<string> Arguments(string projectPath, AppOutput output)
    {
        List<string> args = ["build", projectPath, "--nologo", "--verbosity", "quiet"];
        string relative = Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, output.Directory);
        string[] folders = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if ((folders.Length >= 2) && folders[0].Equals(BinFolder, StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(["--configuration", folders[1]]);
        }

        return args;
    }

    /// <inheritdoc />
    public async Task BuildAsync(AppOutput output)
    {
        string project = ProjectFinder.FindAppProject(output);
        ProcessStartInfo info = new(DotnetHost.Path())
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in Arguments(project, output))
        {
            info.ArgumentList.Add(argument);
        }

        using Process build = Process.Start(info) ?? throw new PreviewSetupException("Cannot start dotnet build.");

        // Read both streams together. A full pipe stops the build.
        Task<string> standardOutput = build.StandardOutput.ReadToEndAsync();
        Task<string> standardError = build.StandardError.ReadToEndAsync();
        await build.WaitForExitAsync();
        string text = await standardOutput + await standardError;
        if (build.ExitCode != 0)
        {
            throw new PreviewSetupException($"The build of '{project}' failed.{Environment.NewLine}{Errors(text)}");
        }
    }

    // MSBuild writes each error two times: during the build and in the summary.
    private static string Errors(string buildOutput)
    {
        string[] lines = buildOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] errors = [.. lines
            .Where(line => line.Contains(ErrorMarker, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)];
        return string.Join(Environment.NewLine, errors.Length > 0 ? errors : lines);
    }
}
