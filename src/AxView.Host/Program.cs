using System.Runtime.CompilerServices;

using Avalonia;

using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        HostOptions options;
        try
        {
            options = HostOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"axview-host: {ex.Message}");
            return (int)ExitReason.BadArguments;
        }

        // Register before any method that touches the loader is compiled.
        LoaderResolver.Register(options.LoaderPath);
        if (options.ChromeThemePath is not null)
        {
            LoaderResolver.Register(options.ChromeThemePath);
        }

        // Without an exit here, .NET stops the host with an unknown exit code and no report.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Environment.Exit((int)BackgroundErrors.Report(options, ex, Console.Error));
            }
        };

        return Run(options);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(HostOptions options)
    {
        List<string> extensions = [];
        AppBuilder builder = AppBuilder.Configure(() => new PreviewApp(options, extensions))
            .UsePlatformDetect()
            .LogToTrace();
        builder = BuilderExtensions.ApplyAvailable(builder, extensions);
        return builder.StartWithClassicDesktopLifetime([]);
    }
}
