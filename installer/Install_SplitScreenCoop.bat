@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Lost and Found Co. - Split-Screen Co-op Mod Installer

set "NO_PAUSE=0"
set "FORCE_DIR=0"
:PARSE_ARGS
if "%~1"=="" goto ARGS_DONE
if /i "%~1"=="--nopause" set "NO_PAUSE=1"
if /i "%~1"=="--force" set "FORCE_DIR=1"
shift
goto PARSE_ARGS
:ARGS_DONE

set "GAME_DIR=%~dp0"
set "PKG_FOLDER=SplitScreenCoop_Installer"
set "PKG_DIR=%GAME_DIR%%PKG_FOLDER%"
set "PKG_DLL=%PKG_DIR%\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll"
set "PKG_CORE=%PKG_DIR%\BepInEx\core"
set "LOADER_DIR=%PKG_DIR%\loaders"
set "MANIFEST=%PKG_DIR%\install_manifest.txt"
set "CANDIDATES=winhttp.dll"

echo ================================================================
echo   LOST AND FOUND CO. - SPLIT-SCREEN CO-OP MOD - INSTALLER
echo ================================================================
echo(

if not exist "%PKG_DLL%" (
    echo [ERROR] "%PKG_FOLDER%\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll" was not found.
    echo Extract the entire ZIP archive directly into your Lost and Found Co. game folder,
    echo right beside "Lost and Found Co.exe".
    set "EXIT_CODE=1"
    goto :FINISH
)

if not exist "%GAME_DIR%Lost and Found Co.exe" (
    echo [WARNING] "Lost and Found Co.exe" was not found in this folder:
    echo   %GAME_DIR%
    if "%FORCE_DIR%"=="1" (
        echo [i] --force specified; continuing without executable check.
    ) else if "%NO_PAUSE%"=="1" (
        echo [ERROR] Aborting non-interactive install because "Lost and Found Co.exe" is missing.
        set "EXIT_CODE=1"
        goto :FINISH
    ) else (
        choice /m "Continue installing into this folder anyway"
        if errorlevel 2 (
            set "EXIT_CODE=1"
            goto :FINISH
        )
    )
)

set "PERM_TEST=%GAME_DIR%.splitcoop_write_test_%RANDOM%.tmp"
type nul > "%PERM_TEST%" 2>nul
if not exist "%PERM_TEST%" (
    echo [ERROR] Cannot write to "%GAME_DIR%".
    echo Move the game folder out of a protected directory or run this installer as Administrator.
    set "EXIT_CODE=1"
    goto :FINISH
)
del /f /q "%PERM_TEST%" >nul 2>nul

if exist "%MANIFEST%" (
    echo [INFO] Split-Screen Co-op Mod is already installed ^(manifest found^).
    echo Run "Uninstall_SplitScreenCoop.bat" first if you want to reinstall or upgrade.
    set "EXIT_CODE=0"
    goto :FINISH
)

echo [1/4] Checking existing BepInEx and mod files ^(no existing mods will be overwritten^)...

set "HAS_BEPINEX=0"
if exist "%GAME_DIR%BepInEx\core\BepInEx.Preloader.dll" (
    if exist "%GAME_DIR%doorstop_config.ini" (
        for %%P in (winhttp.dll version.dll winmm.dll dinput8.dll) do (
            if exist "%GAME_DIR%%%P" set "HAS_BEPINEX=1"
        )
    )
)

set "INSTALLED_BEPINEX=false"
set "SELECTED_LOADER=NONE"
set "LOADER_HASH=NONE"
set "DOORSTOP_HASH=NONE"

if "%HAS_BEPINEX%"=="1" (
    echo  - Existing BepInEx 5 installation detected ^(safe coexistence mode^).
    echo  - Proxy loader, doorstop_config.ini, and other plugins will not be modified.
    echo(
    echo [2/4] Skipping BepInEx setup ^(already present^).
    goto :COPY_PLUGIN
)

if not exist "%PKG_CORE%\BepInEx.Preloader.dll" (
    echo [ERROR] BepInEx runtime files are missing from "%PKG_FOLDER%\BepInEx\core".
    set "EXIT_CODE=1"
    goto :FINISH
)

set "SELECTED_LOADER="
for %%P in (%CANDIDATES%) do (
    if not defined SELECTED_LOADER (
        if exist "%LOADER_DIR%\%%P" (
            if exist "%GAME_DIR%%%P" (
                echo  - %%P is already used by another tool/mod; skipping.
            ) else (
                set "SELECTED_LOADER=%%P"
            )
        )
    )
)

if not defined SELECTED_LOADER (
    echo(
    echo [STOP] Proxy slot ^(winhttp.dll^) is already occupied by another tool or mod.
    echo No files were changed. If you already use BepInEx 5, install manually by copying
    echo "%PKG_FOLDER%\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll" into "BepInEx\plugins\".
    set "EXIT_CODE=2"
    goto :FINISH
)

echo(
echo [2/4] Installing BepInEx 5 runtime and Doorstop proxy ^(!SELECTED_LOADER!^)...
copy /y "%LOADER_DIR%\!SELECTED_LOADER!" "%GAME_DIR%!SELECTED_LOADER!" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy proxy loader !SELECTED_LOADER!.
    set "EXIT_CODE=3"
    goto :FINISH
)

call :HASH_FILE "%GAME_DIR%!SELECTED_LOADER!" LOADER_HASH
set "INSTALLED_BEPINEX=true"

if not exist "%GAME_DIR%doorstop_config.ini" (
    if exist "%PKG_DIR%\doorstop_config.ini" (
        copy /y "%PKG_DIR%\doorstop_config.ini" "%GAME_DIR%doorstop_config.ini" >nul
        if not errorlevel 1 (
            call :HASH_FILE "%GAME_DIR%doorstop_config.ini" DOORSTOP_HASH
        )
    )
) else (
    echo  - Existing doorstop_config.ini detected; kept untouched.
)

if not exist "%GAME_DIR%BepInEx\core" mkdir "%GAME_DIR%BepInEx\core"
xcopy /e /i /y "%PKG_CORE%" "%GAME_DIR%BepInEx\core" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy BepInEx core runtime files.
    set "EXIT_CODE=3"
    goto :FINISH
)

:COPY_PLUGIN
echo [3/4] Installing LostAndFound_SplitScreenCoop.dll...
if not exist "%GAME_DIR%BepInEx\plugins" mkdir "%GAME_DIR%BepInEx\plugins"
copy /y "%PKG_DLL%" "%GAME_DIR%BepInEx\plugins\LostAndFound_SplitScreenCoop.dll" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy LostAndFound_SplitScreenCoop.dll to BepInEx\plugins\.
    set "EXIT_CODE=3"
    goto :FINISH
)

call :HASH_FILE "%GAME_DIR%BepInEx\plugins\LostAndFound_SplitScreenCoop.dll" PLUGIN_HASH

> "%MANIFEST%" echo INSTALLED_BEPINEX=!INSTALLED_BEPINEX!
>> "%MANIFEST%" echo LOADER=!SELECTED_LOADER!
>> "%MANIFEST%" echo LOADER_SHA256=!LOADER_HASH!
>> "%MANIFEST%" echo DOORSTOP_SHA256=!DOORSTOP_HASH!
>> "%MANIFEST%" echo PLUGIN_SHA256=!PLUGIN_HASH!

echo [4/4] Done.
echo(
echo ================================================================
echo   Split-Screen Co-op Mod installed successfully!
echo   Plugin      : BepInEx\plugins\LostAndFound_SplitScreenCoop.dll
if /i "!INSTALLED_BEPINEX!"=="true" (
    echo   Proxy Loader: !SELECTED_LOADER! ^(newly installed with BepInEx 5^)
) else (
    echo   BepInEx     : Reused existing installation ^(other mods preserved^)
)
echo(
echo   In-game Hotkeys:
echo     F8  - Toggle Split-Screen Co-op
echo     F11 - Toggle 2-Player Layout ^(Left/Right or Top/Bottom^)
echo     F7  - Switch P1 between Mouse+Keyboard and Gamepad 1
echo     F10 - Open/Close Co-op Control Panel
echo     F6  - Toggle P2 Keyboard Test Mode ^(IJKL + Arrows + Enter^)
echo(
echo   Launch "Lost and Found Co." through Steam as usual.
echo ================================================================
set "EXIT_CODE=0"

:FINISH
if "%NO_PAUSE%"=="0" pause
exit /b %EXIT_CODE%

:HASH_FILE
set "_HF=%~1"
set "_HTMP="
if not exist "!_HF!" (
    set "%~2=NONE"
    exit /b 1
)
for /f "skip=1 delims=" %%X in ('certutil -hashfile "!_HF!" SHA256 ^| findstr /v /c:"CertUtil"') do (
    if not defined _HTMP set "_HTMP=%%X"
)
if defined _HTMP set "_HTMP=!_HTMP: =!"
set "%~2=!_HTMP!"
exit /b 0
