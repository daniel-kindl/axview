namespace AxView.Protocol;

/// <summary>The options that the launcher gives to the host.</summary>
/// <remarks>
/// The command line is a list of <c>--key value</c> pairs. Each key has a value. The order of the pairs is free.
/// <see cref="Parse"/> rejects an unknown key, a missing value, and a duplicate key.
/// </remarks>
/// <param name="AxamlPath">The first file to show.</param>
/// <param name="ProjectDirectory">The folder of the project that contains the file.</param>
/// <param name="AssemblyName">The assembly that the project builds.</param>
/// <param name="AppAssemblyName">The assembly of the target app, if it is not the project assembly.</param>
/// <param name="LoaderPath">The XAML loader assembly.</param>
/// <param name="SnapshotPath">If set, the host saves one PNG to this path and exits.</param>
/// <param name="HandOffFile">If set, the host writes a <see cref="HandOff"/> here before a project switch.</param>
/// <param name="SetupError">If set, the host shows this error. The launcher cannot prepare the file that the user opened.</param>
/// <param name="SettingsFile">If set, the host reads and writes its window, theme, and preview size in this <see cref="Settings"/> file.</param>
internal sealed record HostOptions(
    string AxamlPath,
    string ProjectDirectory,
    string AssemblyName,
    string? AppAssemblyName,
    string LoaderPath,
    string? SnapshotPath,
    string? HandOffFile,
    string? SetupError = null,
    string? SettingsFile = null)
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

    private static readonly string[] keys =
        [AxamlKey, ProjectKey, AssemblyKey, AppAssemblyKey, LoaderKey, SnapshotKey, HandOffKey, SetupErrorKey, SettingsKey];

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

        return new HostOptions(
            Required(AxamlKey),
            Required(ProjectKey),
            Required(AssemblyKey),
            values.GetValueOrDefault(AppAssemblyKey),
            Required(LoaderKey),
            values.GetValueOrDefault(SnapshotKey),
            values.GetValueOrDefault(HandOffKey),
            values.GetValueOrDefault(SetupErrorKey),
            values.GetValueOrDefault(SettingsKey));
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
