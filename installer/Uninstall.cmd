@echo off
title Havendock FPS Fix - Uninstall
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0files\uninstall.ps1" %*
echo.
pause
