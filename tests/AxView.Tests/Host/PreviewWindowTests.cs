extern alias host;

using Avalonia.Headless.XUnit;
using Avalonia.Threading;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class PreviewWindowTests
{
    private const string TextBlockXaml = """<TextBlock xmlns="https://github.com/avaloniaui" Text="Hello" />""";

    [AvaloniaFact]
    public void Show_ShowsTheStateOfTheModel()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        HostOptions options = new(file, view.Path, "AxView.Tests", null, "Loader.dll", null, null)
        {
            SettingsFile = Path.Combine(view.Path, "settings.json"),
        };
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));
        PreviewWindow window = new(options, projects, [], DesignServices.None, appStyles);

        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Close();

        Assert.Equal($"AxView - {Path.Combine("Views", "Main.axaml")}", window.Title);
        Assert.True(File.Exists(options.SettingsFile));
    }
}
