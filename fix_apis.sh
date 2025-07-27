#!/bin/bash
# Comprehensive API Fix Script

echo "🔧 Fixing obsolete FindObject APIs..."

# Define the base path
BASE_PATH="/home/saschi/Games/Roll-a-Ball/Assets/Scripts"

# Find all C# files
find "$BASE_PATH" -name "*.cs" -type f | while read -r file; do
    echo "Processing: $file"
    
    # Create backup
    cp "$file" "$file.backup"
    
    # Apply API fixes
    sed -i 's/FindObjectOfType</Object.FindFirstObjectByType</g' "$file"
    sed -i 's/Object\.FindObjectOfType</Object.FindFirstObjectByType</g' "$file"
    sed -i 's/FindObjectsOfType</Object.FindObjectsByType</g' "$file"
    sed -i 's/Object\.FindObjectsOfType</Object.FindObjectsByType</g' "$file"
    
    # Fix FindObjectsByType calls to include sorting parameter
    sed -i 's/Object\.FindObjectsByType<\([^>]*\)>()/Object.FindObjectsByType<\1>(FindObjectsSortMode.None)/g' "$file"
    
    # Fix other null checks that might use ! operator incorrectly
    sed -i 's/if (!SaveSystem\.Instance?.CurrentSave)/if (SaveSystem.Instance?.CurrentSave == null)/g' "$file"
    
    echo "✅ Fixed: $file"
done

echo "🎯 All obsolete APIs have been updated!"
