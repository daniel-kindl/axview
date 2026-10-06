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
        "usage: axview [<file.axaml> [--output <app-bin-dir>] [--snapshot <file.png> [--view-model <type>] [--tree <file.json>] | --snapshot-all <dir>]" +
        " [--size <Fit|Phone|Tablet|Desktop|WxH>] [--theme <Default|Light|Dark>]]";

    [STAThread]
    public static int Main(string[] args)
    {
        string? axaml = null;
        string? outputDir = null;
        string? snapshot = null;
        string? snapshotAll = null;
        string? viewModel = null;
        string? sizeText = null;
        string? themeText = null;
        string? tree = null;
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
                case "--size" when i + 1 < args.Length:
                    sizeText = args[++i];
                    break;
                case "--theme" when i + 1 < args.Length:
                    themeText = args[++i];
                    break;
                case "--tree" when i + 1 < args.Length:
                    tree = Path.GetFullPath(args[++i]);
                    break;
                case "-h" or "--help":
                    Console.WriteLine(Usage);
                    return 0;
                default:
                    axaml ??= args[i];
                    break;
            }
        }

        SnapshotRequest request;
        try
        {
            PreviewSize? size = sizeText is null ? null : PreviewSize.Parse(sizeText);
            PreviewTheme? theme = themeText is null ? null : HostOptions.ParseTheme(themeText);
            request = new SnapshotRequest(snapshot, snapshotAll, viewModel, size, theme, tree);
            HostOptions.ValidateMode(request);
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
            if (request.IsSnapshot)
            {
                using QuietBuildDisplay quietDisplay = new();
                HostSession snapshotSession = HostSession.CreateDefault(recent.SettingsFile, quietDisplay);
                return snapshotSession.RunAsync(axaml, outputDir, recent.Add, request).GetAwaiter().GetResult();
            }

            HostSession session = HostSession.CreateDefault(recent.SettingsFile, new BuildWindowDisplay());
            return RunWithBuildWindows(() => session.RunAsync(axaml, outputDir, recent.Add));
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
