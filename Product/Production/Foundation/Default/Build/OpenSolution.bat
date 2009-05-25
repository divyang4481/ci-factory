FOR /F "delims=" %%v IN ('cd') DO set BuildDirectory=%%v
set PATH=%PATH%;%BuildDirectory%\nAnt\bin

set ENVBAT="%ProgramFiles%\Microsoft Visual Studio 9.0\VC\vcvarsall.bat"
IF DEFINED ProgramFiles(x86) set ENVBAT="%ProgramFiles(x86)%\Microsoft Visual Studio 9.0\VC\vcvarsall.bat"
call %ENVBAT%

devenv "%ProjectName%BuildScripts.sln"
