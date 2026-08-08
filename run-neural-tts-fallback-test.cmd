@echo off
call "%~dp0run-spoken-announcement-test.cmd" --simulate-neural-tts-failure
exit /b %errorlevel%
