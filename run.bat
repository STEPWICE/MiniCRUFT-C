@echo off
setlocal
cd /d %~dp0

if not exist logs mkdir logs
set LOG=logs\run.log

echo === %date% %time% === > %LOG%

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] dotnet not found. Install .NET 10 SDK.>> %LOG%
  echo dotnet not found. Install .NET 10 SDK.
  pause
  exit /b 1
)

for /f %%v in ('dotnet --version') do set DOTNET_VER=%%v
echo DOTNET_VERSION=%DOTNET_VER%>> %LOG%

powershell -NoProfile -Command "$OutputEncoding=[Console]::OutputEncoding=[Text.UTF8Encoding]::new(); dotnet run --project src\MiniCRUFT.Game 2>&1 | Tee-Object -FilePath '%LOG%' -Append"
set CODE=%ERRORLEVEL%
if not "%CODE%"=="0" (
  echo.
  echo Exit code %CODE%. See %LOG%
  pause
)
