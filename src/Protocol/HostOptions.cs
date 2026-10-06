namespace AxView.Protocol;

/// <summary>The options that the launcher gives to the host.</summary>
/// <remarks>
/// The command line is a list of <c>--key value</c> pairs. Each key has a value. The order of the pairs is free.
/// <see cref="Parse"/> rejects an unknown key, a missing value, a duplicate key, and options with no single <see cref="HostMode"/>.
/// </remarks>
/// <param name="AxamlPath">The first file to show.</param>
/// <param name="ProjectDirectory">The folder of the project that contains the file.</param>
/// <param name="AssemblyName">The assembly that the project builds.</param>
/// <param name="AppAssemblyName">The assembly of the target app, if it is not the project assembly.</param>
/// <param name="LoaderPath">The XAML loader assembly.</param>
/// <param name="SnapshotPath">If set, the host saves one PNG to this path and exits.</param>
/// <param name="HandOffFile">If set, the host writes a <see cref="HandOff"/> here before a project switch.</param>
/// <param name="SetupError">
/// If set, the host shows this error. The launcher cannot prepare the file, or the build of the app failed.
/// </param>
/// <param name="SettingsFile">If set, the host reads and writes its window, theme, and preview size in this <see cref="Settings"/> file.</param>
/// <param name="AppProjectDirectory">
/// The folder of the app project, if it is not the project folder. A change of its C# files also starts a rebuild.
/// </param>
/// <param name="SnapshotAllDirectory">
/// If set, the host saves one PNG for each AXAML file of the project, and a summary file, in this folder. Then it exits.
/// </param>
/// <param name="ViewModelName">
/// If set, the full name of the ViewModel type for the DataContext of a snapshot. The type is one of the ViewModel catalog.
/// </param>
/// <param name="ReferencedProjects">
/// If set, the folders of the projects that the view project and the app project reference. They are in one list, with
/// <see cref="Path.PathSeparator"/> between them. A change of their C# files also starts a rebuild.
/// </param>
/// <param name="ChromeThemePath">If set, the Simple theme assembly for the chrome. The launcher sets it when the app ships no theme.</param>
/// <param name="Size">If set, the size of each snapshot. <see cref="PreviewSize.Parse"/> reads the value.</param>
/// <param name="Theme">If set, the theme variant of each snapshot. <see cref="ParseTheme"/> reads the value.</param>
internal sealed record HostOptions(
    string AxamlPath,
    string ProjectDirectory,
    string AssemblyName,
    string? AppAssemblyName,
    string LoaderPath,
    string? SnapshotPath,
    string? HandOffFile,
    string? SetupError = null,
    string? SettingsFile = null,
    string? AppProjectDirectory = null,
    string? SnapshotAllDirectory = null,
    string? ViewModelName = null,
    string? ReferencedProjects = null,
    string? ChromeThemePath = null,
    PreviewSize? Size = null,
    PreviewTheme? Theme = null)
{
    private const string AxamlKey = "--axaml";
    private const string ProjectKey = "--project";
    private const string AssemblyKey = "--assembly";
    private const string AppAssemblyKey = "--app-assembly";
    private const string LoaderKey = "--loader";
    private const string SnapshotKey = "--snapshot";
    private const string HandOffKey = "--hand-off";
    private const string SetupErrorKey = "--setup-error";
    private const string SettingsKey = "--settings";
    private const string AppProjectKey = "--app-project";
    private const string SnapshotAllKey = "--snapshot-all";
    private const string ViewModelKey = "--view-model";
    private const string ReferencedProjectsKey = "--referenced-projects";
    private const string ChromeThemeKey = "--chrome-theme";
    private const string SizeKey = "--size";
    private const string ThemeKey = "--theme";

    private static readonly string[] keys =
    [
        AxamlKey, ProjectKey, AssemblyKey, AppAssemblyKey, LoaderKey, SnapshotKey, HandOffKey, SetupErrorKey, SettingsKey, AppProjectKey,
        SnapshotAllKey, ViewModelKey, ReferencedProjectsKey, ChromeThemeKey, SizeKey, ThemeKey,
    ];

    /// <summary>Gets the host mode. A snapshot folder selects <see cref="HostMode.SnapshotAll"/>, a snapshot path <see cref="HostMode.Snapshot"/>.</summary>
    public HostMode Mode =>
        SnapshotAllDirectory is not null ? HostMode.SnapshotAll : SnapshotPath is null ? HostMode.Preview : HostMode.Snapshot;

    /// <summary>Gets the folders of the referenced projects. The list is empty when there are none.</summary>
    public IReadOnlyList<string> ReferencedProjectDirectories =>
        ReferencedProjects?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [];

    /// <summary>Makes sure that the snapshot options select one host mode. The launcher and the host use the same check.</summary>
    /// <param name="snapshotPath">The value of <c>--snapshot</c>, or null.</param>
    /// <param name="snapshotAllDirectory">The value of <c>--snapshot-all</c>, or null.</param>
    /// <param name="viewModelName">The value of <c>--view-model</c>, or null.</param>
    /// <param name="size">The value of <c>--size</c>, or null.</param>
    /// <param name="theme">The value of <c>--theme</c>, or null.</param>
    /// <exception cref="ArgumentException">
    /// The options select more than one mode, a ViewModel without a snapshot, or a size or theme without a snapshot mode.
    /// </exception>
    public static void ValidateMode(
        string? snapshotPath, string? snapshotAllDirectory, string? viewModelName, PreviewSize? size = null, PreviewTheme? theme = null)
    {
        if (snapshotPath is not null && snapshotAllDirectory is not null)
        {
            throw new ArgumentException($"Use {SnapshotKey} or {SnapshotAllKey}, not both.");
        }

        if (viewModelName is not null && snapshotPath is null)
        {
            throw new ArgumentException($"{ViewModelKey} needs {SnapshotKey}.");
        }

        bool isSnapshot = snapshotPath is not null || snapshotAllDirectory is not null;
        if (size is not null && !isSnapshot)
        {
            throw new ArgumentException($"{SizeKey} needs {SnapshotKey} or {SnapshotAllKey}.");
        }

        if (theme is not null && !isSnapshot)
        {
            throw new ArgumentException($"{ThemeKey} needs {SnapshotKey} or {SnapshotAllKey}.");
        }
    }

    /// <summary>Reads the name of a theme variant. The name can have any case.</summary>
    /// <exception cref="ArgumentException">The text is not the name of a <see cref="PreviewTheme"/>.</exception>
    public static PreviewTheme ParseTheme(string text)
    {
        // Enum.TryParse also accepts numbers. Thus compare with the names.
        string name = Enum.GetNames<PreviewTheme>()
            .FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"The theme '{text}' is not Default, Light, or Dark.");
        return Enum.Parse<PreviewTheme>(name);
    }

    /// <summary>Reads the options from the host command line.</summary>
    /// <exception cref="ArgumentException">The arguments are not valid.</exception>
    public static HostOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count % 2 != 0)
        {
            throw new ArgumentException($"The key '{args[^1]}' has no value.", nameof(args));
        }

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int i = 0; i < args.Count; i += 2)
        {
            string key = args[i];
            if (!keys.Contains(key, StringComparer.Ordinal))
            {
                throw new ArgumentException($"Unknown key '{key}'.", nameof(args));
            }

            if (!values.TryAdd(key, args[i + 1]))
            {
                throw new ArgumentException($"The key '{key}' occurs more than once.", nameof(args));
            }
        }

        string Required(string key) =>
            values.TryGetValue(key, out string? value) ? value : throw new ArgumentException($"Missing {key}.", nameof(args));

        PreviewSize? size = values.TryGetValue(SizeKey, out string? sizeText) ? PreviewSize.Parse(sizeText) : null;
        PreviewTheme? theme = values.TryGetValue(ThemeKey, out string? themeText) ? ParseTheme(themeText) : null;
        string? snapshotPath = values.GetValueOrDefault(SnapshotKey);
        string? snapshotAllDirectory = values.GetValueOrDefault(SnapshotAllKey);
        string? viewModelName = values.GetValueOrDefault(ViewModelKey);
        ValidateMode(snapshotPath, snapshotAllDirectory, viewModelName, size, theme);
        return new HostOptions(
            Required(AxamlKey),
            Required(ProjectKey),
            Required(AssemblyKey),
            values.GetValueOrDefault(AppAssemblyKey),
            Required(LoaderKey),
            snapshotPath,
            values.GetValueOrDefault(HandOffKey),
            values.GetValueOrDefault(SetupErrorKey),
            values.GetValueOrDefault(SettingsKey),
            values.GetValueOrDefault(AppProjectKey),
            snapshotAllDirectory,
            viewModelName,
            values.GetValueOrDefault(ReferencedProjectsKey),
            values.GetValueOrDefault(ChromeThemeKey),
            size,
            theme);
    }

    /// <summary>Writes the options as the host command line.</summary>
    public IReadOnlyList<string> ToArguments()
    {
        List<string> args =
        [
            AxamlKey, AxamlPath,
            ProjectKey, ProjectDirectory,
            AssemblyKey, AssemblyName,
            LoaderKey, LoaderPath,
        ];
        AddOptional(args, AppAssemblyKey, AppAssemblyName);
        AddOptional(args, SnapshotKey, SnapshotPath);
        AddOptional(args, HandOffKey, HandOffFile);
        AddOptional(args, SetupErrorKey, SetupError);
        AddOptional(args, SettingsKey, SettingsFile);
        AddOptional(args, AppProjectKey, AppProjectDirectory);
        AddOptional(args, SnapshotAllKey, SnapshotAllDirectory);
        AddOptional(args, ViewModelKey, ViewModelName);
        AddOptional(args, ReferencedProjectsKey, ReferencedProjects);
        AddOptional(args, ChromeThemeKey, ChromeThemePath);
        AddOptional(args, SizeKey, Size?.ToArgument());
        AddOptional(args, ThemeKey, Theme?.ToString());
        return args;
    }

    private static void AddOptional(List<string> args, string key, string? value)
    {
        if (value is not null)
        {
            args.Add(key);
            args.Add(value);
        }
    }
}
