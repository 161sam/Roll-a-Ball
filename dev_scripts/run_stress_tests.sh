#!/bin/bash

# Scene Consolidation Stress Test Quick Execution Script
# Usage: ./run_stress_tests.sh

echo "🧪 Scene Consolidation Stress Test - Quick Execution"
echo "=================================================="

# Check if Unity is available
if ! command -v Unity &> /dev/null; then
    echo "❌ Unity command not found. Please ensure Unity is in your PATH."
    echo "   You can also run this manually from Unity: Roll-a-Ball → Automation → Execute Complete Stress Test Suite"
    exit 1
fi

# Get project directory
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
echo "📁 Project Directory: $PROJECT_DIR"

# Check if Unity project exists
if [ ! -f "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt" ]; then
    echo "❌ Unity project not found in $PROJECT_DIR"
    exit 1
fi

echo "✅ Unity project found"

# Create reports directory if it doesn't exist
mkdir -p "$PROJECT_DIR/Reports/TestScenes"
echo "✅ Reports directory ready"

echo ""
echo "🚀 Starting Unity Stress Tests..."
echo "   This will take approximately 1-2 minutes..."

# Run Unity stress tests
Unity -batchmode -projectPath "$PROJECT_DIR" -executeMethod AutomatedSceneTestExecutor.CommandLineStressTest -quit

# Check exit code
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Stress tests completed successfully!"
    echo ""
    echo "📄 Generated reports:"
    ls -la "$PROJECT_DIR/Reports/TestScenes/"
    echo ""
    echo "🎯 Next steps:"
    echo "   1. Review the reports in Reports/TestScenes/"
    echo "   2. Check STRESS_TEST_COMPLETE.md for overall results"
    echo "   3. Review individual scene reports for details"
else
    echo ""
    echo "❌ Stress tests failed or were interrupted."
    echo "   Check Unity console output above for details."
    exit 1
fi

echo ""
echo "🎉 Scene Consolidation Stress Test Complete!"
