@echo off
call "%~dp0run-media-probe.cmd" --commentary-benchmark %*
exit /b %errorlevel%
