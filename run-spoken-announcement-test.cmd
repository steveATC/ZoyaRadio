@echo off
call "%~dp0run-media-probe.cmd" --announcement-test %*
exit /b %errorlevel%
