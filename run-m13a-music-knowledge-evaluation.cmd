@echo off
setlocal
cd /d "%~dp0"
set DOTNET_CLI_HOME=%CD%\.dotnet-cli-home
set NUGET_PACKAGES=%CD%\.packages
"%CD%\.dotnet\dotnet.exe" run --project "%CD%\tests\ZoyaRadio.MediaProbe.Tests\ZoyaRadio.MediaProbe.Tests.csproj" -c Release --no-build
endlocal
