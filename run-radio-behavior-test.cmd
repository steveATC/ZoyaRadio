@echo off
call "%~dp0run-media-probe.cmd" --radio-behavior-test %*
exit /b %errorlevel%
