# 🎯 Procedural Level Generation System - Setup Guide

## 📋 Übersicht

Das prozedurale Levelgenerierungssystem für Roll-a-Ball ersetzt die manuell erstellten Level durch ein konfigurierbares, automatisches Generierungssystem. Die Architektur ist modular und erweiterbar für zukünftige Features wie Daily Challenges oder OSM-Integration.

---

## 🗂️ Erstellte Dateien

### Core System
- `Assets/Scipts/Generators/LevelProfile.cs` - ScriptableObject für Levelkonfiguration
- `Assets/Scipts/Generators/LevelGenerator.cs` - Hauptgenerierungssystem 
- `Assets/Scipts/Generators/LevelProfileCreator.cs` - Utility zum Erstellen der Profile

### UI Integration
- `Assets/Scipts/UIController.cs` - Erweitert um Leveltyp-Anzeige

### Ordnerstruktur
- `Assets/Scipts/Generators/` - Generierungssystem-Scripte
- `Assets/ScriptableObjects/` - LevelProfile-Assets (wird erstellt)

---

## 🚀 Setup-Schritte

### Schritt 1: Unity-Projekt vorbereiten

1. **Tags hinzufügen** (falls noch nicht vorhanden):
   - Öffne Edit → Project Settings → Tags and Layers
   - Füge folgende Tags hinzu:
     - `Collectible`
     - `Finish` (für GoalZone)

2. **Prefabs überprüfen**:
   - Stelle sicher, dass alle Prefabs im Ordner `Assets/Prefabs/` vorhanden sind:
     - `GroundPrefab.prefab`
     - `WallPrefab.prefab` 
     - `CollectiblePrefab.prefab`
     - `GoalZonePrefab.prefab`
     - `Player.prefab`

### Schritt 2: LevelProfile-Assets erstellen

1. **LevelProfileCreator ausführen**:
   ```csharp
   // Option A: Über Scene GameObject
   GameObject go = new GameObject("LevelProfileCreator");
   LevelProfileCreator creator = go.AddComponent<LevelProfileCreator>();
   creator.CreateAllProfiles(); // Nur im Editor
   ```

2. **Oder manuell über Context Menu**:
   - Erstelle ein leeres GameObject in der Szene
   - Füge `LevelProfileCreator` Component hinzu
   - Rechtsklick auf Component → "Create All Level Profiles"

3. **Verifikation**:
   - Überprüfe, dass in `Assets/ScriptableObjects/` folgende Assets erstellt wurden:
     - `EasyProfile.asset` (Grün, 8x8, 5 Collectibles)
     - `MediumProfile.asset` (Gelb, 12x12, 8 Collectibles, Maze)
     - `HardProfile.asset` (Rot, 16x16, 12 Collectibles, Maze + Rutschflächen)

### Schritt 3: GeneratedLevel-Szene erstellen

1. **Neue Szene erstellen**:
   - File → New Scene
   - Speichere als `GeneratedLevel.unity` in `Assets/Scenes/`

2. **Basic Setup**:
   ```
   GeneratedLevel
   ├── Directional Light
   ├── Main Camera
   ├── EventSystem
   ├── Canvas (UI)
   │   ├── LevelTypeText (TextMeshPro)
   │   ├── CollectibleText (TextMeshPro)
   │   └── FlyEnergyBar (Slider)
   ├── LevelGenerator (Empty GameObject)
   ├── GameManager
   ├── LevelManager
   └── UIController
   ```

3. **LevelGenerator konfigurieren**:
   - Erstelle leeres GameObject "LevelGenerator"
   - Füge `LevelGenerator` Component hinzu
   - Setze folgende Referenzen:
     - Active Profile: `EasyProfile` (zum Testen)
     - Ground Prefab: `GroundPrefab`
     - Wall Prefab: `WallPrefab`
     - Collectible Prefab: `CollectiblePrefab`
     - Goal Zone Prefab: `GoalZonePrefab`
     - Player Prefab: `Player`

### Schritt 4: UI Setup

1. **Canvas UI-Elemente erstellen**:
   ```
   Canvas
   ├── LevelProfilePanel
   │   └── LevelTypeText (TextMeshPro)
   ├── CollectiblePanel
   │   └── CollectibleText (TextMeshPro)
   └── FlyPanel
       └── FlyEnergyBar (Slider)
   ```

2. **UIController konfigurieren**:
   - Weise die neuen UI-Elemente zu:
     - Level Type Text: `LevelTypeText`
     - Level Profile Panel: `LevelProfilePanel`
   - Setze Difficulty-Farben:
     - Easy Level Color: Grün
     - Medium Level Color: Gelb  
     - Hard Level Color: Rot

### Schritt 5: Integration testen

1. **Play Mode Test**:
   - Starte die `GeneratedLevel`-Szene
   - Überprüfe:
     - Level wird automatisch generiert
     - Player spawnt korrekt
     - Collectibles sind platziert
     - UI zeigt "Level: Einfach" an
     - GoalZone ist sichtbar

2. **Regeneration testen**:
   - Drücke `R` im Play Mode → Level regeneriert sich
   - Unterschiedliche Seeds erzeugen verschiedene Layouts

---

## ⚙️ Konfiguration

### LevelProfile-Parameter

| Parameter | Beschreibung | Easy | Medium | Hard |
|-----------|--------------|------|--------|------|
| Level Size | Rastergröße (NxN) | 8 | 12 | 16 |
| Collectible Count | Anzahl Sammelobjekte | 5 | 8 | 12 |
| Obstacle Density | Hindernisdichte (0-1) | 0.1 | 0.25 | 0.4 |
| Generation Mode | Algorithmus | Simple | Maze | Maze |
| Slippery Tiles | Rutschflächen aktiv | Nein | Ja (10%) | Ja (20%) |
| Path Complexity | Wegkomplexität | 0.3 | 0.5 | 0.8 |

### Generierungsmodi

- **Simple**: Offene Fläche mit zufälligen Hindernissen
- **Maze**: Labyrinth-Generator mit Recursive Backtracking
- **Platforms**: Insel-basierte Plattformen (experimentell)
- **Organic**: Organische Strukturen (zukünftig)
- **Hybrid**: Mischung verschiedener Modi (zukünftig)

---

## 🔧 Erweiterte Nutzung

### Profile zur Laufzeit wechseln

```csharp
LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
LevelProfile hardProfile = Resources.Load<LevelProfile>("HardProfile");
generator.GenerateLevel(hardProfile);
```

### Custom Seeds verwenden

```csharp
// In LevelProfile
SetGenerationSeed(12345); // Fester Seed
SetUseTimeBasedSeed(false); // Zeitbasierte Seeds deaktivieren
```

### Ereignisse abonnieren

```csharp
LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
generator.OnLevelGenerationCompleted += (profile) => {
    Debug.Log($"Level '{profile.DisplayName}' generiert!");
};
```

---

## 🐛 Troubleshooting

### Häufige Probleme

1. **"Missing prefab references"**
   - Überprüfe, dass alle Prefab-Referenzen im LevelGenerator gesetzt sind
   - Stelle sicher, dass Prefabs im Assets/Prefabs/ Ordner liegen

2. **"No walkable area found"**
   - Reduziere `obstacleDensity` im LevelProfile
   - Erhöhe `minWalkableArea` auf 70%+

3. **"Collectibles not placed"**
   - Verringere `collectibleCount` oder `minCollectibleDistance`
   - Vergrößere `levelSize`

4. **UI zeigt keine Levelinfo**
   - Überprüfe UIController-Referenzen
   - Stelle sicher, dass LevelGenerator Events feuert

### Debug-Features

- **Show Generation Debug**: Aktiviere in LevelGenerator für detaillierte Logs
- **Show Walkable Area**: Visualisiert begehbare Bereiche in Scene View
- **Regeneration Key**: Drücke R im Play Mode für neue Generierung

---

## 🎯 Nächste Schritte (Optional)

### Phase 3.5 - Bonus Features

1. **Daily Challenge Seeds**:
   ```csharp
   int dailySeed = System.DateTime.Today.GetHashCode();
   profile.SetGenerationSeed(dailySeed);
   ```

2. **Moving Obstacles**:
   - Aktiviere `enableMovingObstacles` in Hard Profile
   - Implementiere bewegliche Plattformen

3. **Particle Effects**:
   - Weise `decorativeEffects` Array in LevelProfile zu
   - Platziere automatisch Dampf-/Steam-Effekte

### Integration mit Build System

1. **Scene Build Settings**:
   - Füge `GeneratedLevel.unity` zu Build Settings hinzu
   - Setze als Standard-Szene für prozeduale Level

2. **Performance Optimization**:
   - Object Pooling für wiederverwendete Prefabs
   - LOD System für große Level
   - Async Loading für Level-Transitions

---

## 📊 Statistiken

Das System kann folgende Metriken tracken:
- Generierungszeit pro Level
- Erfolgreiche Platzierungsrate (Collectibles)
- Begehbare Fläche in Prozent
- Seed-basierte Reproduzierbarkeit

Alle Metriken werden bei `showGenerationDebug = true` in der Console ausgegeben.

---

## ✅ Checkliste

- [ ] Alle Scripte in korrekten Ordnern
- [ ] LevelProfile-Assets erstellt (Easy/Medium/Hard)
- [ ] GeneratedLevel-Szene konfiguriert
- [ ] UI-Elemente zugewiesen
- [ ] Prefab-Referenzen gesetzt
- [ ] Play Mode Test erfolgreich
- [ ] Regeneration (R-Taste) funktioniert
- [ ] Level-Schwierigkeiten unterscheidbar
- [ ] Integration mit GameManager/LevelManager

**Das prozedurale Levelgenerierungssystem ist nun vollständig implementiert und bereit für die Erweiterung mit Daily Challenges oder externen Datenquellen!** 🎉
