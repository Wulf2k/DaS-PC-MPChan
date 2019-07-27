@echo off
set CWD=%~dp0
set VS=%PROGRAMFILES(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\
set UTIL=%CWD%\util
set PATH=%VS%;%UTIL%;%PATH%;

set FLAGS=-p:Configuration=Release -p:OutputPath="%CWD%\bin"

nuget restore
nuget restore il-repack

msbuild il-repack\ILRepack\ILRepack.csproj %FLAGS%
msbuild DSCM.sln -m -nr:false %FLAGS%

%CWD%\bin\ILRepack.exe /log:merge.log /verbose /out:%CWD%\dist\DSCM.exe %CWD%\bin\DSCM.exe %CWD%\bin\CommonMark.dll
