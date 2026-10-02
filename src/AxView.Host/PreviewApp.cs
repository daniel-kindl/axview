using System.Reflection;

using Avalonia;
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
    private TargetStyles target = TargetStyles.None;

    // Load the services first. The factory can initialize static services that App.axaml needs.
    // The target styles go only to the preview. The AxView chrome gets its own theme.
    public override void Initialize()
    {
        services = LoadServices();
        target = TargetAppImporter.Import(options, notes, LoadSettings().RunAppStartup);
        IStyle? chromeTheme = ChromeTheme.Load();
        if (chromeTheme is not null)
        {
            Styles.Add(chromeTheme);
            notes.Add($"AxView theme: {chromeTheme.GetType().Name}.");
            return;
        }

        // Without a theme, the chrome has no control templates. A second instance of the app styles gives it templates.
        TargetAppImporter.Import(options, []).ApplyTo(this);
        notes.Add("No Fluent or Simple theme in the app. The AxView chrome uses the app styles.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = options.SnapshotPath is null
                ? new PreviewWindow(options, notes, services, target)
                : CreateSnapshotWindow(desktop, options.SnapshotPath);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private Settings LoadSettings() => options.SettingsFile is null ? Settings.Default : Settings.Load(options.SettingsFile);

    private DesignServices LoadServices()
    {
        try
        {
            DesignServices loaded = DesignServices.Load(Assembly.Load(options.AssemblyName));
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

    private SnapshotWindow CreateSnapshotWindow(IClassicDesktopStyleApplicationLifetime desktop, string pngPath)
    {
        PreviewRenderer renderer = new(Assembly.Load(options.AssemblyName), options.ProjectDirectory, services);
        SnapshotWindow window = new(renderer, options.AxamlPath, target);
        window.Opened += (_, _) => _ = DispatcherTimer.RunOnce(() => desktop.Shutdown((int)window.Save(pngPath)), snapshotDelay);
        return window;
    }
}
