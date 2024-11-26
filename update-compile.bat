@echo off

echo Updating...
echo.
call git pull

echo.
echo Updating subdirectories...
call git submodule update --init --recursive

echo.
echo Compiling...
call dotnet build -c Release

echo.
pause Done!

exit