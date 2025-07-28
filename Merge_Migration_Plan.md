### Merge & Migration Plan

#### DUPLICATE#1 – `LevelProfileCreator.cs` & `LevelSetupHelper.cs`
**Empfehlung:**
Funktionen zur Erstellung von LevelProfiles zentral in einer Utility-Klasse `LevelProfileUtility.cs` sammeln. Menüeinträge und automatische Setup-Aufrufe nur dort definieren.
**Migration:**
* `LevelProfileCreator.cs` und `LevelSetupHelper.cs` entfernen
* Neue Klasse `LevelProfileUtility` unter `Assets/Scripts/Generators/` anlegen
* Aufrufe in `RollABallControlPanel.cs` anpassen, um `LevelProfileUtility.CreateDefaultProfiles()` zu nutzen

#### DUPLICATE#2 – API Fixer Skripte
**Empfehlung:**
`QuickAPIBatchFixer`, `ObsoleteAPIBatchFixer`, `QuickAPIFix` und `FinalAPICleanup` in ein einziges Skript `UnifiedAPIFixer.cs` konsolidieren. Spezialfunktionen wie `ControlPanelAPIFix` in dieselbe Klasse als Methoden aufnehmen.
**Migration:**
* Alle genannten Fixer-Skripte löschen
* Neues Werkzeug `UnifiedAPIFixer` im Menü `Roll-a-Ball/Fix Tools` registrieren
* Verweise in Dokumentation oder Build-Skripten aktualisieren

#### DUPLICATE#3 – Editor Menüfenster
**Empfehlung:**
`CleanRollABallMenu`, `RollABallMenuIntegration` und `RollABallControlPanelRestorer` zu einem einzigen übersichtlichen Editorfenster zusammenführen.
**Migration:**
* Gemeinsame Funktionen in `CleanRollABallMenu` belassen und andere Fenster entfernen
* Menüeinträge anpassen, sodass nur noch ein Control Panel existiert

#### DUPLICATE#4–#6 – LevelProfile Assets
**Empfehlung:**
Nur die Assets im Ordner `Assets/ScriptableObjects/` verwenden. Die Kopien in `Resources/LevelProfiles` entfernen.
**Migration:**
* Alle `Resources.Load<LevelProfile>`-Aufrufe auf direkte Referenzen oder Addressables umstellen
* Assets in `Resources/LevelProfiles` löschen, danach Verweise prüfen
