@echo off
echo Building project...
dotnet build "2025毕业设计.csproj"
if %ERRORLEVEL% NEQ 0 (
  echo Build failed with error level %ERRORLEVEL%
  pause
  exit /b %ERRORLEVEL%
)
echo Build completed successfully.
pause 