@echo off
REM Wrapper to run build-publish-multi.ps1 with ExecutionPolicy Bypass
REM Usage: build-publish-multi.cmd -ProjectPath ".\SampleApp" -Framework net9.0 -Verbose

set SCRIPT_DIR=%~dp0
set PS_SCRIPT="%SCRIPT_DIR%build-publish-multi.ps1"

powershell -NoProfile -ExecutionPolicy Bypass -File %PS_SCRIPT% %*
