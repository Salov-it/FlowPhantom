@echo off
echo ========================================
echo  Installing FlowPhantom Windows Client
echo ========================================

REM Create install directory
set INSTALL_DIR="C:\Program Files\FlowPhantom"
echo Creating directory %INSTALL_DIR%...
mkdir %INSTALL_DIR% >nul 2>&1

echo Copying client files...
copy /Y FlowPhantom.* %INSTALL_DIR% >nul
copy /Y wintun.dll %INSTALL_DIR% >nul

REM Create service (OPTIONAL)
echo Creating Windows service FlowPhantomClient...

sc create FlowPhantomClient binPath= "%INSTALL_DIR%\FlowPhantom.Client.exe" start= auto
sc description FlowPhantomClient "FlowPhantom VPN Client Service"

echo Starting service...
sc start FlowPhantomClient

echo ========================================
echo FlowPhantom Client installed!
echo Run: services.msc → FlowPhantomClient
echo ========================================
pause
