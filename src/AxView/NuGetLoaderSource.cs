using System.Diagnostics;

namespace AxView;

/// <summary>Takes the XAML loader from the NuGet cache. Restores the package into the cache if it is missing.</summary>
/// <remarks>Apps do not usually ship the loader, so the launcher cannot take it from the app output.</remarks>
internal sealed class NuGetLoaderSource : ILoaderSource
{
    private const string PackageId = "avalonia.markup.xaml.loader";
    private const string AssemblyFile = "Avalonia.Markup.Xaml.Loader.dll";
    private static readonly string[] preferredTargets = ["net8.0", "net6.0", "netstandard2.0"];

    /// <summary>Returns the loader of the exact version in the packages folder, or null.</summary>
    /// <param name="packagesRoot">The NuGet global packages folder.</param>
    /// <param name="version">The Avalonia version.</param>
    public static string? FindInCache(string packagesRoot, string version)
    {
        string lib = Path.Combine(packagesRoot, PackageId, version.ToLowerInvariant(), "lib");
        return preferredTargets
            .Select(tfm => Path.Combine(lib, tfm, AssemblyFile))
            .FirstOrDefault(File.Exists);
    }

    /// <inheritdoc />
    public string Find(string avaloniaVersion)
    {
        string packagesRoot = NuGetPackagesRoot();
        string? cached = FindInCache(packagesRoot, avaloniaVersion);
        if (cached is not null)
        {
            return cached;
        }

        Restore(avaloniaVersion);
        return FindInCache(packagesRoot, avaloniaVersion)
            ?? throw new PreviewSetupException($"Restore did not produce {PackageId} {avaloniaVersion} in '{packagesRoot}'.");
    }

    private static string NuGetPackagesRoot()
    {
        string? configured = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
            : configured;
    }

    private static void Restore(string version)
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxView", "loader-restore", version);
        _ = Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "restore.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Avalonia.Markup.Xaml.Loader" Version="[{version}]" />
              </ItemGroup>
            </Project>
            """);

        Console.Error.WriteLine($"axview: restoring Avalonia.Markup.Xaml.Loader {version} ...");
        ProcessStartInfo info = new(DotnetHost.Path()) { WorkingDirectory = dir, UseShellExecute = false };
        info.ArgumentList.Add("restore");
        info.ArgumentList.Add("--nologo");
        using Process process = Process.Start(info) ?? throw new PreviewSetupException("Cannot start 'dotnet restore'.");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new PreviewSetupException($"'dotnet restore' of Avalonia.Markup.Xaml.Loader {version} failed.");
        }
    }
}
