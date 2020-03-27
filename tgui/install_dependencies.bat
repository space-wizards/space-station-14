@echo off
echo node.js 5.3.0 or newer must be installed for this script to work.
echo If this script fails, try closing editors and running it again first.
echo Any warnings about optional dependencies can be safely ignored.
pause
REM Install dependencies
npm ci
pause
