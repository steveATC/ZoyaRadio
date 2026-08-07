@echo off
call "%~dp0run-media-probe.cmd" --control-test
exit /b %errorlevel%
