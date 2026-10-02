using System.Reflection;

using Avalonia;

namespace AxView.Protocol;

/// <summary>The facts that the About dialog shows.</summary>
/// <param name="Version">The version of the AxView assembly, without the source revision.</param>
/// <param name="BuildDate">The date that the AxView assembly was written.</param>
/// <param name="AvaloniaVersion">The version of the Avalonia assembly that runs in this process.</param>
internal sealed record AboutInfo(string Version, DateOnly BuildDate, string AvaloniaVersion)
{
    /// <summary>Reads the facts about an AxView assembly and the running Avalonia.</summary>
    /// <param name="axView">An assembly of AxView.</param>
    public static AboutInfo Read(Assembly axView)
    {
        string version = axView.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? axView.GetName().Version!.ToString(3);
        DateOnly buildDate = DateOnly.FromDateTime(File.GetLastWriteTime(axView.Location));
        string avaloniaVersion = typeof(Application).Assembly.GetName().Version!.ToString(3);
        return new AboutInfo(version.Split('+')[0], buildDate, avaloniaVersion);
    }
}
