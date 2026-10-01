namespace AxView;

/// <summary>A build output of an executable Avalonia app. The host runs inside its runtime.</summary>
internal sealed record AppOutput(string Directory, string DepsFile, string RuntimeConfig)
{
    /// <summary>The app's assembly name. The runtimeconfig file name carries it.</summary>
    public string AppName => Path.GetFileName(RuntimeConfig)[..^".runtimeconfig.json".Length];
}
