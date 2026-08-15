@echo off
setlocal
cd /d "%~dp0"
call run-media-probe.cmd --owner-curated-commentary-benchmark
exit /b %errorlevel%