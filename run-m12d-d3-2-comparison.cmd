@echo off
setlocal
cd /d "%~dp0"
set DOTNET_CLI_HOME=%CD%\.dotnet-cli-home
set NUGET_PACKAGES=%CD%\.packages
"%CD%\.dotnet\dotnet.exe" run --project "%CD%\src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj" -c Release --no-build -- --m12d-d3-2-comparison
endlocal
