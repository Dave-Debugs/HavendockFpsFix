@echo off
title Havendock FPS Fix - Install
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0files\install.ps1" %*
echo.
pause
