extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class PreviewWindowModelTests
{
    private const string ViewAssembly = "AxView.Tests";
    private const string TextBlockXaml = """<TextBlock xmlns="https://github.com/avaloniaui" Text="Hello" />""";
    private const string StylesXaml = """<Styles xmlns="https://github.com/avaloniaui"><Style Selector="TextBlock" /></Styles>""";
    private const string GalleryTitle = "Control gallery";
    private static readonly TimeSpan changeTimeout = TimeSpan.FromSeconds(3);

    [AvaloniaFact]
    public void Entries_PutTheGalleryFirst_AndSelectTheShownFile()
    {
        using TempDir view = new();
        string first = view.Write("Views/First.axaml", TextBlockXaml);
        _ = view.Write("Views/Second.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, first));

        Assert.Equal([GalleryTitle, "First", "Second"], model.Entries.Select(e => e.Title));
        Assert.True(model.Entries[0].IsGallery);
        Assert.Equal("Views", model.Entries[1].Detail);
        Assert.Equal(model.Entries[1], model.SelectedEntry);
        Assert.Equal($"AxView - {Path.Combine("Views", "First.axaml")}", model.Title);
    }

    [AvaloniaFact]
    public void Entries_GiveTheKindOfEachFile()
    {
        using TempDir view = new();
        string main = view.Write("Views/Main.axaml", TextBlockXaml);
        _ = view.Write("Styles/Buttons.axaml", StylesXaml);
        _ = view.Write("Styles/Single.axaml", """<Style xmlns="https://github.com/avaloniaui" Selector="Button" />""");
        _ = view.Write("Styles/Colors.axaml", """<ResourceDictionary xmlns="https://github.com/avaloniaui" />""");
        _ = view.Write("Views/Broken.axaml", "<TextBlock");
        using PreviewWindowModel model = Create(Options(view, main));

        Dictionary<string, FileKind> kinds = model.Entries.ToDictionary(e => e.Title, e => e.Kind);

        Assert.Equal(FileKind.Gallery, kinds[GalleryTitle]);
        Assert.Equal(FileKind.View, kinds["Main"]);
        Assert.Equal(FileKind.Styles, kinds["Buttons"]);
        Assert.Equal(FileKind.Styles, kinds["Single"]);
        Assert.Equal(FileKind.Resources, kinds["Colors"]);
        Assert.Equal(FileKind.View, kinds["Broken"]);
    }

    [AvaloniaFact]
    public void OutputDirectory_IsTheFolderOfTheViewAssembly_AndNotInTheStatus()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file));

        string expected = Path.GetDirectoryName(typeof(PreviewWindowModelTests).Assembly.Location)!;

        Assert.Equal(expected, model.OutputDirectory);
        Assert.DoesNotContain(expected, model.Status, StringComparison.Ordinal);
        Assert.StartsWith("Rendered", model.Status, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void OutputDirectory_IsTheOutputOfTheOptions_WhenTheHostRunsInACopy()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        string output = Path.Combine(view.Path, "bin", "Debug", "net9.0");
        using PreviewWindowModel model = Create(Options(view, file) with { OutputDirectory = output });

        Assert.Equal(output, model.OutputDirectory);
    }

    [AvaloniaFact]
    public void SetFilter_KeepsTheMatchingEntries_AndTheSelection()
    {
        using TempDir view = new();
        string first = view.Write("Views/First.axaml", TextBlockXaml);
        _ = view.Write("Views/Second.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, first));

        model.SetFilter("first");
        IReadOnlyList<string> files = [.. model.Entries.Select(e => e.Title)];
        FileListEntry? selected = model.SelectedEntry;
        model.SetFilter("GALLERY");

        Assert.Equal(["First"], files);
        Assert.Equal("First", selected?.Title);
        Assert.True(Assert.Single(model.Entries).IsGallery);
        Assert.Null(model.SelectedEntry);
    }

    [AvaloniaFact]
    public void Select_TheGallery_ShowsTheGallery()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file));
        Control? fileView = model.Content;

        model.Select(model.Entries[0]);

        Assert.NotNull(model.Content);
        Assert.NotSame(fileView, model.Content);
        Assert.True(model.SelectedEntry?.IsGallery);
        Assert.Equal($"AxView - {GalleryTitle}", model.Title);
        Assert.StartsWith(GalleryTitle, model.Status, StringComparison.Ordinal);
        Assert.False(model.CanSelectViewModel);
        Assert.False(model.IsDataContextMissing);
    }

    [AvaloniaFact]
    public void Select_TheShownEntry_DoesNotLoadAgain()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file));
        Control? shown = model.Content;
        bool isChanged = false;
        model.Changed += (_, _) => isChanged = true;

        model.Select(model.SelectedEntry!);

        Assert.False(isChanged);
        Assert.Same(shown, model.Content);
    }

    [AvaloniaFact]
    public void FileChange_WhileTheGalleryShows_MakesTheGalleryAgain_AndHidesTheFileError()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file));
        model.Select(model.Entries[0]);
        Control? gallery = model.Content;

        File.WriteAllText(file, "<TextBlock");

        Assert.True(WaitForChange(model));
        Assert.NotNull(model.Content);
        Assert.NotSame(gallery, model.Content);
        Assert.Null(model.Error);
        Assert.StartsWith(GalleryTitle, model.Status, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Select_AFileAfterTheGallery_ShowsTheFile()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file));
        model.Select(model.Entries[0]);

        model.Select(model.Entries[1]);

        _ = Assert.IsType<TextBlock>(model.Content);
        Assert.Equal("Main", model.SelectedEntry?.Title);
        Assert.True(model.CanSelectViewModel);
        Assert.StartsWith("Rendered", model.Status, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void IsDataContextMissing_IsTrueOnlyForAControlWithoutDataContext()
    {
        using TempDir view = new();
        string control = view.Write("Views/Main.axaml", TextBlockXaml);
        string styles = view.Write("Styles/Main.axaml", StylesXaml);
        using PreviewWindowModel model = Create(Options(view, control));
        bool isControlMissing = model.IsDataContextMissing;

        model.Open(styles);

        Assert.True(isControlMissing);
        Assert.False(model.IsDataContextMissing);
    }

    [AvaloniaFact]
    public void SetupError_ShowsUntilTheNextLoad()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file) with { SetupError = "The build failed." });
        string? setupError = model.Error;
        string status = model.Status;

        File.WriteAllText(file, TextBlockXaml.Replace("Hello", "Again", StringComparison.Ordinal));

        Assert.True(WaitForChange(model));
        Assert.Equal("The build failed.", setupError);
        Assert.StartsWith("Error", status, StringComparison.Ordinal);
        Assert.Null(model.Error);
    }

    [AvaloniaFact]
    public void RefreshCatalog_WithoutTheProjectFolder_ShowsTheError_AndKeepsTheEntries()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file));
        IReadOnlyList<FileListEntry> entries = model.Entries;
        Directory.Delete(view.Path, recursive: true);

        model.RefreshCatalog();

        Assert.Contains("Cannot list the project files", model.Error, StringComparison.Ordinal);
        Assert.StartsWith("Error", model.Status, StringComparison.Ordinal);
        Assert.Equal(entries, model.Entries);
    }

    [AvaloniaFact]
    public void BrokenFile_KeepsTheLastGoodView_AndTellsIt()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file));
        Control? good = model.Content;

        File.WriteAllText(file, "<TextBlock");

        Assert.True(WaitForChange(model));
        Assert.Same(good, model.Content);
        Assert.NotNull(model.Error);
        Assert.Contains("Showing the last good render", model.Status, StringComparison.Ordinal);
        Assert.True(model.IsLastGoodRenderShown);
    }

    [AvaloniaFact]
    public void BrokenFile_WithNoGoodLoad_ShowsNoLastGoodRender()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", "<TextBlock");

        using PreviewWindowModel model = Create(Options(view, file));

        Assert.NotNull(model.Error);
        Assert.False(model.IsLastGoodRenderShown);
    }

    [AvaloniaFact]
    public void GoodFile_ShowsNoLastGoodRender()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);

        using PreviewWindowModel model = Create(Options(view, file));

        Assert.Null(model.Error);
        Assert.False(model.IsLastGoodRenderShown);
    }

    [AvaloniaFact]
    public void Open_FileOfAnotherProjectWithoutHandOff_ShowsTheError()
    {
        using TempDir view = new();
        using TempDir other = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        string otherFile = other.Write("Views/Other.axaml", TextBlockXaml);
        using PreviewWindowModel model = Create(Options(view, file) with { HandOffFile = null });

        model.Open(otherFile);

        Assert.Contains("another project", model.Error, StringComparison.Ordinal);
        Assert.StartsWith("Error", model.Status, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Open_FileOfAnotherProject_SavesTheSettingsAndTheHandOff_ThenStops()
    {
        using TempDir view = new();
        using TempDir other = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        string otherFile = other.Write("Views/Other.axaml", TextBlockXaml);
        HostOptions options = Options(view, file);
        int? exitCode = null;
        using PreviewWindowModel model = Create(options, code => exitCode = code);

        model.Open(otherFile);

        int code = Assert.NotNull(exitCode);
        Assert.Equal((int)ExitReason.SwitchProject, code);
        Assert.Equal(Path.GetFullPath(otherFile), HandOff.Take(options.HandOffFile!, code)?.NextFile);
        Assert.Equal(PreviewTheme.Dark, Settings.Load(options.SettingsFile!).Theme);
    }

    [AvaloniaFact]
    public void SelectViewModel_KeepsTheChoiceForTheNextHost()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        HostOptions options = Options(view, file);
        using (PreviewWindowModel first = Create(options))
        {
            first.SelectViewModel(typeof(FixtureOtherViewModel));
        }

        using PreviewWindowModel second = Create(options);

        Assert.Equal(typeof(FixtureOtherViewModel).FullName, second.ViewModelName);
        _ = Assert.IsType<FixtureOtherViewModel>(second.Content?.DataContext);
    }

    [AvaloniaFact]
    public void SaveSettings_AddsTheWindowState()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        HostOptions options = Options(view, file);
        using PreviewWindowModel model = Create(options);

        model.SaveSettings();

        Assert.Equal(PreviewTheme.Dark, Settings.Load(options.SettingsFile!).Theme);
    }

    private static PreviewWindowModel Create(HostOptions options, Action<int>? shutdown = null)
    {
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));
        return new PreviewWindowModel(
            options,
            projects,
            DesignServices.None,
            appStyles,
            settings => settings with { Theme = PreviewTheme.Dark },
            shutdown ?? (_ => { }));
    }

    private static HostOptions Options(TempDir view, string file) =>
        new(file, view.Path, ViewAssembly, null, "Loader.dll", null, Path.Combine(view.Path, "hand-off.json"))
        {
            SettingsFile = Path.Combine(view.Path, "settings.json"),
        };

    // Run the dispatcher loop until the model changes, or until the timeout. True when the model changed.
    private static bool WaitForChange(PreviewWindowModel model)
    {
        bool isChanged = false;
        DispatcherFrame frame = new();
        void OnChanged(object? sender, EventArgs e)
        {
            isChanged = true;
            frame.Continue = false;
        }

        model.Changed += OnChanged;
        using (DispatcherTimer.RunOnce(() => frame.Continue = false, changeTimeout))
        {
            Dispatcher.UIThread.PushFrame(frame);
        }

        model.Changed -= OnChanged;
        return isChanged;
    }
}
