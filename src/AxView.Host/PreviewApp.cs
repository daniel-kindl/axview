using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;

using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Host.UI;
using AxView.Protocol;

namespace AxView.Host;

/// <summary>
/// The host application. It gives the styles and resources of the target app to the preview only,
/// but does not run the target app's startup code. It runs only the design services factory of the target app.
/// </summary>
internal sealed class PreviewApp(HostOptions options, List<string> notes) : Application
{
    // Give layout, bindings, and image loads time to settle before the snapshot.
    private static readonly TimeSpan snapshotDelay = TimeSpan.FromMilliseconds(500);

    private DesignServices services = DesignServices.None;
    private AppStyles? appStyles;

    // Load the services first. The factory can initialize static services that App.axaml needs.
    // The target styles go only to the preview. The AxView chrome gets its own theme.
    public override void Initialize()
    {
        Projects = ProjectSet.Load(options);
        services = LoadServices();
        appStyles = AppStyles.Import(Projects, services, notes, LoadSettings().RunAppStartup);

        // The app gets the first copy. It is the import that ran the startup.
        // The preview finds Application.Current resources too. Thus it finds the resources that the startup adds.
        IStyle? chromeTheme = ChromeTheme.Load();
        if (chromeTheme is not null)
        {
            // The resources come first in the styles. Thus the chrome theme wins when both define a key.
            appStyles.AddResourcesTo(this);
            Styles.Add(chromeTheme);
            notes.Add($"AxView theme: {chromeTheme.GetType().Name}.");
            return;
        }

        // Without a theme, the chrome has no control templates. A copy of the app styles gives it templates.
        appStyles.AddTo(this);
        notes.Add("No Fluent or Simple theme in the app. The AxView chrome uses the app styles.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (options.Mode == HostMode.Preview)
            {
                desktop.MainWindow = new PreviewWindow(options, Projects, notes, services, ImportedAppStyles);
            }
            else
            {
                // A snapshot run opens and closes its own windows. Thus the app must not exit at the first close.
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                _ = SnapshotAsync(desktop);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Initialize loads the projects and the app styles. The framework calls it before each other member.
    private ProjectSet Projects
    {
        get => field ?? throw new InvalidOperationException("The app is not initialized.");
        set;
    }

    private AppStyles ImportedAppStyles => appStyles ?? throw new InvalidOperationException("The app is not initialized.");

    private Settings LoadSettings() => options.SettingsFile is null ? Settings.Default : Settings.Load(options.SettingsFile);

    private DesignServices LoadServices()
    {
        try
        {
            DesignServices loaded = DesignServices.Load(Projects.ViewAssembly);
            notes.Add(loaded == DesignServices.None ? "No AxViewDesign.CreateServices factory." : "Design services from AxViewDesign.");
            return loaded;
        }
        catch (TargetInvocationException ex)
        {
            // The factory is user code. The preview still works without its services.
            notes.Add($"Cannot create the design services: {ex.GetBaseException().Message}");
            return DesignServices.None;
        }
    }

    private async Task SnapshotAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        SnapshotRunner runner = new(Projects, services, ImportedAppStyles.CreateCopy, snapshotDelay);
        ExitReason reason;
        try
        {
            reason = await runner.RunAsync(options);
        }
        catch (Exception ex)
        {
            // The run must not end silently. Without a shutdown, the host would wait forever.
            await Console.Error.WriteLineAsync($"axview: error: {ex.Message}");
            reason = ExitReason.RenderFailed;
        }

        // A run can end before the main loop starts, for example on an unknown ViewModel. Thus the shutdown waits for the main loop.
        Dispatcher.UIThread.Post(() => desktop.Shutdown((int)reason));
    }
}
