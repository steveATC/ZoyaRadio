@echo off
setlocal
cd /d "%~dp0"
set DOTNET_CLI_HOME=%CD%\.dotnet-cli-home
set NUGET_PACKAGES=%CD%\.packages
.\.dotnet\dotnet.exe run --project .\src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj -c Release --no-build -- --m9b-gate3-smoke
endlocal
