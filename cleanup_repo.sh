#!/bin/bash

echo "🚀 Roll-a-Ball Repository Cleanup Script"
echo "========================================"

# Schritt 1: Aktuelle Änderungen stashen
echo "📦 Uncommitted changes werden gestasht..."
git add -A
git stash push -m "Cleanup: Uncommitted changes before repository cleanup"

# Schritt 2: Git-Cache leeren
echo "🧹 Git-Cache wird geleert..."
git rm -r --cached .

# Schritt 3: .gitignore neu laden
echo "📋 .gitignore wird neu geladen..."
git add .gitignore

# Schritt 4: Nur relevante Unity-Dateien hinzufügen
echo "📁 Nur relevante Unity-Dateien werden hinzugefügt..."
git add Assets/
git add ProjectSettings/
git add Packages/manifest.json
git add Packages/packages-lock.json
git add *.md
git add *.txt

# Schritt 5: Commit der Bereinigung
echo "💾 Repository-Bereinigung committen..."
git commit -m "Repository cleanup: Remove Unity Library and .git internals

- Removed Library/ folder from tracking
- Removed .git internal files from tracking  
- Cleaned up build artifacts
- Applied proper Unity .gitignore
- Project size reduced significantly"

# Schritt 6: Repository komprimieren
echo "🗜️ Repository wird komprimiert..."
git gc --aggressive --prune=now

# Schritt 7: Repository-Größe anzeigen
echo "📊 Neue Repository-Größe:"
du -sh .git/

echo "✅ Repository-Bereinigung abgeschlossen!"
echo "📤 Jetzt 'git push origin main' ausführen"
