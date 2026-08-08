@echo off
"%~dp0.dotnet\dotnet.exe" run --project "%~dp0src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj" --configuration Release -- --audio-ducking-test %*
