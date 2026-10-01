using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using AxView.Protocol;

namespace AxView.Host;

/// <summary>
/// The host application. It takes the styles and resources of the target app,
/// but does not run the target app's startup code.
/// </summary>
internal sealed class PreviewApp(HostOptions options, List<string> notes) : Application
{
    public override void Initialize() => notes.Add(TargetAppImporter.Import(this, options));

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new PreviewWindow(options, notes);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
