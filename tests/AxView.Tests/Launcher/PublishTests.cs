using System.Diagnostics;

using AxView.Hosting;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class PublishTests
{
    private const string SolutionFileName = "AxView.sln";

    [Fact]
    public async Task Publish_PutsEachHostBuildInItsHostFolderAsync()
    {
        using TempDir publish = new();
        string launcherProject = Path.Combine(SolutionDirectory(), "src", "AxView");
        ProcessStartInfo info = new(DotnetHost.Path())
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in new[] { "publish", launcherProject, "-c", "Release", "-o", publish.Path, "--nologo" })
        {
            info.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(info)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.True(process.ExitCode == 0, await output + await error);
        Assert.True(File.Exists(Path.Combine(publish.Path, "host", "AxView.Host.dll")));
        Assert.True(File.Exists(Path.Combine(publish.Path, "host12", "AxView.Host.dll")));
    }

    private static string SolutionDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException($"No {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
