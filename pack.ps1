#Requires -Version 7.0

<#
.SYNOPSIS
Writes the source and binary archives of AxView to the artifacts folder.

.DESCRIPTION
The actions are:
- src: writes AxView_<yyyyMMdd>_<HHmmss>_src.zip from git HEAD. A dirty work tree stops the script, unless you give -Force.
- bin: publishes the launcher and the host, and writes AxView_<version>_<yyyyMMdd>_<HHmmss>_bin.zip.
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

    # The launcher project copies each host build into its host folder of the output.
    Invoke-Native dotnet @('publish', (Join-Path $PSScriptRoot 'src' 'AxView'), '-c', 'Release', '-o', $publish)

    # AboutInfo shows the informational version without the source revision.
    $version = (Get-Item (Join-Path $publish 'axview.dll')).VersionInfo.ProductVersion.Split('+')[0]

    Remove-OldArchive 'bin'
    $archive = Join-Path $artifacts "AxView_${version}_${stamp}_bin.zip"
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive
    Write-Host "[bin] $archive"
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
