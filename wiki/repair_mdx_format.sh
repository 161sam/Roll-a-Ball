#!/bin/bash

# repair_mdx_format.sh
# Repariert ungültige MDX-Tags wie <123:25> in allen .md-Dateien des Docusaurus-Doku-Verzeichnisses
# Ersetzt sie durch Inline-Code `123:25`

DOC_DIR="docs"
BACKUP_DIR="docs_backup_$(date +%Y%m%d_%H%M%S)"

echo "🔧 Starte MDX-Reparatur in $DOC_DIR ..."
echo "📦 Erstelle Backup unter $BACKUP_DIR ..."
cp -r "$DOC_DIR" "$BACKUP_DIR"

# Suche alle .md-Dateien und repariere ungültige JSX-ähnliche Zahlen-Tags wie <123:25>
find "$DOC_DIR" -type f -name "*.md" | while read -r file; do
  echo "🛠️  Bearbeite Datei: $file"
  sed -i -E 's/<([0-9][^>]*)>/`\1`/g' "$file"
done

echo "✅ Reparatur abgeschlossen."
echo "📁 Originaldateien im Backup: $BACKUP_DIR"
echo "🚀 Führe jetzt \`npm run build\` erneut aus."
