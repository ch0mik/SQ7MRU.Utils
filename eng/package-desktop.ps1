param(
    [Parameter(Mandatory)][ValidateSet('win-x86', 'win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'linux-arm', 'osx-x64', 'osx-arm64')][string]$Rid,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')][string]$Version,
    [string]$PublishDirectory = 'artifacts/publish',
    [string]$OutputDirectory = 'artifacts/packages'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Native([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}

$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
$executable = if ($Rid.StartsWith('win-')) { 'SQ7MRU.Downloader.exe' } else { 'SQ7MRU.Downloader' }
$files = @(Get-ChildItem -LiteralPath $publish -Recurse -File -Force)
if ($files.Count -ne 1 -or $files[0].Name -ne $executable -or $files[0].DirectoryName -ne $publish) {
    throw 'Publication must contain exactly one executable at its root.'
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$name = "SQ7MRU.Downloader-$Version-$Rid"
$source = Join-Path $publish $executable
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash

if ($Rid.StartsWith('win-')) {
    if (-not $IsWindows) { throw 'Windows packaging requires a Windows runner.' }
    $package = Join-Path $output "$name.zip"
    Compress-Archive -LiteralPath $source -DestinationPath $package -CompressionLevel Optimal -Force
    $archive = [IO.Compression.ZipFile]::OpenRead($package)
    try {
        if ($archive.Entries.Count -ne 1 -or $archive.Entries[0].FullName -ne $executable) {
            throw 'ZIP must contain exactly the expected executable.'
        }
        $stream = $archive.Entries[0].Open()
        try { $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
        finally { $stream.Dispose() }
        if ($digest -ne $sourceHash) { throw 'ZIP executable differs from publication.' }
    } finally { $archive.Dispose() }
} elseif ($Rid.StartsWith('linux-')) {
    if (-not $IsLinux) { throw 'Linux packaging requires a Linux runner.' }
    Invoke-Native chmod @('755', $source)
    $package = Join-Path $output "$name.tar.gz"
    Invoke-Native tar @('-czf', $package, '-C', $publish, $executable)
    $members = @(& tar -tzf $package)
    if ($LASTEXITCODE -ne 0 -or $members.Count -ne 1 -or $members[0] -ne $executable) {
        throw 'TAR.GZ must contain exactly the expected executable.'
    }
    # Verify bytes and executable permissions after a real extraction.
    $verification = Join-Path $output "verify-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $verification | Out-Null
    Invoke-Native tar @('-xzf', $package, '-C', $verification)
    $extracted = Join-Path $verification $executable
    Invoke-Native test @('-x', $extracted)
    if ((Get-FileHash -LiteralPath $extracted -Algorithm SHA256).Hash -ne $sourceHash) {
        throw 'TAR.GZ executable differs from publication.'
    }
    # Only remove the known verification file and its now-empty directory.
    Remove-Item -LiteralPath $extracted
    Remove-Item -LiteralPath $verification
} else {
    if (-not $IsMacOS) { throw 'DMG packaging requires a macOS runner.' }
    $staging = Join-Path $output "dmg-$([Guid]::NewGuid().ToString('N'))"
    $contents = Join-Path $staging 'SQ7MRU.Downloader.app/Contents'
    $macos = Join-Path $contents 'MacOS'
    $resources = Join-Path $contents 'Resources'
    New-Item -ItemType Directory -Path $macos, $resources | Out-Null
    Copy-Item -LiteralPath $source -Destination (Join-Path $macos $executable)
    Invoke-Native chmod @('755', (Join-Path $macos $executable))
    $bundleVersion = $Version.Split('-')[0]
    @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleIdentifier</key><string>org.sq7mru.downloader</string>
  <key>CFBundleName</key><string>SQ7MRU Downloader</string>
  <key>CFBundleDisplayName</key><string>SQ7MRU Downloader</string>
  <key>CFBundleExecutable</key><string>SQ7MRU.Downloader</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$bundleVersion</string>
  <key>CFBundleVersion</key><string>$bundleVersion</string>
  <key>CFBundleIconFile</key><string>AppIcon.icns</string>
  <key>NSHighResolutionCapable</key><true/>
</dict></plist>
"@ | Set-Content -LiteralPath (Join-Path $contents 'Info.plist') -Encoding utf8NoBOM
    Invoke-Native plutil @('-lint', (Join-Path $contents 'Info.plist'))
    $icon = Join-Path $staging 'icon.png'
    Invoke-Native swift @((Join-Path $PSScriptRoot 'render-macos-icon.swift'), $icon)
    $iconset = Join-Path $staging 'AppIcon.iconset'
    New-Item -ItemType Directory -Path $iconset | Out-Null
    foreach ($size in @(16, 32, 128, 256, 512)) {
        Invoke-Native sips @('-z', "$size", "$size", $icon, '--out', (Join-Path $iconset "icon_${size}x${size}.png"))
        $double = $size * 2
        Invoke-Native sips @('-z', "$double", "$double", $icon, '--out', (Join-Path $iconset "icon_${size}x${size}@2x.png"))
    }
    Invoke-Native iconutil @('-c', 'icns', $iconset, '-o', (Join-Path $resources 'AppIcon.icns'))
    # Keep intermediate icon sources outside the disk image's distribution root.
    $distribution = Join-Path $staging 'distribution'
    New-Item -ItemType Directory -Path $distribution | Out-Null
    Move-Item -LiteralPath (Join-Path $staging 'SQ7MRU.Downloader.app') -Destination $distribution
    Invoke-Native ln @('-s', '/Applications', (Join-Path $distribution 'Applications'))
    $package = Join-Path $output "$name.dmg"
    Invoke-Native hdiutil @('create', '-ov', '-format', 'UDZO', '-volname', 'SQ7MRU Downloader', '-srcfolder', $distribution, $package)
    Invoke-Native hdiutil @('verify', $package)
    $mount = Join-Path $staging 'mounted'
    New-Item -ItemType Directory -Path $mount | Out-Null
    Invoke-Native hdiutil @('attach', '-readonly', '-nobrowse', '-mountpoint', $mount, $package)
    try {
        $app = Join-Path $mount 'SQ7MRU.Downloader.app/Contents'
        $mountedBinary = Join-Path $app 'MacOS/SQ7MRU.Downloader'
        Invoke-Native test @('-x', $mountedBinary)
        Invoke-Native plutil @('-lint', (Join-Path $app 'Info.plist'))
        if ((Get-FileHash -LiteralPath $mountedBinary -Algorithm SHA256).Hash -ne $sourceHash) {
            throw 'DMG executable differs from publication.'
        }
        if (-not (Test-Path -LiteralPath (Join-Path $app 'Resources/AppIcon.icns'))) { throw 'Missing app icon.' }
        $link = & readlink (Join-Path $mount 'Applications')
        if ($LASTEXITCODE -ne 0 -or $link -ne '/Applications') { throw 'Missing Applications shortcut.' }
    } finally { Invoke-Native hdiutil @('detach', $mount) }
}

# Hash the completed distribution container, never the uncompressed executable.
$hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($package))" | Set-Content -LiteralPath "$package.sha256" -Encoding ascii
Write-Output "Verified desktop package: $package"
