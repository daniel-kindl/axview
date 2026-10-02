using Avalonia;

using AxView.Hosting;
using AxView.Picker;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView;

/// <summary>Starts the picker UI, or previews a file given on the command line.</summary>
internal static class Program
{
    private const string Usage = "usage: axview [<file.axaml> [--output <app-bin-dir>] [--snapshot <file.png> | --snapshot-all <dir>] [--view-model <type>]]";

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

        if (axaml is null)
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        try
        {
            RecentFiles recent = RecentFiles.ForCurrentUser();
            HostSession session = HostSession.CreateDefault(recent.SettingsFile);
            return session.RunAsync(axaml, outputDir, snapshot, recent.Add, snapshotAll, viewModel).GetAwaiter().GetResult();
        }
        catch (PreviewSetupException ex)
        {
            Console.Error.WriteLine($"axview: {ex.Message}");
            return (int)ExitReason.SetupFailed;
        }
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<PickerApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
