@echo off
REM Scene Consolidation Stress Test Quick Execution Script (Windows)
REM Usage: run_stress_tests.bat

echo 🧪 Scene Consolidation Stress Test - Quick Execution
echo ==================================================

REM Get project directory
set PROJECT_DIR=%~dp0..
echo 📁 Project Directory: %PROJECT_DIR%

REM Check if Unity project exists
if not exist "%PROJECT_DIR%\ProjectSettings\ProjectVersion.txt" (
    echo ❌ Unity project not found in %PROJECT_DIR%
    pause
    exit /b 1
)

echo ✅ Unity project found

REM Create reports directory if it doesn't exist
if not exist "%PROJECT_DIR%\Reports\TestScenes" mkdir "%PROJECT_DIR%\Reports\TestScenes"
echo ✅ Reports directory ready

echo.
echo 🚀 Starting Unity Stress Tests...
echo    This will take approximately 1-2 minutes...

REM Run Unity stress tests
Unity.exe -batchmode -projectPath "%PROJECT_DIR%" -executeMethod AutomatedSceneTestExecutor.CommandLineStressTest -quit

REM Check exit code
if %errorlevel% equ 0 (
    echo.
    echo ✅ Stress tests completed successfully!
    echo.
    echo 📄 Generated reports:
    dir "%PROJECT_DIR%\Reports\TestScenes\"
    echo.
    echo 🎯 Next steps:
    echo    1. Review the reports in Reports\TestScenes\
    echo    2. Check STRESS_TEST_COMPLETE.md for overall results
    echo    3. Review individual scene reports for details
) else (
    echo.
    echo ❌ Stress tests failed or were interrupted.
    echo    Check Unity console output above for details.
    pause
    exit /b 1
)

echo.
echo 🎉 Scene Consolidation Stress Test Complete!
pause
