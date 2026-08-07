@echo off
call "%~dp0run-media-probe.cmd" --ai-comment-test %*
exit /b %errorlevel%
