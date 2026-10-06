using System.Diagnostics;
using System.Text;

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
        List<string> args = ["build", projectPath, "--nologo", "--verbosity", "minimal"];
        string relative = Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, output.Directory);
        string[] folders = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if ((folders.Length >= 2) && folders[0].Equals(BinFolder, StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(["--configuration", folders[1]]);
        }

        return args;
    }

    /// <inheritdoc />
    public async Task BuildAsync(AppOutput output, IProgress<string> progress, CancellationToken cancellation)
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

        using Process build = new() { StartInfo = info };
        StringBuilder text = new();
        build.OutputDataReceived += (_, e) => Receive(e.Data, text, progress);
        build.ErrorDataReceived += (_, e) => Receive(e.Data, text, progress);
        if (!build.Start())
        {
            throw new PreviewSetupException("Cannot start dotnet build.");
        }

        // Read both streams together. A full pipe stops the build.
        build.BeginOutputReadLine();
        build.BeginErrorReadLine();
        try
        {
            await build.WaitForExitAsync(cancellation);
        }
        catch (OperationCanceledException)
        {
            build.Kill(entireProcessTree: true);
            throw new PreviewSetupException($"The build of '{project}' was canceled.");
        }

        if (build.ExitCode != 0)
        {
            throw new PreviewSetupException($"The build of '{project}' failed.{Environment.NewLine}{Errors(text.ToString())}");
        }
    }

    // The process calls this method on worker threads, one time for each line. The last call gives null.
    private static void Receive(string? line, StringBuilder text, IProgress<string> progress)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (text)
        {
            _ = text.AppendLine(line);
        }

        progress.Report(line);
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
