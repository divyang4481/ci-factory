:: "$(ProjectDir)UpdateFoundation.bat" "$(SolutionDir)" "$(TargetDir)"
copy %2%3.dll %1Production\Foundation\Default\Build\dashboard\bin
copy %2%3.pdb %1Production\Foundation\Default\Build\dashboard\bin
