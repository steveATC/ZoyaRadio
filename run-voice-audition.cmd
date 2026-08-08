@echo off
setlocal
cd /d "%~dp0"

set "DOTNET_CLI_HOME=%~dp0.dotnet-cli-home"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "NUGET_PACKAGES=%~dp0.packages"

set "DOTNET_EXE=%~dp0.dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" set "DOTNET_EXE=dotnet"

"%DOTNET_EXE%" run --configuration Release --project "%~dp0src\ZoyaRadio.VoiceAudition\ZoyaRadio.VoiceAudition.csproj" --no-restore
exit /b %errorlevel%
