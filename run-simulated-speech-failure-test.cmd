@echo off
call "%~dp0run-media-probe.cmd" --announcement-test --simulate-speech-failure
exit /b %errorlevel%
