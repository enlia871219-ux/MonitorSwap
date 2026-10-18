@echo off
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo C# compiler not found.
    echo Install .NET Framework 4.x Developer Pack or Visual Studio Build Tools.
    pause
    exit /b 1
)

if not exist "%~dp0MonitorSwap.cs" (
    echo MonitorSwap.cs not found.
    pause
    exit /b 1
)

if not exist "%~dp0Icon_Gamgo.ico" (
    echo Icon_Gamgo.ico not found.
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /out:"%~dp0MonitorSwap.exe" /win32icon:"%~dp0Icon_Gamgo.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "%~dp0MonitorSwap.cs"
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

echo Build successful: MonitorSwap.exe
pause
