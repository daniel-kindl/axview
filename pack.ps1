#Requires -Version 7.0

<#
.SYNOPSIS
Writes the source and binary archives of AxView to the artifacts folder.

.DESCRIPTION
The actions are:
- src: writes AxView_<yyyyMMdd>_<HHmmss>_src.zip from git HEAD. A dirty work tree stops the script, unless you give -Force.
- bin: publishes the launcher and the host, and writes AxView_<version>_<yyyyMMdd>_<HHmmss>_bin.zip.
  A smoke check then renders a small test app with the packed axview.dll.
- all: does src, then bin.
Each action deletes the old archives of its kind first.
The script uses only git, dotnet, and PowerShell 7. It runs on Windows, Linux, and macOS.

.PARAMETER Action
The action to do: src, bin, or all.

.PARAMETER Force
Writes the source archive also when the work tree has changes. The archive contains HEAD, not the changes.

.EXAMPLE
./pack.ps1 all
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet('src', 'bin', 'all')]
    [string] $Action,

    [switch] $Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$artifacts = Join-Path $PSScriptRoot 'artifacts'
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'

# The smoke app uses the Avalonia version of the launcher. The package cache already has it after a build.
$smokeAvaloniaVersion = '11.3.22'

function Invoke-Native {
    param([string] $FilePath, [string[]] $Arguments)

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$FilePath $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }
}

function Remove-OldArchive {
    param([string] $Kind)

    Get-ChildItem -Path $artifacts -Filter "AxView_*_$Kind.zip" -File | Remove-Item -Force
}

function New-SourceArchive {
    $changes = git -C $PSScriptRoot status --porcelain
    if ($changes -and -not $Force) {
        throw 'The work tree has changes. Commit them, or give -Force to archive HEAD without them.'
    }

    if ($changes) {
        Write-Warning 'The work tree has changes. The source archive contains HEAD without them.'
    }

    Remove-OldArchive 'src'
    $archive = Join-Path $artifacts "AxView_${stamp}_src.zip"
    Invoke-Native git @('-C', $PSScriptRoot, 'archive', '--format=zip', "--output=$archive", 'HEAD')
    Write-Host "[src] $archive"
}

function New-BinaryArchive {
    $publish = Join-Path $artifacts 'publish'
    if (Test-Path $publish) {
        Remove-Item $publish -Recurse -Force
    }

    # The launcher project copies the host into the host folder of the output.
    Invoke-Native dotnet @('publish', (Join-Path $PSScriptRoot 'src' 'AxView'), '-c', 'Release', '-o', $publish)

    # AboutInfo shows the informational version without the source revision.
    $version = (Get-Item (Join-Path $publish 'axview.dll')).VersionInfo.ProductVersion.Split('+')[0]

    Remove-OldArchive 'bin'
    $archive = Join-Path $artifacts "AxView_${version}_${stamp}_bin.zip"
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive
    Write-Host "[bin] $archive"

    Test-BinaryArchive $archive
}

# Unpacks the archive, and renders a view of a new small app with the packed launcher.
function Test-BinaryArchive {
    param([string] $Archive)

    # Outside the repo, the build rules of AxView do not apply to the smoke app.
    $smoke = Join-Path ([System.IO.Path]::GetTempPath()) "axview-smoke-$stamp"
    $snapshot = Join-Path $artifacts 'smoke.png'
    Remove-Item $snapshot -Force -ErrorAction SilentlyContinue
    try {
        $unpacked = Join-Path $smoke 'bin'
        Expand-Archive -Path $Archive -DestinationPath $unpacked

        $view = Write-SmokeApp (Join-Path $smoke 'SmokeApp')
        Invoke-Native dotnet @('build', (Join-Path $smoke 'SmokeApp'))
        Invoke-Native dotnet @((Join-Path $unpacked 'axview.dll'), $view, '--snapshot', $snapshot)
    }
    finally {
        Remove-Item $smoke -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (-not (Test-Path $snapshot)) {
        throw "The smoke check wrote no snapshot: $snapshot"
    }

    Write-Host "[smoke] $snapshot"
}

# Writes an Avalonia app with one view. Returns the path of the view.
function Write-SmokeApp {
    param([string] $Directory)

    $files = @{
        'SmokeApp.csproj'      = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="$smokeAvaloniaVersion" />
    <PackageReference Include="Avalonia.Desktop" Version="$smokeAvaloniaVersion" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="$smokeAvaloniaVersion" />
  </ItemGroup>
</Project>
"@
        'Program.cs'           = @'
using Avalonia;

namespace SmokeApp;

public static class Program
{
    public static void Main(string[] args) =>
        AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
'@
        'App.axaml'            = @'
<Application xmlns="https://github.com/avaloniaui" x:Class="SmokeApp.App"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
'@
        'App.axaml.cs'         = @'
using Avalonia;
using Avalonia.Markup.Xaml;

namespace SmokeApp;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
'@
        'Views/MainView.axaml' = @'
<UserControl xmlns="https://github.com/avaloniaui" Width="320" Height="120">
  <TextBlock Text="AxView smoke check" HorizontalAlignment="Center" VerticalAlignment="Center" />
</UserControl>
'@
    }

    foreach ($name in $files.Keys) {
        $path = Join-Path $Directory $name
        New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
        Set-Content -Path $path -Value $files[$name]
    }

    return Join-Path $Directory 'Views' 'MainView.axaml'
}

New-Item -ItemType Directory -Force $artifacts | Out-Null
switch ($Action) {
    'src' { New-SourceArchive }
    'bin' { New-BinaryArchive }
    'all' {
        New-SourceArchive
        New-BinaryArchive
    }
}
