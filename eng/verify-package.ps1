[CmdletBinding()]
param([string]$PackageDirectory = './artifacts/nuget', [string]$Version)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $PackageDirectory).Path
if ([string]::IsNullOrWhiteSpace($Version)) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $versions = @(foreach ($package in Get-ChildItem -LiteralPath $source -Filter '*.nupkg' -File) {
        $archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            foreach ($entry in $archive.Entries | Where-Object FullName -like '*.nuspec') {
                $reader = [IO.StreamReader]::new($entry.Open())
                try {
                    [xml]$spec = $reader.ReadToEnd()
                    if ($spec.package.metadata.id -eq 'SQ7MRU.Utils') { [string]$spec.package.metadata.version }
                }
                finally { $reader.Dispose() }
            }
        }
        finally { $archive.Dispose() }
    })
    if ($versions.Count -ne 1) { throw 'Expected exactly one SQ7MRU.Utils package; pass -Version to select explicitly.' }
    $Version = $versions[0]
}
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$') { throw 'Invalid package version' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$candidate = Join-Path $source "SQ7MRU.Utils.$Version.nupkg"
$archive = [IO.Compression.ZipFile]::OpenRead($candidate)
try {
    $readme = $archive.GetEntry('README.md')
    if ($null -eq $readme -or $readme.Length -eq 0) { throw 'Package README.md is missing or empty' }
    foreach ($assembly in @('SQ7MRU.Utils', 'SQ7MRU.Utils.Core', 'SQ7MRU.Utils.Application')) {
        $entry = $archive.GetEntry("lib/net10.0/$assembly.xml")
        if ($null -eq $entry) { throw "Missing public API XML documentation: $assembly" }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { [xml]$documentation = $reader.ReadToEnd() }
        finally { $reader.Dispose() }
        if ($documentation.doc.assembly.name -ne $assembly) { throw "Unexpected documentation assembly: $assembly" }
        $members = @($documentation.SelectNodes('/doc/members/member'))
        if ($members.Count -eq 0) { throw "Empty API documentation: $assembly" }
        $documentedTypes = @($members | Where-Object { $_.GetAttribute('name').StartsWith('T:') } |
            ForEach-Object { $_.GetAttribute('name').Substring(2) })
        if ($documentedTypes.Count -eq 0) { throw "No documented API types: $assembly" }
        foreach ($member in $members) {
            $memberName = $member.GetAttribute('name')
            # Regex source generators emit remarks-only entries for private methods
            # on undocumented internal types. They are not part of the public API.
            if (-not $memberName.StartsWith('T:') -and
                @($documentedTypes | Where-Object { $memberName.Substring(2).StartsWith($_ + '.', [StringComparison]::Ordinal) }).Count -eq 0) { continue }
            $summary = $member.SelectSingleNode('summary')
            if ($null -eq $summary -or [string]::IsNullOrWhiteSpace($summary.InnerText)) {
                throw "Missing API summary: $($member.GetAttribute('name'))"
            }
            foreach ($parameter in $member.SelectNodes('param')) {
                if ([string]::IsNullOrWhiteSpace($parameter.InnerText)) { throw "Empty parameter documentation: $($member.GetAttribute('name'))" }
            }
        }
    }
}
finally { $archive.Dispose() }
$work = Join-Path $PSScriptRoot "../artifacts/package-smoke/$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $work -Force | Out-Null
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><PackageReference Include="SQ7MRU.Utils" Version="[$Version]" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $work 'Consumer.csproj')
@'
using Microsoft.Extensions.DependencyInjection;
using SQ7MRU.Utils;
using SQ7MRU.Utils.Infrastructure;
using System.IO.Compression;
using System.Reflection.PortableExecutable;

await using var provider = new ServiceCollection().AddSq7MruUtils().BuildServiceProvider(
    new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
var service = provider.GetRequiredService<IDownloadService>();
// All three shipped library assemblies must remain CPU-neutral IL.
foreach (var assembly in new[] { typeof(IDownloadService).Assembly, typeof(DownloadService).Assembly, typeof(ServiceHttpOptions).Assembly })
{
    using var binary = File.OpenRead(assembly.Location);
    using var pe = new PEReader(binary);
    var flags = pe.PEHeaders.CorHeader?.Flags ?? 0;
    if (pe.PEHeaders.CoffHeader.Machine != Machine.I386 ||
        (flags & CorFlags.ILOnly) == 0 || (flags & CorFlags.Requires32Bit) != 0)
        throw new Exception("The NuGet library must contain AnyCPU assemblies.");
}
_ = provider.GetRequiredService<IEqslAccountService>();
_ = provider.GetRequiredService<IAdifUploader>();
_ = provider.GetRequiredService<IArtifactStore>();
_ = provider.GetRequiredService<IServiceHttpSessionFactory>();
using var reader = new AdifReader("<CALL:6>SQ7MRU<EOR>");
if (reader.GetAdifRows().Count != 1) throw new Exception("Parser failed");
// Prove that the new document API and reviewed embedded catalog work from NuGet.
using var current = new AdifReader(AdifHelper.ExportAsADIF([new() { CALL = "K1ABC", TX_PWR = "0" }]));
if (current.ReadDocument().Records.Count != 1 || AdifHelper.FreqToBand(40) != "8m")
    throw new Exception("Current ADIF profile or embedded metadata failed");
using var legacy = new AdifReader("<ADIF_VER:5>2.2.7<EOH><CALL:5>K1ABC<MODE:5>PSK31<EOR>");
if (legacy.GetAdifRows().Count != 1 || !AdifWriter.Export(legacy.ReadDocument(AdifProfile.EqslLegacy), AdifProfile.EqslLegacy).Contains("2.2.7"))
    throw new Exception("Legacy ADIF compatibility failed");
using var cts = new CancellationTokenSource(); cts.Cancel();
try
{
    await service.DownloadAsync(new(QslService.HrdLog, new("SQ7MRU"), "output"), cancellationToken: cts.Token);
    throw new Exception("Cancellation failed");
}
catch (OperationCanceledException) { }
// Exercise the native SQLite dependency through the public package API.
var store = provider.GetRequiredService<IArtifactStore>();
var output = Path.Combine(Path.GetTempPath(), "sq7mru-package-" + Guid.NewGuid().ToString("N"));
try
{
    using var payload = new MemoryStream();
    using (var archive = new ZipArchive(payload, ZipArchiveMode.Create, leaveOpen: true))
    {
        using var image = archive.CreateEntry("TEST-100.png").Open();
        image.Write(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aHZkAAAAASUVORK5CYII="));
    }
    var item = new DownloadItem("EPC/TEST/Award.jpg", "", AllowImageArchive: true);
    await store.SaveAsync(output, item, payload.ToArray(), default);
    if (!await store.IsValidAsync(output, item, default) || !File.Exists(Path.Combine(output, ".sq7mru", "downloads.sqlite")))
        throw new Exception("SQLite package dependency failed");
}
finally { if (Directory.Exists(output)) Directory.Delete(output, recursive: true); }
Console.WriteLine("NuGet consumer verified.");
'@ | Set-Content (Join-Path $work 'Program.cs')
# A separate package cache and explicit config ensure the local artifact is consumed.
$escapedSource = [System.Security.SecurityElement]::Escape($source)
@"
<configuration><packageSources><clear />
  <add key="local" value="$escapedSource" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources><packageSourceMapping>
  <packageSource key="local"><package pattern="SQ7MRU.Utils" /></packageSource>
  <packageSource key="nuget.org"><package pattern="*" /></packageSource>
</packageSourceMapping></configuration>
"@ | Set-Content (Join-Path $work 'nuget.config')
& dotnet restore (Join-Path $work 'Consumer.csproj') --configfile (Join-Path $work 'nuget.config') --packages (Join-Path $work 'packages')
if ($LASTEXITCODE -ne 0) { throw 'Package consumer restore failed' }
& dotnet run --project (Join-Path $work 'Consumer.csproj') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Package consumer failed' }
