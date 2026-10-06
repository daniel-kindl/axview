extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;

using AxView.Building;
using AxView.Picker;
using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Chrome;

/// <summary>Writes a PNG of each AxView window in each chrome look. An agent looks at the PNGs to check the chrome.</summary>
public sealed class ChromeShotTests
{
    private const string ViewXaml = """
        <StackPanel xmlns="https://github.com/avaloniaui" Margin="24" Spacing="8">
          <TextBlock Text="Sign in" FontSize="20" />
          <TextBox Watermark="User name" />
          <Button Content="Continue" />
        </StackPanel>
        """;

    private const string DataContextXaml = """
        <TextBlock xmlns="https://github.com/avaloniaui" xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                   xmlns:f="clr-namespace:AxView.Tests.Fixtures;assembly=AxView.Tests"
                   d:DataContext="{d:DesignInstance f:FixtureViewModel, IsDesignTimeCreatable=True}"
                   Margin="24" Text="{Binding Greeting}" />
        """;

    private const string BrokenXaml = """<StackPanel xmlns="https://github.com/avaloniaui"><Unknown /></StackPanel>""";

    public static TheoryData<ChromeLook> Looks => ChromeShots.Looks;

    [AvaloniaTheory]
    [MemberData(nameof(Looks))]
    public void PreviewWindow_WithAView(ChromeLook look) => SavePreview("preview-view", DataContextXaml, look);

    [AvaloniaTheory]
    [MemberData(nameof(Looks))]
    public void PreviewWindow_WithNoDataContext(ChromeLook look) => SavePreview("preview-no-datacontext", ViewXaml, look);

    [AvaloniaTheory]
    [MemberData(nameof(Looks))]
    public void PreviewWindow_WithAnError(ChromeLook look) => SavePreview("preview-error", BrokenXaml, look);

    [AvaloniaTheory]
    [MemberData(nameof(Looks))]
    public void PickerWindow_WithRecentFiles(ChromeLook look)
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "settings.json"));
        recent.Add(dir.Write("Desktop/Views/SettingsView.axaml"));
        recent.Add(dir.Write("Desktop/Views/MainView.axaml"));

        AssertSaved(() => new PickerWindow(recent), "picker-recent", look);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks))]
    public void PickerWindow_WithNoRecentFiles(ChromeLook look)
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "settings.json"));

        AssertSaved(() => new PickerWindow(recent), "picker-empty", look);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Looks))]
    public void BuildWindow_WithABuildLine(ChromeLook look) =>
        AssertSaved(() => CreateBuildWindow("  Desktop -> /work/Desktop/bin/Debug/net8.0/Desktop.dll"), "build", look);

    private static void SavePreview(string name, string xaml, ChromeLook look)
    {
        using TempDir view = new();
        string file = view.Write("Desktop/Views/MainView.axaml", xaml);
        _ = view.Write("Desktop/Views/SettingsView.axaml", ViewXaml);
        _ = view.Write("Desktop/Styles/Buttons.axaml", """<Styles xmlns="https://github.com/avaloniaui" />""");

        // The sidebar shows the name of the project folder.
        HostOptions options = new(file, Path.Combine(view.Path, "Desktop"), "AxView.Tests", null, "Loader.dll", null, null);
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));

        AssertSaved(() => new PreviewWindow(options, projects, [], DesignServices.None, appStyles), name, look);
    }

    private static BuildWindow CreateBuildWindow(string buildLine)
    {
        BuildWindow window = new("Desktop");
        window.Report(buildLine);
        return window;
    }

    private static void AssertSaved(Func<Window> createWindow, string name, ChromeLook look)
    {
        ChromeShot shot = ChromeShots.Save(createWindow, name, look);

        using Bitmap bitmap = new(shot.Png);
        Assert.Equal(shot.ClientSize.Width, bitmap.PixelSize.Width);
        Assert.Equal(shot.ClientSize.Height, bitmap.PixelSize.Height);
    }
}
