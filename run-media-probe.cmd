@echo off
setlocal
cd /d "%~dp0"

set "DOTNET_CLI_HOME=%~dp0.dotnet-cli-home"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "NUGET_PACKAGES=%~dp0.packages"

set "APP_EXE=%~dp0src\ZoyaRadio.MediaProbe\bin\Release\net8.0-windows10.0.19041.0\ZoyaRadio.MediaProbe.exe"
if exist "%APP_EXE%" goto run_built_probe

set "DOTNET_EXE=%~dp0.dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" set "DOTNET_EXE=dotnet"

"%DOTNET_EXE%" run --configuration Release --project "%~dp0src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj" -- %*
exit /b %errorlevel%

:run_built_probe
"%APP_EXE%" %*
exit /b %errorlevel%
