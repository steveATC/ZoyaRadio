@echo off
setlocal
cd /d "%~dp0"
call run-media-probe.cmd --deterministic-context-benchmark
exit /b %errorlevel%