[CmdletBinding()]
param([string]$PackageDirectory = './artifacts/nuget', [string]$Version = '2.0.0')
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $PackageDirectory).Path
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

await using var provider = new ServiceCollection().AddSq7MruUtils().BuildServiceProvider(
    new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
var service = provider.GetRequiredService<IDownloadService>();
_ = provider.GetRequiredService<IEqslAccountService>();
_ = provider.GetRequiredService<IAdifUploader>();
_ = provider.GetRequiredService<IArtifactStore>();
_ = provider.GetRequiredService<IServiceHttpSessionFactory>();
using var reader = new AdifReader("<CALL:6>SQ7MRU<EOR>");
if (reader.GetAdifRows().Count != 1) throw new Exception("Parser failed");
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
