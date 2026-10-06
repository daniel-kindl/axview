using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using AxView.Hosting;
using AxView.Resolution;

namespace AxView.Packages;

/// <summary>Takes the assembly of a package from the NuGet cache. Restores the package into the cache if it is missing.</summary>
/// <remarks>Apps do not usually ship the XAML loader, so the launcher cannot take it from the app output.</remarks>
internal sealed class NuGetPackageSource : IPackageSource
{
    private static readonly string[] preferredTargets = ["net8.0", "net6.0", "netstandard2.0"];

    /// <summary>Returns the assembly of the package of the exact version in the packages folder, or null.</summary>
    /// <param name="packagesRoot">The NuGet global packages folder.</param>
    /// <param name="packageId">The package. Its assembly has the same name.</param>
    /// <param name="version">The Avalonia version.</param>
    [SuppressMessage("Globalization", "CA1308", Justification = "The NuGet cache uses lowercase folder names.")]
    public static string? FindInCache(string packagesRoot, string packageId, string version)
    {
        string lib = Path.Combine(packagesRoot, packageId.ToLowerInvariant(), version.ToLowerInvariant(), "lib");
        return preferredTargets
            .Select(tfm => Path.Combine(lib, tfm, packageId + ".dll"))
            .FirstOrDefault(File.Exists);
    }

    /// <inheritdoc />
    public string Find(string packageId, string avaloniaVersion)
    {
        string packagesRoot = NuGetPackagesRoot();
        string? cached = FindInCache(packagesRoot, packageId, avaloniaVersion);
        if (cached is not null)
        {
            return cached;
        }

        Restore(packageId, avaloniaVersion);
        return FindInCache(packagesRoot, packageId, avaloniaVersion)
            ?? throw new PreviewSetupException($"Restore did not produce {packageId} {avaloniaVersion} in '{packagesRoot}'.");
    }

    private static string NuGetPackagesRoot()
    {
        string? configured = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
            : configured;
    }

    private static void Restore(string packageId, string version)
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxView", "package-restore", packageId, version);
        _ = Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "restore.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{packageId}" Version="[{version}]" />
              </ItemGroup>
            </Project>
            """);

        Console.Error.WriteLine($"axview: restoring {packageId} {version} ...");
        ProcessStartInfo info = new(DotnetHost.Path()) { WorkingDirectory = dir, UseShellExecute = false };
        info.ArgumentList.Add("restore");
        info.ArgumentList.Add("--nologo");
        using Process process = Process.Start(info) ?? throw new PreviewSetupException("Cannot start 'dotnet restore'.");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new PreviewSetupException($"'dotnet restore' of {packageId} {version} failed.");
        }
    }
}
