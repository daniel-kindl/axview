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
/// but does not run the target app's startup code.
/// </summary>
internal sealed class PreviewApp(HostOptions options, List<string> notes) : Application
{
    // Give layout, bindings, and image loads time to settle before the snapshot.
    private static readonly TimeSpan snapshotDelay = TimeSpan.FromMilliseconds(500);

    public override void Initialize() => notes.Add(TargetAppImporter.Import(this, options));

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = options.SnapshotPath is null
                ? new PreviewWindow(options, notes)
                : CreateSnapshotWindow(desktop, options.SnapshotPath);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private SnapshotWindow CreateSnapshotWindow(IClassicDesktopStyleApplicationLifetime desktop, string pngPath)
    {
        PreviewRenderer renderer = new(Assembly.Load(options.AssemblyName), options.ProjectDirectory);
        SnapshotWindow window = new(renderer, options.AxamlPath);
        window.Opened += (_, _) => _ = DispatcherTimer.RunOnce(() => desktop.Shutdown((int)window.Save(pngPath)), snapshotDelay);
        return window;
    }
}
