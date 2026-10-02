extern alias host;

using Avalonia.Headless.XUnit;
using Avalonia.Threading;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class ProjectPreviewTests
{
    private const string ViewAssembly = "AxView.Tests";
    private const string TextBlockXaml = """<TextBlock xmlns="https://github.com/avaloniaui" Text="Hello" />""";
    private static readonly TimeSpan stopTimeout = TimeSpan.FromSeconds(3);

    [AvaloniaFact]
    public void CodeChange_InAReferencedProject_RequestsARebuildOfTheShownFile()
    {
        using TempDir view = new();
        using TempDir library = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        string code = library.Write("Library.cs", "class Library { }");
        using ProjectPreview preview = Create(Options(view, file, library));
        preview.Show(file);

        File.WriteAllText(code, "class Library { int value; }");

        StopRequest request = Assert.IsType<StopRequest>(WaitForStop(preview));
        Assert.Equal(ExitReason.Rebuild, request.Reason);
        Assert.Equal(Path.GetFullPath(file), request.NextFile);
    }

    [AvaloniaFact]
    public void Open_FileOfAnotherProject_RequestsASwitchToThatFile()
    {
        using TempDir view = new();
        using TempDir other = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        string otherFile = other.Write("Views/Other.axaml", TextBlockXaml);
        using ProjectPreview preview = Create(Options(view, file));
        preview.Show(file);
        StopRequest? request = null;
        preview.StopRequested += (_, e) => request = e;

        preview.Open(otherFile);

        Assert.NotNull(request);
        Assert.Equal(ExitReason.SwitchProject, request.Reason);
        Assert.Equal(Path.GetFullPath(otherFile), request.NextFile);
    }

    [AvaloniaFact]
    public void Open_FileOfAnotherProjectWithoutHandOff_GivesAnError()
    {
        using TempDir view = new();
        using TempDir other = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        string otherFile = other.Write("Views/Other.axaml", TextBlockXaml);
        using ProjectPreview preview = Create(Options(view, file) with { HandOffFile = null });
        preview.Show(file);
        bool isStopRequested = false;
        string? error = null;
        preview.StopRequested += (_, _) => isStopRequested = true;
        preview.Failed += (_, message) => error = message;

        preview.Open(otherFile);

        Assert.False(isStopRequested);
        Assert.NotNull(error);
        Assert.Contains("another project", error);
        Assert.Equal(Path.GetFullPath(file), preview.FilePath);
    }

    [AvaloniaFact]
    public void Show_AnotherFileAndBack_KeepsTheViewModelOfTheFile()
    {
        using TempDir view = new();
        string first = view.Write("Views/First.axaml", TextBlockXaml);
        string second = view.Write("Views/Second.axaml", TextBlockXaml);
        using ProjectPreview preview = Create(Options(view, first));
        preview.Show(first);
        preview.SelectViewModel(typeof(FixtureOtherViewModel));

        preview.Show(second);
        string? secondViewModel = preview.ViewModelName;
        preview.Show(first);

        Assert.Null(secondViewModel);
        Assert.Equal(typeof(FixtureOtherViewModel).FullName, preview.ViewModelName);
        _ = Assert.IsType<FixtureOtherViewModel>(preview.View!.DataContext);
        Assert.Equal(typeof(FixtureOtherViewModel).FullName, preview.ViewModels[Path.GetFullPath(first)]);
    }

    private static ProjectPreview Create(HostOptions options)
    {
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));
        return new ProjectPreview(options, projects, DesignServices.None, appStyles, new Dictionary<string, string>());
    }

    private static HostOptions Options(TempDir view, string file, params TempDir[] referenced) =>
        new(file, view.Path, ViewAssembly, null, "Loader.dll", null, Path.Combine(view.Path, "hand-off.json"))
        {
            ReferencedProjects = referenced.Length == 0 ? null : string.Join(Path.PathSeparator, referenced.Select(r => r.Path)),
        };

    // Run the dispatcher loop until the preview requests a stop, or until the timeout.
    private static StopRequest? WaitForStop(ProjectPreview preview)
    {
        StopRequest? request = null;
        DispatcherFrame frame = new();
        void OnStopRequested(object? sender, StopRequest e)
        {
            request = e;
            frame.Continue = false;
        }

        preview.StopRequested += OnStopRequested;
        using (DispatcherTimer.RunOnce(() => frame.Continue = false, stopTimeout))
        {
            Dispatcher.UIThread.PushFrame(frame);
        }

        preview.StopRequested -= OnStopRequested;
        return request;
    }
}
