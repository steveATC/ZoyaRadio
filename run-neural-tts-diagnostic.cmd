@echo off
call "%~dp0run-media-probe.cmd" --measure-neural-tts
exit /b %errorlevel%
