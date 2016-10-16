@echo off
rem
rem Get build type from argument list
    if "%1" == "debug"   goto SetBuildType
    if "%1" == "release" goto SetBuildType
    if "%1" == "i4dbg"   goto SetBuildType
    if "%1" == "i4rel"   goto SetBuildType
    if "%1" == ""        goto SetDefaultBuildType
    echo "%0: Unknown build type %1"
    set build_type=release
    echo " Build type set to %build_type%"
    goto BuildTypeDone
:SetBuildType
    set build_type=%1
    goto BuildTypeDone
:SetDefaultBuildType
    set build_type=release
:BuildTypeDone

rem === Diana Environment Setup ===
rem
    set DIAPATH=C:/PROGRA~1/DIANA1~1.0
    set DIAPATH_W=C:\PROGRA~1\DIANA1~1.0
rem
    if "_%DIASHARE%_" == "__" set DIASHARE=%DIAPATH%/share
    set DIABUILD=%DIAPATH%
    set DIASCRIPT=%DIASHARE%/script
    set DIABIN=%DIABUILD%/bin
    set BINSEG=%DIABUILD%/binseg
    set SEGPATH=%BINSEG%
rem
    set PATH=%DIABIN%;%DIASCRIPT%;;%PATH%
rem
    set DIALIB=%DIABUILD%/lib
    set DIASLIB=%DIASHARE%/lib
    set IENV=terminal
    set DIAERRPATH=%DIASHARE%/src
    set FG_PRE_INT=DIANA
rem
    if NOT "_%FGV_CADREPAIR%_" == "__" goto CadRepairDone
    if exist %DIALIB%/CadRepair set FGV_CADREPAIR=%DIALIB%/CadRepair
:CadRepairDone
rem
    if exist %DIASHARE%/Makemac.bat call %DIASHARE%/Makemac.bat
rem
    echo       Welcome to DIANA 10.0
    echo       %build_type% build
    title Diana 10.0 Command Box -  %build_type% build