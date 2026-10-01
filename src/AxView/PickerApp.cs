using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace AxView;

/// <summary>The launcher UI. It lets the user pick an AXAML file, then starts the preview host.</summary>
internal sealed class PickerApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new PickerWindow(RecentFiles.ForCurrentUser());
        }

        base.OnFrameworkInitializationCompleted();
    }
}
