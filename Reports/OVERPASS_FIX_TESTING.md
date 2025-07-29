# 🧪 Overpass API Fix - Testanleitung

## Sofort-Test der Lösung

### 1. Komponenten-Test
1. Erstelle ein leeres GameObject in deiner Level_OSM Szene
2. Füge `OverpassAPITest` Component hinzu
3. Im Inspector: "Run Tests On Start" aktivieren
4. Szene starten → Console prüfen auf "✓ SUCCESS" Nachrichten

### 2. AddressResolver-Konfiguration prüfen
```csharp
// Diese Werte sollten in AddressResolver jetzt so stehen:
useHardcodedCoordinates = false  // ✓ Produktionseinstellung
useSimpleQuery = false          // ✓ Vollständige Queries
enableBoundingBoxValidation = true  // ✓ Extra-Schutz
```

### 3. Manueller Test mit problematischen Koordinaten
```csharp
// Context Menu "Test Leipzig Address Loading" verwenden
// Oder direkt im Code:
AddressResolver resolver = FindFirstObjectByType<AddressResolver>();
resolver.ResolveAddressAndLoadMap("Leipzig, Germany");
```

### 4. Edge-Case Tests
- **Near Poles:** Koordinaten wie (89.9, 0.0) mit 500m Radius
- **Near Dateline:** Koordinaten wie (0.0, 179.9) mit 500m Radius  
- **Large Radius:** Leipzig mit 10000m Radius

## Erwartete Ergebnisse

### ✅ Erfolgreich behoben:
- Keine HTTP 400 "floats between -180.0 and 180.0" Fehler mehr
- Alle BoundingBox-Koordinaten bleiben in gültigen Bereichen
- Graceful Handling von extremen Positionen (Pole, Dateline)
- Fallback auf Leipzig bei unauflösbaren Koordinaten

### 📊 Console Output sollte zeigen:
```
[CoordinateValidator] Safe bounding box: lat[51.330000, 51.347400], lon[12.370000, 12.387800]
[AddressResolver] Validated bounding box: lat[51.330000, 51.347400], lon[12.370000, 12.387800]
[AddressResolver] SUCCESS! Received response of 1234 characters
[AddressResolver] Map data loaded successfully. Roads: 5, Buildings: 12, Areas: 2, POIs: 3
```

### ❌ Frühere Fehler sollten verschwunden sein:
```
// Diese Fehler sollten nicht mehr auftreten:
"only allowed values are floats between -180.0 and 180.0"
"Invalid bounding box dimensions"  
"Coordinates out of valid range"
```

## Bei Problemen

### Debug-Schritte:
1. **Prüfe AddressResolver Inspector**: Debug-Flags sollten deaktiviert sein
2. **Console Log aktivieren**: Alle `[AddressResolver]` und `[CoordinateValidator]` Meldungen beachten
3. **Test-Component verwenden**: `OverpassAPITest` → "Run All Coordinate Tests"
4. **Fallback prüfen**: Bei API-Fehlern sollte automatisch Leipzig geladen werden

### Häufige Ursachen falls noch Fehler:
- Debug-Flags noch aktiv (useHardcodedCoordinates=true)
- Sehr alte Koordinaten-Werte im Cache
- Netzwerk-Probleme (andere Fehlermeldungen)
- Inspector-Einstellungen überschreiben Code-Änderungen

## Integration in Spielablauf

Die Lösung ist **voll kompatibel** mit deinem bestehenden System:
- OSM-Level startet wie gewohnt über Address-Eingabe
- Endlos-Modus nach Level 3 funktioniert weiterhin
- Fallback-System bleibt aktiv
- Keine Breaking Changes an bestehenden APIs

**Status: ✅ PRODUKTIONSBEREIT**
