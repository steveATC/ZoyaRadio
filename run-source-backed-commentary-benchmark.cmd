@echo off
call "%~dp0run-media-probe.cmd" --source-backed-commentary-benchmark %*
exit /b %errorlevel%
