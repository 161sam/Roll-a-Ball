# 🚨 Roll-a-Ball GitHub Push Problem - Lösungsguide

## Problem-Analyse
- Repository-Größe: 1.04 GiB (zu groß für GitHub Standard-Push)
- HTTP 500 Fehler: GitHub Server überlastet durch große Push-Anfrage
- Uncommitted changes: Viele gelöschte Dateien in Assets/Scipts/
- Git-interne Dateien werden getrackt (.git/index, .git/logs/)

## ⚡ SOFORTLÖSUNG (Empfohlen)

### 1. Repository bereinigen
```bash
# Uncommitted changes committen
git add -A
git commit -m "Clean up: Remove obsolete scripts and organize project structure"

# Git-Cache komplett leeren und neu aufbauen
git rm -r --cached .
git add .gitignore
git add Assets/ ProjectSettings/ Packages/manifest.json Packages/packages-lock.json *.md
git commit -m "Repository cleanup: Remove Library/ and build artifacts"

# Repository komprimieren
git gc --aggressive --prune=now
```

### 2. Alternativer Push-Ansatz
```bash
# Kleinere Chunks pushen
git config http.postBuffer 524288000
git push origin main

# Falls weiterhin Fehler: Shallow Push
git push --depth=1 origin main
```

## 🔧 ALTERNATIVE LÖSUNGEN

### Option A: Git LFS verwenden
```bash
# Git LFS installieren und konfigurieren
git lfs install
git lfs track "*.unity" "*.prefab" "*.asset" "*.mat" "*.png" "*.jpg"
git add .gitattributes
git commit -m "Add Git LFS for Unity assets"
git push origin main
```

### Option B: Neues Repository erstellen
```bash
# Backup erstellen
cp -r /home/saschi/Games/Roll-a-Ball /home/saschi/Games/Roll-a-Ball-backup

# Neues sauberes Repository
cd /home/saschi/Games
git clone https://github.com/161sam/Roll-a-Ball.git Roll-a-Ball-clean
cd Roll-a-Ball-clean

# Nur relevante Dateien vom Backup kopieren
cp -r ../Roll-a-Ball-backup/Assets ./
cp -r ../Roll-a-Ball-backup/ProjectSettings ./
cp ../Roll-a-Ball-backup/Packages/manifest.json ./Packages/
cp ../Roll-a-Ball-backup/*.md ./

# Standard Unity .gitignore verwenden
curl -o .gitignore https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore

# Sauberer Commit
git add .
git commit -m "Initial clean Unity Roll-a-Ball project"
git push origin main
```

### Option C: GitHub Desktop verwenden
- GitHub Desktop kann große Repositories besser handhaben
- Automatische LFS-Erkennung
- Chunked Uploads

## 📊 REPOSITORY-ANALYSE

### Was sollte NICHT im Repository sein:
- ❌ Library/ Ordner (Unity-generiert)
- ❌ Temp/ Ordner (Temporäre Dateien)
- ❌ .git/index, .git/logs/ (Git-interne Dateien)
- ❌ obj/, Builds/ (Kompilierte Dateien)
- ❌ *.tmp, *.bak (Backup-Dateien)

### Was SOLLTE im Repository sein:
- ✅ Assets/ (Game-Dateien)
- ✅ ProjectSettings/ (Unity-Einstellungen)
- ✅ Packages/manifest.json (Package-Abhängigkeiten)
- ✅ README.md, Dokumentation
- ✅ .gitignore

## 🎯 EMPFOHLENER WORKFLOW

1. **Sofort**: Repository mit cleanup_repo.sh bereinigen
2. **Testen**: `git push origin main` versuchen
3. **Falls Fehler**: Git LFS mit github_push_fix.sh aktivieren
4. **Notfall**: Neues sauberes Repository (Option B)

## 🔍 DEBUGGING

Repository-Größe prüfen:
```bash
# Größte Dateien finden
git rev-list --objects --all | git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' | sed -n 's/^blob //p' | sort --numeric-sort --key=2 | tail -10

# Repository-Größe
du -sh .git/
```

Aktuelle Tracking-Status:
```bash
git ls-files | wc -l    # Anzahl getrackte Dateien
git count-objects -v    # Repository-Statistiken
```

## ✅ SUCCESS INDICATORS

- Repository-Größe &lt; 100MB
- Keine Library/ Dateien in `git ls-files`
- Erfolgreicher `git push origin main`
- Keine HTTP 500 Fehler

---

**Status**: Ready to execute cleanup
**Nächster Schritt**: `chmod +x cleanup_repo.sh && ./cleanup_repo.sh`
