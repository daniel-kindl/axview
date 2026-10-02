using System.Reflection;

using Avalonia;

using AxView.Protocol;

namespace AxView.Tests.Launcher;

public sealed class AboutInfoTests
{
    [Fact]
    public void Read_GivesTheVersionWithoutTheSourceRevision()
    {
        Assembly assembly = typeof(AboutInfo).Assembly;
        string informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        AboutInfo info = AboutInfo.Read(assembly);

        Assert.Equal(informational.Split('+')[0], info.Version);
        Assert.DoesNotContain('+', info.Version);
    }

    [Fact]
    public void Read_GivesTheWriteDateOfTheAssemblyAsTheBuildDate()
    {
        Assembly assembly = typeof(AboutInfo).Assembly;

        AboutInfo info = AboutInfo.Read(assembly);

        Assert.Equal(DateOnly.FromDateTime(File.GetLastWriteTime(assembly.Location)), info.BuildDate);
    }

    [Fact]
    public void Read_GivesTheAvaloniaVersionOfTheRuntime()
    {
        AboutInfo info = AboutInfo.Read(typeof(AboutInfo).Assembly);

        Assert.Equal(typeof(Application).Assembly.GetName().Version!.ToString(3), info.AvaloniaVersion);
    }
}
