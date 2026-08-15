@echo off
setlocal
cd /d "%~dp0"
call run-media-probe.cmd --m9c6-whole-comment-benchmark
exit /b %errorlevel%
