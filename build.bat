@echo off
echo Building project...
dotnet build "SIASGraduate.csproj"
if %ERRORLEVEL% NEQ 0 (
  echo Build failed with error level %ERRORLEVEL%
  pause
  exit /b %ERRORLEVEL%
)
echo Build completed successfully.
pause 