#!/bin/bash

# Unity-Szenen Automatische Reparatur Starter
# Führt systematische Reparatur aller Roll-a-Ball Szenen durch

echo "🚀 Unity-Szenen Automatische Reparatur gestartet"
echo "================================================"

PROJECT_PATH="/home/saschi/Games/Roll-a-Ball"
UNITY_EDITOR="/opt/unity/Editor/Unity"

echo "📁 Projekt-Pfad: $PROJECT_PATH"
echo "🎯 Unity Editor: $UNITY_EDITOR"

# Überprüfung ob Unity-Editor existiert
if [ ! -f "$UNITY_EDITOR" ]; then
    echo "❌ Unity Editor nicht gefunden unter: $UNITY_EDITOR"
    echo "💡 Versuche alternative Pfade..."
    
    # Alternative Unity-Pfade
    ALTERNATIVE_PATHS=(
        "/snap/unity/current/Editor/Unity"
        "/usr/bin/unity-editor"
        "/opt/Unity/Editor/Unity"
        "$(which unity-editor)"
    )
    
    for path in "${ALTERNATIVE_PATHS[@]}"; do
        if [ -f "$path" ]; then
            UNITY_EDITOR="$path"
            echo "✅ Unity Editor gefunden: $UNITY_EDITOR"
            break
        fi
    done
    
    if [ ! -f "$UNITY_EDITOR" ]; then
        echo "❌ Unity Editor konnte nicht gefunden werden!"
        echo "🔧 Bitte Unity Editor installieren oder Pfad anpassen"
        exit 1
    fi
fi

# Überprüfung ob Projekt existiert
if [ ! -d "$PROJECT_PATH" ]; then
    echo "❌ Roll-a-Ball Projekt nicht gefunden: $PROJECT_PATH"
    exit 1
fi

echo "✅ Alle Voraussetzungen erfüllt"
echo ""

# Unity mit automatischer Szenen-Reparatur starten
echo "🎮 Starte Unity Editor mit automatischer Reparatur..."
echo "💡 Das AutoSceneRepair-Tool wird im Unity Editor verfügbar sein"
echo "📋 Gehen Sie zu: Roll-a-Ball → Auto Scene Repair"
echo ""

# Unity starten (im Hintergrund)
cd "$PROJECT_PATH"
nohup "$UNITY_EDITOR" -projectPath "$PROJECT_PATH" > unity_repair.log 2>&1 &

UNITY_PID=$!
echo "🚀 Unity Editor gestartet (PID: $UNITY_PID)"
echo "📄 Log-Datei: $PROJECT_PATH/unity_repair.log"
echo ""

# Warten bis Unity vollständig geladen ist
echo "⏳ Warte auf Unity-Start..."
sleep 10

# Anweisungen für den Benutzer
echo "🎯 NÄCHSTE SCHRITTE:"
echo "1. Unity Editor sollte jetzt geöffnet sein"
echo "2. Gehen Sie zu: Roll-a-Ball → Auto Scene Repair"
echo "3. Klicken Sie auf '🚀 ALLE SZENEN REPARIEREN'"
echo "4. Warten Sie, bis alle Reparaturen abgeschlossen sind"
echo ""

echo "📊 REPARATUR-UMFANG:"
echo "   ✅ GeneratedLevel.unity - Prefab-Refs, UI, LevelGenerator"
echo "   ✅ Level1.unity - Prefab-Konvertierung, Tutorial-Setup"
echo "   ✅ Level2.unity - Schwierigkeit, Steampunk-Intro"
echo "   ✅ Level3.unity - Master-Level-Konfiguration"
echo "   ✅ Level_OSM.unity - OSM-Integration-Setup"
echo "   ✅ MiniGame.unity - Basis-Setup"
echo ""

echo "🔍 FORTSCHRITT ÜBERWACHEN:"
echo "   - Unity Console für Script-Output"
echo "   - AutoSceneRepair-Fenster für detailliertes Log"
echo "   - unity_repair.log für technische Details"
echo ""

# Optional: Unity-Prozess überwachen
echo "🛡️ Unity-Prozess läuft (PID: $UNITY_PID)"
echo "💾 Log-Updates:"
tail -f unity_repair.log &
TAIL_PID=$!

# Cleanup-Funktion für Strg+C
cleanup() {
    echo ""
    echo "🛑 Reparatur-Prozess wird beendet..."
    kill $TAIL_PID 2>/dev/null
    echo "✅ Cleanup abgeschlossen"
    exit 0
}

trap cleanup INT

echo "🎯 Drücken Sie Strg+C um dieses Script zu beenden"
echo "💡 Unity Editor läuft weiter im Hintergrund"

# Warten auf Benutzer-Eingabe
wait $TAIL_PID
