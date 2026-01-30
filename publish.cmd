@echo off
setlocal

set SOLUTION=SAM.sln
set PLATFORM=x64
set RUNTIME=win-x64
set OUTPUT_DIR=publish
set UPLOAD_DIR=upload

echo ============================================
echo  SAM.X Self-Contained Publish
echo ============================================
echo.

:: Clean previous publish output
if exist "%OUTPUT_DIR%" (
    echo Cleaning previous publish output...
    rmdir /s /q "%OUTPUT_DIR%"
)

:: Build Release (framework-dependent) to upload/ as usual
echo.
echo [1/4] Building Release (framework-dependent) to %UPLOAD_DIR%\...
dotnet build %SOLUTION% -c Release -p:Platform=%PLATFORM%
if errorlevel 1 (
    echo ERROR: Release build failed.
    exit /b 1
)

:: Publish self-contained
echo.
echo [2/4] Publishing self-contained to %OUTPUT_DIR%\...
dotnet publish SAM.Picker\SAM.Picker.csproj -c Release -r %RUNTIME% --self-contained true -p:Platform=%PLATFORM% -p:PublishSingleFile=false -o "%OUTPUT_DIR%"
if errorlevel 1 (
    echo ERROR: SAM.Picker publish failed.
    exit /b 1
)

dotnet publish SAM.Game\SAM.Game.csproj -c Release -r %RUNTIME% --self-contained true -p:Platform=%PLATFORM% -p:PublishSingleFile=false -o "%OUTPUT_DIR%"
if errorlevel 1 (
    echo ERROR: SAM.Game publish failed.
    exit /b 1
)

:: Clean up build artifacts
echo.
echo [3/4] Cleaning up build artifacts...
if exist "%UPLOAD_DIR%\win-x64" rmdir /s /q "%UPLOAD_DIR%\win-x64"
del /q "%UPLOAD_DIR%\*.pdb" 2>nul
del /q "%OUTPUT_DIR%\*.pdb" 2>nul

:: Copy extra files (same as Release build)
echo.
echo [4/4] Copying extra files...
copy /y "SAM.Picker\games.xml" "%OUTPUT_DIR%\games.xml" >nul
copy /y "SAM.Game\LICENSE.txt" "%OUTPUT_DIR%\LICENSE.txt" >nul

echo.
echo ============================================
echo  Done!
echo.
echo  Framework-dependent : %UPLOAD_DIR%\
echo  Self-contained      : %OUTPUT_DIR%\
echo ============================================

endlocal
