@echo off
set CWD=%~dp0
set BIN="%CWD%\DaS-PC-MPChan\bin\Release"
set IL="C:\il-repack\ILRepack\bin\Release"
set PATH=%IL%;%PATH%

ilrepack /log:merge.log /verbose /out:DSCM.exe %BIN%\DSCM.exe %BIN%\CommonMark.dll
