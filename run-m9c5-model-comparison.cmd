@echo off
setlocal
cd /d "%~dp0"
call run-media-probe.cmd --m9c5-model-comparison
exit /b %errorlevel%
