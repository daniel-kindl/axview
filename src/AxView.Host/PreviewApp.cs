using System.Reflection;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Host.UI;
using AxView.Protocol;

namespace AxView.Host;

/// <summary>
/// The host application. It takes the styles and resources of the target app,
/// but does not run the target app's startup code. It runs only the design services factory of the target app.
/// </summary>
internal sealed class PreviewApp(HostOptions options, List<string> notes) : Application
{
    // Give layout, bindings, and image loads time to settle before the snapshot.
    private static readonly TimeSpan snapshotDelay = TimeSpan.FromMilliseconds(500);

    private DesignServices services = DesignServices.None;

    // Load the services first. The factory can initialize static services that App.axaml needs.
    public override void Initialize()
    {
        services = LoadServices();
        notes.Add(TargetAppImporter.Import(this, options));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = options.SnapshotPath is null
                ? new PreviewWindow(options, notes, services)
                : CreateSnapshotWindow(desktop, options.SnapshotPath);
        }

        base.OnFrameworkInitializationCompleted();
    }

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
        SnapshotWindow window = new(renderer, options.AxamlPath);
        window.Opened += (_, _) => _ = DispatcherTimer.RunOnce(() => desktop.Shutdown((int)window.Save(pngPath)), snapshotDelay);
        return window;
    }
}
