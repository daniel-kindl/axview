using System.Diagnostics;
using System.Text.Json;

namespace AxView;

/// <summary>Finds the runtime XAML loader that matches the target app's Avalonia version.</summary>
/// <remarks>
/// The loader uses Avalonia internals, so its version must match the target's Avalonia exactly.
/// Apps do not usually ship the loader, so the launcher takes it from the NuGet cache.
/// </remarks>
internal static class LoaderLocator
{
    private const string PackageId = "avalonia.markup.xaml.loader";
    private const string AssemblyFile = "Avalonia.Markup.Xaml.Loader.dll";
    private static readonly string[] PreferredTargets = ["net8.0", "net6.0", "netstandard2.0"];

    public static string ReadAvaloniaVersion(string depsFile)
    {
        using FileStream stream = File.OpenRead(depsFile);
        using JsonDocument doc = JsonDocument.Parse(stream);
        if (doc.RootElement.TryGetProperty("libraries", out JsonElement libraries))
        {
            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                if (library.Name.StartsWith("Avalonia/", StringComparison.OrdinalIgnoreCase))
                {
                    return library.Name["Avalonia/".Length..];
                }
            }
        }

        throw new PreviewSetupException($"'{depsFile}' does not reference the Avalonia package.");
    }

    public static string? FindInCache(string packagesRoot, string version)
    {
        string lib = Path.Combine(packagesRoot, PackageId, version.ToLowerInvariant(), "lib");
        return PreferredTargets
            .Select(tfm => Path.Combine(lib, tfm, AssemblyFile))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>Returns the loader path. Restores the package into the NuGet cache if it is missing.</summary>
    public static string Ensure(string version)
    {
        string packagesRoot = NuGetPackagesRoot();
        string? cached = FindInCache(packagesRoot, version);
        if (cached is not null)
        {
            return cached;
        }

        Restore(version);
        return FindInCache(packagesRoot, version)
            ?? throw new PreviewSetupException($"Restore did not produce {PackageId} {version} in '{packagesRoot}'.");
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
