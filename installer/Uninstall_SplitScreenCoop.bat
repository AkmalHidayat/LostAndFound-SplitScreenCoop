@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Lost and Found Co. - Split-Screen Co-op Mod Uninstaller

set "NO_PAUSE=0"
:PARSE_ARGS
if "%~1"=="" goto ARGS_DONE
if /i "%~1"=="--nopause" set "NO_PAUSE=1"
shift
goto PARSE_ARGS
:ARGS_DONE

set "GAME_DIR=%~dp0"
set "PKG_DIR=%GAME_DIR%SplitScreenCoop_Installer"
set "MANIFEST=%PKG_DIR%\install_manifest.txt"
set "PLUGIN_TARGET=%GAME_DIR%BepInEx\plugins\LostAndFound_SplitScreenCoop.dll"

echo ================================================================
echo   LOST AND FOUND CO. - SPLIT-SCREEN CO-OP MOD - UNINSTALLER
echo ================================================================
echo(

set "EXIT_CODE=0"
if not exist "%MANIFEST%" (
    echo [INFO] Install manifest was not found at "%MANIFEST%".
    echo No files were deleted to avoid removing files owned by other mods.
    echo To remove the mod manually, delete only:
    echo   BepInEx\plugins\LostAndFound_SplitScreenCoop.dll
    set "EXIT_CODE=1"
    goto :FINISH
)

set "INSTALLED_BEPINEX=false"
set "LOADER=NONE"
set "LOADER_SHA256=NONE"
set "DOORSTOP_SHA256=NONE"
set "PLUGIN_SHA256="

for /f "usebackq tokens=1,* delims==" %%A in ("%MANIFEST%") do (
    if /i "%%A"=="INSTALLED_BEPINEX" set "INSTALLED_BEPINEX=%%B"
    if /i "%%A"=="LOADER" set "LOADER=%%B"
    if /i "%%A"=="LOADER_SHA256" set "LOADER_SHA256=%%B"
    if /i "%%A"=="DOORSTOP_SHA256" set "DOORSTOP_SHA256=%%B"
    if /i "%%A"=="PLUGIN_SHA256" set "PLUGIN_SHA256=%%B"
)

if not defined PLUGIN_SHA256 (
    echo [ERROR] Install manifest is incomplete or corrupted. No files were deleted.
    set "EXIT_CODE=1"
    goto :FINISH
)

if exist "%PLUGIN_TARGET%" (
    call :HASH_FILE "%PLUGIN_TARGET%" CUR_PLUGIN_HASH
    if /i "!CUR_PLUGIN_HASH!"=="!PLUGIN_SHA256!" (
        del /f /q "%PLUGIN_TARGET%"
        echo [OK] Removed BepInEx\plugins\LostAndFound_SplitScreenCoop.dll
    ) else (
        echo [SKIPPED] LostAndFound_SplitScreenCoop.dll does not match the installed SHA-256 hash.
        echo Preserving it because it appears to have been replaced manually.
    )
) else (
    echo [i] LostAndFound_SplitScreenCoop.dll was already removed.
)

set "CAN_REMOVE_CORE=0"
if /i "!INSTALLED_BEPINEX!"=="true" (
    set "OTHER_PLUGINS=0"
    if exist "%GAME_DIR%BepInEx\plugins" (
        for /f "delims=" %%F in ('dir /b "%GAME_DIR%BepInEx\plugins\*.dll" 2^>nul') do (
            if /i not "%%F"=="LostAndFound_SplitScreenCoop.dll" set "OTHER_PLUGINS=1"
        )
    )

    if "!OTHER_PLUGINS!"=="1" (
        echo [KEPT] Other BepInEx plugins were detected in BepInEx\plugins\.
        echo Keeping BepInEx core and proxy loader ^(!LOADER!^) so your other mods continue working.
    ) else (
        if not "!LOADER!"=="NONE" if exist "%GAME_DIR%!LOADER!" (
            call :HASH_FILE "%GAME_DIR%!LOADER!" CUR_LOADER_HASH
            if /i "!CUR_LOADER_HASH!"=="!LOADER_SHA256!" (
                del /f /q "%GAME_DIR%!LOADER!"
                echo [OK] Removed !LOADER! installed by this package.
                set "CAN_REMOVE_CORE=1"
            ) else (
                echo [SKIPPED] !LOADER! differs from the installed SHA-256 hash.
                echo Keeping !LOADER! and BepInEx\core because another mod appears to use it.
            )
        ) else (
            set "CAN_REMOVE_CORE=1"
        )

        if "!CAN_REMOVE_CORE!"=="1" (
            if not "!DOORSTOP_SHA256!"=="NONE" if exist "%GAME_DIR%doorstop_config.ini" (
                call :HASH_FILE "%GAME_DIR%doorstop_config.ini" CUR_DOORSTOP_HASH
                if /i "!CUR_DOORSTOP_HASH!"=="!DOORSTOP_SHA256!" (
                    del /f /q "%GAME_DIR%doorstop_config.ini"
                    echo [OK] Removed doorstop_config.ini installed by this package.
                ) else (
                    echo [SKIPPED] doorstop_config.ini was modified; kept untouched.
                )
            )
            if exist "%GAME_DIR%BepInEx\core" (
                rmdir /s /q "%GAME_DIR%BepInEx\core"
                echo [OK] Removed BepInEx\core installed by this package.
            )
            rmdir "%GAME_DIR%BepInEx\plugins" 2>nul
            rmdir "%GAME_DIR%BepInEx" 2>nul
        )
    )
) else (
    echo [i] Existing BepInEx installation and proxy loader left untouched.
)

if exist "%MANIFEST%" del /f /q "%MANIFEST%" >nul 2>nul

echo(
echo Uninstall complete. Existing game files and other mods were not disturbed.

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
