using Avalonia;
using Avalonia.Threading;

using AxView.Building;
using AxView.Hosting;
using AxView.Picker;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView;

/// <summary>Starts the picker UI, or previews a file given on the command line.</summary>
internal static class Program
{
    private const string Usage =
        "usage: axview [<file.axaml> [--output <app-bin-dir>] [--snapshot <file.png> [--view-model <type>] | --snapshot-all <dir>]]";

    [STAThread]
    public static int Main(string[] args)
    {
        string? axaml = null;
        string? outputDir = null;
        string? snapshot = null;
        string? snapshotAll = null;
        string? viewModel = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output" when i + 1 < args.Length:
                    outputDir = args[++i];
                    break;
                case "--snapshot" when i + 1 < args.Length:
                    snapshot = Path.GetFullPath(args[++i]);
                    break;
                case "--snapshot-all" when i + 1 < args.Length:
                    snapshotAll = Path.GetFullPath(args[++i]);
                    break;
                case "--view-model" when i + 1 < args.Length:
                    viewModel = args[++i];
                    break;
                case "-h" or "--help":
                    Console.WriteLine(Usage);
                    return 0;
                default:
                    axaml ??= args[i];
                    break;
            }
        }

        try
        {
            HostOptions.ValidateMode(snapshot, snapshotAll, viewModel);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"axview: {ex.Message}");
            Console.Error.WriteLine(Usage);
            return (int)ExitReason.BadArguments;
        }

        if (axaml is null)
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        try
        {
            RecentFiles recent = RecentFiles.ForCurrentUser();
            if ((snapshot is not null) || (snapshotAll is not null))
            {
                HostSession snapshotSession = HostSession.CreateDefault(recent.SettingsFile, new QuietBuildDisplay());
                return snapshotSession.RunAsync(axaml, outputDir, snapshot, recent.Add, snapshotAll, viewModel).GetAwaiter().GetResult();
            }

            HostSession session = HostSession.CreateDefault(recent.SettingsFile, new BuildWindowDisplay());
            return RunWithBuildWindows(() => session.RunAsync(axaml, outputDir, snapshotPath: null, recent.Add));
        }
        catch (PreviewSetupException ex)
        {
            Console.Error.WriteLine($"axview: {ex.Message}");
            return (int)ExitReason.SetupFailed;
        }
    }

    // The build window needs the UI thread. The session runs on it, and the main loop stops when the session stops.
    private static int RunWithBuildWindows(Func<Task<int>> run)
    {
        _ = BuildAvaloniaApp().SetupWithoutStarting();
        using CancellationTokenSource stop = new();
        Task<int> session = Dispatcher.UIThread.InvokeAsync(run);
        _ = session.ContinueWith(_ => stop.Cancel(), TaskScheduler.Default);
        Dispatcher.UIThread.MainLoop(stop.Token);
        return session.GetAwaiter().GetResult();
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<PickerApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
