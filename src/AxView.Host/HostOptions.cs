namespace AxView.Host;

/// <summary>The arguments that the launcher passes to the host.</summary>
internal sealed record HostOptions(
    string AxamlPath,
    string ProjectDirectory,
    string AssemblyName,
    string? AppAssemblyName,
    string LoaderPath,
    string? SnapshotPath,
    string? SwitchFile)
{
    /// <summary>The host exits with this code after it writes a file of another project to <see cref="SwitchFile"/>.</summary>
    /// <remarks>The launcher defines the same value. Change both.</remarks>
    public const int SwitchProjectExitCode = 3;

    public static HostOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            values[args[i]] = args[i + 1];
        }

        string Required(string key) =>
            values.TryGetValue(key, out string? value) ? value : throw new ArgumentException($"Missing {key}.");

        return new HostOptions(
            Required("--axaml"),
            Required("--project"),
            Required("--assembly"),
            values.GetValueOrDefault("--app-assembly"),
            Required("--loader"),
            values.GetValueOrDefault("--snapshot"),
            values.GetValueOrDefault("--switch-file"));
    }
}
