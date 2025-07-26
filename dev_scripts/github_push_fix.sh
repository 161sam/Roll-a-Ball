#!/bin/bash

echo "🚀 GitHub Large Repository Push Solution"
echo "======================================="

# Option 1: Git LFS für große Dateien aktivieren
echo "📦 Git LFS wird konfiguriert..."
git lfs install

# Unity-spezifische große Dateien für LFS konfigurieren
git lfs track "*.fbx"
git lfs track "*.png" 
git lfs track "*.jpg"
git lfs track "*.tga"
git lfs track "*.tiff"
git lfs track "*.asset"
git lfs track "*.unity"
git lfs track "*.prefab"
git lfs track "*.mat"
git lfs track "*.controller"
git lfs track "*.anim"
git lfs track "*.wav"
git lfs track "*.mp3"
git lfs track "*.ogg"

# .gitattributes committen
git add .gitattributes
git commit -m "Add Git LFS configuration for Unity assets"

# Option 2: Chunked Push (falls LFS nicht verfügbar)
echo "📤 Pushing in kleinen Chunksn..."
git config http.postBuffer 524288000
git config http.lowSpeedLimit 0
git config http.lowSpeedTime 999999

# Option 3: Force push mit reduced pack size
git config pack.packSizeLimit 100m
git config pack.windowMemory 100m

echo "✅ Repository für GitHub-Push optimiert!"
echo "📤 Jetzt 'git push origin main' versuchen"
