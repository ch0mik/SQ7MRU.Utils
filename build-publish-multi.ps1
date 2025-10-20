<#
.SYNOPSIS
    Publish .NET application as single-file, self-contained, ReadyToRun binaries for multiple RIDs.

.DESCRIPTION
    Runs `dotnet publish` for a list of Runtime Identifiers (RIDs) and produces outputs
    under the `./publish/<rid>/<configuration>` folder. Each publish uses:
        - PublishSingleFile=true
        - SelfContained=true
        - PublishReadyToRun=true

.PARAMETER ProjectPath
    Path to the project (.csproj) or solution/folder. Defaults to current directory.

.PARAMETER RIDs
    Array of runtime identifiers to publish for. Defaults to a common set covering
    Windows, macOS and Linux.

.PARAMETER Framework
    Target framework (e.g. net9.0). If not provided, uses the target(s) from the project.

.PARAMETER Configuration
    Build configuration: Debug or Release. Defaults to Release.

.PARAMETER Verbose
    Switch to enable verbose logging.

EXAMPLE
    .\build-publish-multi.ps1 -ProjectPath './SampleApp' -RIDs 'win-x64','osx-arm64','linux-x64' -Framework net9.0

NOTES
    - RIDs reference: https://learn.microsoft.com/dotnet/core/rid-catalog
    - Requires dotnet SDK on PATH.
#>

[CmdletBinding()]
param(
    [string]$ProjectPath = ".",
    [string[]]$RIDs = @(
        'win-x64',
        'win-x86',
        'osx-x64',
        'osx-arm64',
        'linux-x64',
        'linux-arm64'
    ),
    [string]$Framework = $null,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release'
    , [switch]$NoZip
)

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $time = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    if ($Level -eq 'ERROR') { Write-Host "[$time] [$Level] $Message" -ForegroundColor Red }
    elseif ($Level -eq 'WARN') { Write-Host "[$time] [$Level] $Message" -ForegroundColor Yellow }
    else { Write-Host "[$time] [$Level] $Message" }
}

function Ensure-DotNet {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Log "'dotnet' command not found on PATH. Please install .NET SDK: https://dotnet.microsoft.com/download" 'ERROR'
        exit 2
    }
}

Ensure-DotNet

$startDir = Get-Location
try {
    $projectRoot = Resolve-Path -Path $ProjectPath -ErrorAction Stop
} catch {
    Write-Log "Cannot resolve path '$ProjectPath'" 'ERROR'
    exit 3
}

if ($Framework) {
    Write-Log "Target framework: $Framework"
} else {
    Write-Log "Target framework: (not specified) will use project defaults"
}

Write-Log "Configuration: $Configuration"

 $publishRoot = Join-Path -Path $projectRoot.Path -ChildPath 'publish'
 # central artifacts folder (zips) - place next to this script (typically repo root)
 $scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
 $artifactsRoot = Join-Path -Path $scriptDir -ChildPath 'artifacts'
 if (-not (Test-Path $artifactsRoot)) { New-Item -ItemType Directory -Path $artifactsRoot | Out-Null }
if (-not (Test-Path $publishRoot)) { New-Item -ItemType Directory -Path $publishRoot | Out-Null }

$summary = @()

# Determine which project files to publish.
function Get-ProjectFilesForPath {
    param([string]$Path)

    if (Test-Path $Path -PathType Leaf) {
        $resolved = Resolve-Path $Path
        $ext = [IO.Path]::GetExtension($resolved.Path)
        if ($ext -ieq '.csproj') {
            return @( $resolved.Path )
        } elseif ($ext -ieq '.sln') {
            $slnDir = Split-Path -Path $resolved.Path -Parent
            return Get-ChildItem -Path $slnDir -Recurse -Filter *.csproj -File | Select-Object -ExpandProperty FullName
        } else {
            return @()
        }
    } elseif (Test-Path $Path -PathType Container) {
        return Get-ChildItem -Path (Resolve-Path $Path).Path -Recurse -Filter *.csproj -File | Select-Object -ExpandProperty FullName
    } else {
        return @()
    }
}

function Get-TargetFrameworksFromCsproj {
    param([string]$csproj)
    try {
        $content = Get-Content -Raw -Path $csproj -ErrorAction Stop
    } catch {
        return @()
    }
    $tfMatch = [regex]::Match($content, '<TargetFramework>(.*?)</TargetFramework>', 'Singleline')
    if ($tfMatch.Success) { return @($tfMatch.Groups[1].Value.Trim()) }
    $tfsMatch = [regex]::Match($content, '<TargetFrameworks>(.*?)</TargetFrameworks>', 'Singleline')
    if ($tfsMatch.Success) { return ($tfsMatch.Groups[1].Value -split ';') | ForEach-Object { $_.Trim() } }
    return @()
}

$projectFiles = Get-ProjectFilesForPath -Path $ProjectPath
if (-not $projectFiles -or $projectFiles.Count -eq 0) {
    Write-Log "No .csproj files found for path '$ProjectPath'" 'ERROR'
    Set-Location -Path $startDir
    exit 4
}

foreach ($proj in $projectFiles) {
    Write-Log "Processing project: $proj"
    $tfs = Get-TargetFrameworksFromCsproj -csproj $proj
    if (-not $tfs -or $tfs.Count -eq 0) {
        Write-Log "  Could not detect TargetFramework(s) for project $proj" 'WARN'
    }

    foreach ($rid in $RIDs) {
        $tfToUse = $Framework
        if (-not $tfToUse) {
            if ($tfs.Count -gt 1) {
                Write-Log "  Project targets multiple frameworks: $($tfs -join ', '). Use -Framework to pick one. Using first: $($tfs[0])" 'WARN'
                $tfToUse = $tfs[0]
            } elseif ($tfs.Count -eq 1) {
                $tfToUse = $tfs[0]
            }
        }

        if (-not $tfToUse) {
            Write-Log "  No target framework available for project $proj; skipping RID $rid" 'ERROR'
            $summary += [PSCustomObject]@{ Project = $proj; RID = $rid; Success = $false; ExitCode = -1; OutputDir = ''; Files = @() }
            continue
        }

        Write-Log "  Publishing for RID: $rid (TFM: $tfToUse)"

    # PowerShell v5.1 doesn't support -LeafBase; use .NET to get filename without extension
    $projName = [IO.Path]::GetFileNameWithoutExtension($proj)
        $outDir = Join-Path $publishRoot "$projName\$rid\$Configuration"
        if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
        New-Item -ItemType Directory -Path $outDir | Out-Null

        $args = @('publish', $proj, '-c', $Configuration, '-r', $rid, '-o', $outDir, '--self-contained', 'true', '/p:PublishSingleFile=true', '/p:PublishReadyToRun=true', '-f', $tfToUse)
        if ($PSBoundParameters.ContainsKey('Verbose')) { $args += ('-v', 'd') }

        Write-Log "  Running: dotnet $($args -join ' ')"
        try {
            & dotnet @args
            $exitCode = $LASTEXITCODE
        } catch {
            Write-Log "  Failed to start dotnet: $($_.Exception.Message)" 'ERROR'
            $exitCode = 1
        }

        if ($exitCode -eq 0) {
            $files = Get-ChildItem -Path $outDir -File | Select-Object -ExpandProperty Name
            $summary += [PSCustomObject]@{ Project = $proj; RID = $rid; Success = $true; ExitCode = $exitCode; OutputDir = $outDir; Files = $files }
            Write-Log "  Publish succeeded for $proj / $rid. Output: $outDir"

            if (-not $NoZip) {
                # Remove PDB files from the publish output before zipping
                try {
                    $pdbs = Get-ChildItem -Path $outDir -Filter '*.pdb' -File -ErrorAction SilentlyContinue
                    if ($pdbs) {
                        foreach ($pdb in $pdbs) {
                            try { Remove-Item -Path $pdb.FullName -Force -ErrorAction Stop } catch { Write-Log "  Warning: failed to remove pdb $($pdb.FullName): $($_.Exception.Message)" 'WARN' }
                        }
                        Write-Log "  Removed PDB files from $outDir"
                    }
                } catch {
                    Write-Log "  Warning: error while trying to remove pdbs: $($_.Exception.Message)" 'WARN'
                }

                # Create a zip named <ProjectName>-<rid>.zip in the publish root (adjacent to project publish folder)
                $zipName = "$projName-$rid.zip"
                $zipPath = Join-Path -Path $artifactsRoot -ChildPath $zipName
                if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

                # Try compressing with retries (handle transient file locks)
                $maxAttempts = 6
                $attempt = 0
                $compressed = $false
                while (-not $compressed -and $attempt -lt $maxAttempts) {
                    try {
                        $attempt++
                        Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipPath -Force
                        $compressed = $true
                    } catch {
                        Write-Log "  Compress attempt $attempt failed: $($_.Exception.Message)" 'WARN'
                        Start-Sleep -Seconds ( [Math]::Min(5, 1 + $attempt) )
                    }
                }

                if (-not $compressed) {
                    # Fallback: copy to a temp folder and compress from there (avoids locks on source files)
                    $tempDir = Join-Path -Path $env:TEMP -ChildPath ("publishzip_{0}_{1}" -f $projName, ([Guid]::NewGuid().ToString().Substring(0,8)))
                    try {
                        New-Item -ItemType Directory -Path $tempDir | Out-Null
                        Copy-Item -Path (Join-Path $outDir '*') -Destination $tempDir -Recurse -Force -ErrorAction Stop
                        Compress-Archive -Path (Join-Path $tempDir '*') -DestinationPath $zipPath -Force
                        $compressed = $true
                        Write-Log "  Compressed output to $zipPath (from temp copy)"
                    } catch {
                        Write-Log "  Failed to compress from temp copy: $($_.Exception.Message)" 'WARN'
                    } finally {
                        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
                    }
                } else {
                    Write-Log "  Compressed output to $zipPath"
                }

                if (-not $compressed) {
                    Write-Log "  ERROR: Unable to create zip $zipPath after retries" 'ERROR'
                }
            }
        } else {
            $summary += [PSCustomObject]@{ Project = $proj; RID = $rid; Success = $false; ExitCode = $exitCode; OutputDir = $outDir; Files = @() }
            Write-Log "  Publish FAILED for $proj / $rid (exit code $exitCode)" 'ERROR'
        }
    }
}

Write-Log "\nPublish summary"
foreach ($s in $summary) {
    $status = if ($s.Success) { 'OK' } else { 'FAILED' }
    Write-Log "RID: $($s.RID) - $status - ExitCode: $($s.ExitCode) - Output: $($s.OutputDir)"
    if ($s.Success -and $s.Files.Count -gt 0) {
        Write-Log "  Files: $($s.Files -join ', ')"
    }
}

Set-Location -Path $startDir

Write-Log "All done. Publish folder: $publishRoot"
