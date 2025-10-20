Build & publish multi-RID single-file, self-contained, ReadyToRun binaries

Usage

PowerShell (from repository root):

    # Basic usage with defaults (win-x64, win-x86, osx-x64, osx-arm64, linux-x64, linux-arm64)
    .\build-publish-multi.ps1

    # Specify project and framework
    .\build-publish-multi.ps1 -ProjectPath './SampleApp' -Framework net9.0

    # Specify custom RIDs
    .\build-publish-multi.ps1 -ProjectPath './SampleApp' -RIDs 'win-x64','osx-arm64','linux-x64' -Framework net9.0

    # Create zip archives for each RID output (default). To disable zipping use -NoZip
    .\build-publish-multi.ps1 -ProjectPath './SampleApp' -Framework net9.0
    .\build-publish-multi.ps1 -ProjectPath './SampleApp' -Framework net9.0 -NoZip

Notes

- The script requires the .NET SDK (dotnet) to be installed and available on PATH.
- RIDs refer to Runtime Identifiers: https://learn.microsoft.com/dotnet/core/rid-catalog
- Output is placed in ./publish/<rid>/<Configuration>
- By default each RID output is also compressed into a centralized artifacts folder at the repository root: ./artifacts/<ProjectName>-<rid>.zip
- The script uses these properties: PublishSingleFile=true, SelfContained=true, PublishReadyToRun=true
