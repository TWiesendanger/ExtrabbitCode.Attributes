@echo on
SET ProjectDir=%~1
SET TargetName=%~2
SET TargetPath=%~3
SET TargetDir=%~4

echo debug
echo ProjectDir is %ProjectDir%
echo TargetName is %TargetName%
echo TargetPath is %TargetPath%
echo TargetDir is %TargetDir%
echo "%ProjectDir%Addin\signing\mt.exe"
echo "%ProjectDir%Addin\ExtrabbitCode.Attributes.manifest"

echo - Copy the .addin file and the ButtonResources folder into the result folder.
if not exist "C:\ProgramData\Autodesk\Inventor Addins" mkdir "C:\ProgramData\Autodesk\Inventor Addins"
XCopy "%ProjectDir%Addin\ExtrabbitCode.Attributes.addin" "C:\ProgramData\Autodesk\Inventor Addins" /y

XCopy "%ProjectDir%Resources" "%TargetDir%Resources" /y /r /i /s /f

echo - Delete the existing add-in folder.
echo rmdir /q /s "C:\ProgramData\Autodesk\Inventor Addins\%TargetName%"

REM - Copy the folder to the Inventor Addins folder so Inventor will see it and run it.
XCopy "%TargetDir%*" "C:\ProgramData\ExtrabbitCode\ExtrabbitCode.Attributes" /y /r /i /s /f /g /k