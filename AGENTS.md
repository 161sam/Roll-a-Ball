
# 🤖 AGENTEN-ÜBERSICHT

In diesem Dokument werden die verschiedenen **KI-Agenten** (OpenAI Codex, Claude via Unity MCP) beschrieben, die zur Analyse, Korrektur und Erweiterung des Roll-a-Ball Projekts eingesetzt werden. Jeder Agent fokussiert sich auf eine bestimmte Phase oder Komponente der Entwicklung mit klar definierten Zielen, Arbeitsschritten und Erfolgsindikatoren.

## 🎯 Agentenauftrag: Roll-a-Ball Scene Normalisierung & UI-Korrektur (Phase 3)

### 🧠 Agentenname

`SceneNormalizerAgent`

### 🗂️ Projektverzeichnis

`/home/saschi/Games/Roll-a-Ball/`

### 🔍 Ziel

Alle Unity-Szenen im Projekt sollen **konsistent, funktionsfähig und prefab-basiert** aufgebaut sein. Insbesondere müssen UI-Elemente korrekt verankert, referenziert und benutzbar sein. Dieser Agent identifiziert systematisch Abweichungen in den Szenen und dokumentiert sowie korrigiert diese.

### 📌 Auftragsschritte

#### 1. Szene-Erkennung

* Alle `.unity`-Dateien im Verzeichnis `Assets/Scenes/` werden aufgelistet (z. B. *GeneratedLevel.unity*, *Level1.unity*, *Level2.unity*, *Level3.unity*, *Level\_OSM.unity*, *MiniGame.unity*).
* Es wird eine Liste aller Szenennamen mit ihren Speicherpfaden erstellt, um sicherzustellen, dass keine Szene übersehen wird.

#### 2. Szenen laden & analysieren

* Der Agent lädt jede gefundene Szene nacheinander im Unity Editor (über Unity MCP) und inspiziert systematisch alle **GameObjects**, **UI-Elemente** und **Prefab-Referenzen**.
* Folgende Prüfkategorien werden für jede Szene durchlaufen:

  * **Prefab-Check:** Prüfen, ob **Ground**, **Wall**, **Collectible** und **GoalZone** als Instanzen der vorgesehenen Prefabs platziert sind. Manuell duplizierte Objekte (statt Prefab-Nutzung) werden erkannt. Ebenso wird überprüft, ob benötigte Skripte wie `CollectibleController` oder `GoalZoneTrigger` an den Objekten hängen.
  * **UI-Check:** Sicherstellen, dass ein funktionierender **Canvas** mit korrekt eingestelltem `CanvasScaler` (Scale With Screen Size) und ein **EventSystem** vorhanden sind. Alle UI-Textfelder und Buttons müssen richtig verankert sein (Anchors) und ein `UIController` sollte existieren und mit den UI-Elementen verknüpft sein.
  * **Hierarchie-Check:** Überprüfen, ob alle obligatorischen Objekte in der Szene vorhanden und richtig konfiguriert sind:

    * **Main Camera:** Existiert und hat das Skript `CameraFollow` (oder entsprechendes Kamerasteuerungs-Skript) angehängt.
    * **Player:** Existiert mit dem `PlayerController`-Skript und korrekten Physik-Einstellungen.
    * **GameManager / LevelManager:** Entsprechende Manager-Objekte sind in der Szene und verwalten den Spielzustand (z. B. Level-Fortschritt, Punktestand).
    * **Tags & Layers:** Wichtige Objekte tragen die erwarteten Tags (z. B. Player, Collectible) und befinden sich auf den korrekten Layers (z. B. Ground, Obstacles), damit Kollisionen und Trigger einheitlich funktionieren.
    * **Spezial-Objekte:** Falls die Szene steampunk-spezifische Objekte enthält (z. B. `MovingPlatform`, `RotatingObstacle`, Tore), wird geprüft, ob diese die nötigen Komponenten/Skripte besitzen und ordnungsgemäß funktionieren.

#### 3. Fehlerbericht & Korrekturvorschläge

* Für jede Szene erstellt der Agent einen Markdown-Bericht `SceneReport_<Szene>.md`. Darin werden **abweichende Befunde** festgehalten, z. B. fehlende Prefab-Verwendung, falsche Komponenten oder defekte UI-Elemente.
* Unter *⚠️ Probleme* listet der Bericht alle gefundenen Fehler und Inkonsistenzen (z. B. *"Collectible X ist kein Prefab-Instance"*, *"CanvasScaler fehlt auf Canvas"*, *"Player fehlt Tag=Player"*).
* Unter *✅ Empfehlungen* folgen konkrete Vorschläge zur Behebung jedes Problems (etwa Ersatz eines duplizierten Objekts durch das entsprechende Prefab, Hinzufügen eines EventSystems, Zuweisung fehlender Skriptreferenzen).
* Außerdem enthält der Bericht einen Abschnitt *🔄 Konsolidierungsvorschläge*, der zusammenfasst, wie die Szene insgesamt an den Standard angepasst werden kann (z. B. Ersetzen aller Boden-Objekte durch `GroundPrefab`, Neuaufbau der UI anhand einer funktionierenden Vorlage). Abschließend gibt eine *📊 Statistik* einen Überblick, wie viele Prefabs korrekt bzw. falsch instanziert wurden (z. B. *10/12 Prefabs konsistent*).

#### 4. Optional: Automatisierte Korrektur

* Wenn konfiguriert, nimmt der Agent direkt Korrekturen in den Szenen vor: Manuell duplizierte Objekte werden **gelöscht und durch Prefab-Instanzen ersetzt**. Fehlende Canvas-Elemente (Canvas, EventSystem) werden neu erstellt mit korrekten Einstellungen.
* UI-Elemente, die falsch verankert sind, werden neu positioniert oder mit geeigneten Anchor-Presets versehen. Falls die UI einer Szene unbrauchbar ist, kann der Agent optional die UI **komplett neu aufbauen** (entsprechend dem Standard-Layout aus einer funktionierenden Szene).
* Einheitliche **Kamera-, Manager- und Player-Setups** werden in allen Szenen sichergestellt (z. B. Hinzufügen des `GameManager`-Prefabs falls nicht vorhanden, Setzen der Kamera-Tag und -Follow-Skript, etc.).
* Veraltete oder duplizierte Komponenten werden entweder deaktiviert oder – falls nötig – durch neuere API-Versionen (Unity 6.1 kompatibel) ersetzt. Beispielsweise könnten obsolete Input-Handling-Komponenten durch das neue Input-System ersetzt werden, falls dies im Projektstandard so vorgesehen ist.

### ✅ Erfolgsbedingungen

Der Agent gilt als erfolgreich, wenn anschließend:

* **Keine Fehler** oder Warnungen in der Unity-Konsole beim Laden jeder Szene auftreten.
* Alle Szenen einheitlich funktionieren (Spielerbewegung, Collectibles einsammeln, Level-Abschluss etc. in jeder Szene möglich).
* Die UI in allen Szenen ist vollständig sichtbar, responsiv (skalierend) und zeigt die richtigen Informationen an (z. B. Anzahl gesammelter Objekte, Level-Name).
* **Prefab-Konsistenz > 95%**: Nahezu alle wiederverwendbaren Objekte in den Szenen basieren auf den vorgesehenen Prefabs statt auf Kopien.
* Alle relevanten **Skripte sind korrekt zugewiesen** (keine Missing Script Fehler) und wichtige Objekte haben die richtigen Tags/Layers.

Erst wenn jede Szene die obigen Kriterien erfüllt, wird die Normalisierung als abgeschlossen betrachtet.

### 🧾 Status-Tracking

Zu jeder Szene wird der Bearbeitungsstand festgehalten:

| **Szene**      | **Analysiert** | **Fehlerfrei** | **UI repariert** | **Prefabs konsistent** | **Report-Datei**                |
| -------------- | :------------: | :------------: | :--------------: | :--------------------: | ------------------------------- |
| GeneratedLevel |        ⏳       |        ⏳       |         ⏳        |            ⏳           | `SceneReport_GeneratedLevel.md` |
| Level1         |        ✅       |        ✅       |         ✅        |            ✅           | `SceneReport_Level1.md`         |
| Level2         |        ✅       |        ❌       |         ✅        |           🔄           | `SceneReport_Level2.md`         |
| Level3         |        ✅       |        ❌       |         ❌        |           🔄           | `SceneReport_Level3.md`         |
| Level\_OSM     |        ⏳       |        ⏳       |         ⏳        |            ⏳           | `SceneReport_Level_OSM.md`      |
| MiniGame       |        ⏳       |        ⏳       |         ⏳        |            ⏳           | `SceneReport_MiniGame.md`       |

> **Legende:** ✅ = erledigt, ❌ = Mangel/Fehler vorhanden, 🔄 = teilweise erledigt/in Bearbeitung, ⏳ = noch ausstehend. *(Die Tabelle wird automatisch aktualisiert, sobald Berichte erstellt oder Korrekturen durchgeführt wurden.)*

### 🧠 Agentenlogik

Der **SceneNormalizerAgent** verwendet *OpenAI Codex* (integriert über Unity MCP) zur Analyse und Modifikation der Unity-Szenen. Er hat Zugriff auf:

* **Unity Editor APIs:** Kann Szenen programmgesteuert laden (`EditorSceneManager`), Objekte finden (`FindObjectsOfType`), Prefabs instanziieren und UI-Anker setzen.
* **Dateisystem:** Darf Projektdateien lesen und schreiben (z. B. mit `read_file`, `edit_file` Aktionen), um Reports zu erzeugen oder Prefab-Verknüpfungen zu ändern.
* **Konsole & Logs:** Liest Konsolenausgaben (`read_console`), um Fehler oder Warnungen zu erkennen, die während des Szenenladens auftreten.
* **Unity Scripting:** Kann Unity-Skripte ausführen, um z. B. per Script Light die Hierarchie zu prüfen oder Objekte zu ersetzen (`ReplaceWithPrefab`, `FixUIAnchors` sind interne Routinen).

Dank dieser Fähigkeiten kann der Agent sowohl **diagnostizieren** als auch **intervenieren**, um die Szenen ohne manuelles Eingreifen zu reparieren. Alle Änderungen erfolgen skriptgesteuert und nachvollziehbar in den generierten SceneReport-Dateien.

---

## 🎯 Agentenauftrag: OpenStreetMap Integration (Phase 4)

### 🧠 Agentenname

`OSMIntegrationAgent`

### 🗂️ Projektverzeichnis

`/home/saschi/Games/Roll-a-Ball/`

### 🔍 Ziel

Die Spielszene *Level\_OSM* soll echte Kartendaten integrieren, sodass Spieler durch Eingabe einer **Adresse** ihre eigene Stadt als Roll-a-Ball-Level erleben können. Ziel ist eine vollständige **OpenStreetMap-Integration**: Straßenzüge werden zu begehbaren Pfaden, Gebäude als Hindernisse generiert und Collectibles strategisch in der echten Umgebung platziert – alles im bestehenden Steampunk-Stil des Spiels. Die Integration muss nahtlos ins Spiel passen (UI, Gameplay, Performance) und robust gegen Fehler funktionieren (Fallback bei fehlender Internetverbindung oder ungültigen Adressen).

### 📌 Auftragsschritte

#### 1. OSM-Framework einbinden

* **APIs vorbereiten:** Der Agent richtet den Zugriff auf die OpenStreetMap-APIs ein. Dazu gehört die **Nominatim API** für Geocoding (Umwandlung von Adresse zu Koordinaten) und die **Overpass API** für das Abrufen von Kartendaten (Straßen, Gebäude etc.). Es wird sichergestellt, dass keine API-Schlüssel nötig sind bzw. falls doch, diese konfiguriert werden.
* **Datenstrukturen definieren:** Es werden C#-Klassen für die OSM-Daten erstellt (z. B. eine Klasse `OSMMapData` mit Listen von Straßen, Gebäuden, Flächen und Bounds). Diese sollen serialisierbar sein, um die empfangenen JSON/XML-Daten bequem zu speichern und weiterzuverarbeiten.
* **AddressResolver entwickeln:** Ein Skript `AddressResolver` wird implementiert, das eine vom Spieler eingegebene Adresse entgegennimmt und asynchron die Geo-Koordinaten abruft. Bei Erfolg soll ein Event (z. B. `OnMapDataReady`) ausgelöst werden, das den Start der Kartengenerierung triggert. Fehlerfälle (kein Ergebnis, Zeitüberschreitung) müssen abgefangen und gemeldet werden (z. B. via `OnError` Event und UI-Dialog).

#### 2. Kartengenerierung implementieren

* **MapGenerator entwickeln:** Der Agent erstellt ein zentrales Skript `MapGenerator`, das die von `AddressResolver` erhaltenen OSM-Daten in **Unity-GameObjects** umwandelt. Straßen (`highway` in OSM) werden z. B. als begehbare Wege generiert (z. B. durch Instanziieren eines Straßen-Prefabs oder Extrudieren eines Meshes entlang der Koordinaten). Gebäude (`building` in OSM) werden als Hindernisse platziert (z. B. platzierte Quader mit Höhe basierend auf Gebäude-Tags oder standardisiert). Parks und Gewässer können mit speziellen Materialien oder flachen Objekten dargestellt werden, um visuelle Abwechslung zu bieten.
* **Prefab-Nutzung:** Soweit möglich nutzt der MapGenerator vorhandene Prefabs aus dem Projekt: Für Straßen ein Boden- oder Straßen-Prefab (z. B. `GroundPrefab`), für Gebäude ggf. `WallPrefab` oder ein neues Gebäude-Prefab, für Sammelobjekte natürlich das vorhandene `CollectiblePrefab`. Die **Steampunk-Optik** wird beibehalten, indem z. B. spezielle Materialien (Kupfer, Messing) auf die generierten Objekte gelegt werden.
* **Player-Integration:** Nach Generierung der Karte wird der Spieler-Ball (`Player`-Prefab) an der Startposition (typischerweise der Mittelpunkt der eingegebenen Adresse oder ein definierter Spawnpunkt) platziert. Sollte bereits ein Player in der Szene vorhanden sein, wird dieser ggf. repositioniert. Zusätzlich wird ein **GoalZone**-Prefab in angemessener Entfernung oder an einem markanten Ort platziert, um ein Ziel für den Spieler zu definieren.
* **Performance optimieren:** Da potenziell viele Objekte generiert werden, achtet der Agent auf Performance-Optimierungen: Generierung findet in **Koroutinen** oder über mehrere Frames verteilt statt, um Frame-Drops zu vermeiden. Außerdem wird **GPU Instancing** oder Batching aktiviert, wo immer möglich (z. B. für viele Straßen-Segmente oder Gebäude-Wände, die alle dasselbe Material nutzen).

#### 3. Scene Setup & UI-Integration

* **Level\_OSM Scene vervollständigen:** Der Agent prüft, ob die Szene *Level\_OSM* bereits alle nötigen Grund-Objekte enthält. Falls nicht, werden diese hinzugefügt: z. B. ein **Canvas** mit einem Panel für die Adresseingabe, ein **InputField** (für Adresse), ein **Button** ("Karte laden"), eine Text-Anzeige für Ladefortschritt, etc. Ein EventSystem wird ebenfalls sichergestellt.
* **UI-Verknüpfung:** Ein Skript `MapStartupController` wird entwickelt oder konfiguriert, das die Brücke zwischen UI und Backend schlägt. Dieses Skript registriert z. B. den Button-Klick ("Karte laden"), sammelt die Adresse aus dem InputField und ruft den `AddressResolver` auf. Ebenso verwaltet es die UI-Anzeigen (Progress-Bar, Fehlermeldungen), während die Karte generiert wird. Alle UI-Elemente werden korrekt im Canvas verankert, damit sie auf unterschiedlichen Auflösungen sichtbar bleiben.
* **Event-Verkettung:** Die einzelnen Komponenten werden verbunden: *AddressResolver* informiert *MapGenerator*, wenn die Daten bereit sind; *MapGenerator* informiert *MapStartupController*, wenn die Welt aufgebaut ist. Ebenso wird das GameManager/LevelManager-System benachrichtigt, dass ein neues Level gestartet wurde, damit bestehende Systeme (z. B. Zeitnahme, Punkte) weiter funktionieren.
* **Build-Settings aktualisieren:** Die Szene *Level\_OSM* wird den **Build Settings** des Unity-Projekts hinzugefügt, damit sie auch in gebauten Versionen des Spiels enthalten ist. Der Agent prüft außerdem, ob für plattformspezifische Einstellungen etwas angepasst werden muss (z. B. Internetzugriff für Android erlauben).

#### 4. Testing & Fallbacks

* **Funktionstest mit Beispieldaten:** Der Agent führt die Szene im Play-Modus aus (ggf. via Simulation) und testet mit mehreren Beispieladressen:

  * *"Leipzig, Augustusplatz"*: Erwartet wird das Laden einer Stadtumgebung mit Straßen und Gebäuden im Zentrum von Leipzig.
  * *Ungültige Adresse* (z. B. "asdlfkj"): Es sollte eine Fehlermeldung erscheinen und ggf. das **Fallback-System** greifen.
* **Fallback-System:** Bei jeglichen Fehlern im Ablauf (kein Internet, API antwortet nicht, Parsing-Fehler) stellt der Agent sicher, dass ein Fallback greift. Standardmäßig wird eine vordefinierte Stadt (z. B. Leipzig Markt) verwendet. D. h. der Agent prüft, ob `enableFallbackMode` in *MapStartupController* aktiviert ist und die Fallback-Koordinaten hinterlegt sind. Bei einem erzwungenen Fallback-Durchlauf wird kontrolliert, dass zumindest eine kleine Dummy-Stadt generiert wird, damit der Spieler weiterspielen kann.
* **Leistungstest:** Nach erfolgreicher Generierung beobachtet der Agent die Framerate im Editor. Das Ziel ist, dass auch komplexere Stadt-Ausschnitte das Spiel nicht unspielbar machen. Falls nötig, macht der Agent Vorschläge, wie die Performance weiter verbessert werden könnte (z. B. weniger Details generieren, Simplify Meshes, kleinere Umkreissuche für OSM-Daten).
* **Speicherbereinigung:** Der Agent prüft, dass nach Verlassen der Szene oder beim Neustart generierte Objekte aufgeräumt werden (z. B. beim Laden einer neuen Adresse alte GameObjects löschen), um Speicherlecks zu vermeiden.

#### 5. Optional: Erweiterte Features

* **Street View Integration:** Als Ausbaustufe kann der Agent einen Ausblick auf Street-View-Integration geben. Z. B. könnte er vorschlagen, für markante Punkte Panorama-Texturen zu laden oder ein kleines Vorschaubild aus Google Street View API einzublenden, um die Immersion zu erhöhen. (Diese Schritte werden nur als Idee dokumentiert, nicht automatisch implementiert.)
* **GPS & Standort**: Perspektivisch könnte ein **GPS-Feature** integriert werden, mit dem der Spieler anstelle einer Adresse seinen aktuellen Standort laden kann. Der Agent vermerkt, welche Schnittstellen dafür vorbereitet werden müssten (z. B. Unity LocationService für mobile Plattformen).
* **Offline-Modus:** Optional wird festgehalten, wie ein Offline-Spielmodus aussehen könnte (z. B. Nutzung zuvor geladener Kartendaten oder eines begrenzten lokalen Kartenabschnitts), falls Internet nicht verfügbar ist.

### ✅ Erfolgsbedingungen

Die OpenStreetMap-Integration gilt als erfolgreich, wenn:

* **Adresseingabe und Laden** in *Level\_OSM* ohne Entwickler-Eingriff funktionieren: Der Spieler kann eine Adresse eingeben, auf *Karte laden* klicken und binnen kurzer Zeit erscheint die entsprechende Spielwelt.
* **OSM-Objekte korrekt generiert** werden: Straßen verlaufen begehbar, Gebäude stehen an plausiblen Positionen und bilden Hindernisse, Collectibles sind verteilt und erreichbar, ohne in der Luft zu schweben oder unzugänglich zu sein.
* Der **Steampunk-Stil** bleibt konsistent: Alle generierten Objekte verwenden die vorgesehenen Materialien, Prefabs und Effekte (z. B. Dampfeffekte in Straßen oder an Gebäuden, falls vorgesehen), sodass kein Bruch im visuellen Design entsteht.
* **Performance**: Die generierte Szene läuft mit ausreichender Bildrate (idealerweise >30 FPS auf durchschnittlicher Hardware). Größere Kartenabschnitte dürfen die Engine nicht einfrieren; die Ladezeit ist angemessen (ein paar Sekunden, aber keine Minuten).
* **Stabilität**: Fallbacks greifen bei Fehlern. D. h. keine Abstürze oder Endlos-Schleifen bei Netzwerkausfall oder ungültigen Eingaben. Wenn APIs ausfallen, wird automatisch die Standardstadt geladen und das Spiel bleibt spielbar.
* **Keine Regressionen**: Die Integration darf bestehende Spielmechaniken nicht brechen. Insbesondere müssen Kamerasteuerung, Playercontroller, UI-Anzeigen (Punktestand, Timer) weiterhin funktionieren, auch wenn die Level-Geometrie nun dynamisch erstellt wird.

### 🧠 Agentenlogik

Der **OSMIntegrationAgent** greift ebenfalls auf *OpenAI Codex* über Unity MCP zurück, mit Spezialisierungen für Web- und Datenoperationen:

* **HTTP-Anfragen**: Der Agent kann Unity-intern Networking-Funktionen nutzen (z. B. `UnityWebRequest`) um API-Aufrufe an Nominatim und Overpass durch den Editor zu simulieren. (Hinweis: Im Editor kann dies mit Coroutine/EditorCoroutine geschehen, da echte HTTP-Aufrufe außerhalb des Spielmodus erfolgen müssen.)
* **Datenparsing**: Mithilfe von Codex generiert der Agent Parser für das JSON/XML der API-Antworten (beispielsweise mit Newtonsoft Json oder SimpleJSON).
* **Scene Manipulation**: Der Agent verwendet Unity APIs, um Objekte zu erzeugen (`GameObject.Instantiate` für Prefabs) und transformieren. Er kann komplexe Berechnungen (z. B. Geodaten in Unity-Koordinaten umrechnen) durchführen und direkt im Editor testen.
* **Editor-Scripting**: Da Setup-Schritte oft Editor-Arbeit erfordern (z. B. Hinzufügen der Scene zu Build Settings, Erstellen von Menu-Items), nutzt der Agent die Unity Editor Scripting API. Beispielsweise kann er ein EditorWindow erstellen (`OSMEditorExtension`), um manuelle Eingriffe zu erleichtern, oder Menüpunkte wie *Roll-a-Ball → OSM → Setup OSM Scene* programmatisch hinzufügen.
* **Fehleranalyse**: Falls während der Implementierung Fehler auftreten (Exceptions, NullRefs), erkennt der Agent diese via Konsole und passt den Code entsprechend an (z. B. fügt `if`-Checks ein, erhöht Timeouts, etc.).

**Hinweis:** Die OSM-Integration erfordert potenziell Internetzugriff. Falls die Ausführung in einer isolierten Umgebung erfolgt, generiert der Agent Beispiel-Datenstrukturen, um die Logik dennoch testen zu können. Abschließend aktualisiert der Agent die Projektdokumentation (z. B. erstellt/aktualisiert `OSM_INTEGRATION_USER_GUIDE.md` und einen technischen Report *PHASE\_4\_COMPLETE\_SUMMARY.md*) und markiert Phase 4 im `README.md` als abgeschlossen.

---

## 🎯 Agentenauftrag: Multiplayer-Kompatibilität (Phase 5)

### 🧠 Agentenname

`MultiplayerAgent`

### 🗂️ Projektverzeichnis

`/home/saschi/Games/Roll-a-Ball/`

### 🔍 Ziel

Das Spiel soll **Multiplayer-fähig** werden, sodass mehrere Spieler gleichzeitig in den Roll-a-Ball Levels spielen können. Geplant ist zunächst ein **kooperativer Modus** (z. B. gemeinsames Einsammeln von Collectibles) und perspektivisch auch kompetitive Elemente (Wettlauf um Punkte oder Zeit). Der Agent bereitet das Projekt auf Netzwerkfähigkeit vor, passt bestehenden Code an für mehrere Spieler und stellt sicher, dass Synchronisation, Spielablauf und UI für alle Beteiligten korrekt funktionieren.

### 📌 Auftragsschritte

#### 1. Networking-Framework einrichten

* **Netzwerk-API Auswahl:** Der Agent evaluiert verfügbare Unity-Multiplayer-Frameworks (z. B. **Unity Netcode for GameObjects** (NGO) vs. **Mirror** oder Photon). Für eine Open-Source-Lösung bietet sich *Mirror* an, während NGO eine offizielle Lösung ist. Entscheidungskriterien (Latency, Ease of integration, Doku) werden notiert.
* **Grundsetup:** Nachdem ein Framework gewählt ist, integriert der Agent dieses ins Projekt (entsprechende Packages importieren, ggf. Project Settings anpassen). Ein zentrales **NetworkManager**-Objekt wird eingerichtet, das die Verbindung zwischen Host und Clients verwaltet. Hier definiert man z. B. maximale Spieleranzahl, Transport-Protokoll (kann z. B. UNet Transport oder KCP für Mirror sein) und grundlegende Callbacks (OnServerConnect, OnClientDisconnect etc.).
* **Lobby/Testumgebung:** Optional erstellt der Agent eine einfache Lobby-Szene oder benutzt *GeneratedLevel.unity* als Test, in der per Tastendruck entweder ein Host gestartet oder ein Client dem Spiel beitritt. Dies dient zum schnellen Testen der Verbindung, bevor Gameplay synchronisiert wird.

#### 2. Spielobjekte für Networking vorbereiten

* **Player-Prefab networkfähig machen:** Der existierende Spieler-Ball (`Player` GameObject/Prefab) wird um Netzwerk-Funktionen erweitert. Je nach Framework wird z. B. eine `NetworkIdentity` (Mirror) oder `NetworkObject` (Unity Netcode) Komponente hinzugefügt. Der Player-Controller-Code muss so angepasst werden, dass er die Bewegung **nur für den eigenen Spieler** ausführt und die Position/Rotation über das Netzwerk synchronisiert. (Oft mittels *Client Authority* oder transform sync Komponente).
* **Synchronisation der Collectibles:** Alle **Collectible-Objekte** müssen bei allen Spielern konsistent sein. Der Agent ändert das Collectible-System dahingehend, dass das Einsammeln eines Objekts vom Server autoritativ gehandhabt wird: Wenn ein Spieler einsammelt, wird das Objekt auf dem Server zerstört und dieser Zustand an alle Clients repliziert. Dies verhindert, dass verschiedene Spieler dasselbe Item mehrfach sehen oder einsammeln können.
* **Goals & Trigger:** Ähnlich wie Collectibles müssen Endzonen/Trigger (z. B. `GoalZone`) so angepasst werden, dass sie nur einmal feuern und allen Spielern das Levelende signalisieren. Der Agent implementiert, dass beim Erreichen der GoalZone durch einen beliebigen Spieler ein Event an alle gesendet wird (z. B. *LevelComplete*), woraufhin die Runde für alle endet.
* **Multiplayer-taugliche Camera:** Die Kamera-Verfolgung muss pro Spieler-Instanz getrennt funktionieren. Im einfachsten Fall wird für jeden Client die lokale Kamera verwendet, die dem eigenen Player folgt. Der Agent stellt sicher, dass die Kamera nicht versucht, mehreren Spielern gleichzeitig zu folgen. Gegebenenfalls spawnt der NetworkManager pro Client eine eigene Kamera oder der Player spawnt mit einer Kamera als Kindobjekt.

#### 3. UI & Spiellogik anpassen

* **UI für mehrere Spieler:** Der Agent passt die UI-Elemente wie Punktestand und Collectible-Zähler an, sodass sie pro Spieler oder für das gesamte Team funktionieren. Beispielsweise könnte bei kooperativem Modus allen Spielern ein gemeinsamer Collectible-Zähler (X/Y gefunden) angezeigt werden, der synchron aktualisiert wird. In kompetitiven Modi bräuchte jeder Spieler eine eigene Anzeige seines Scores – hierfür kann der Agent das HUD duplizieren und an verschiedene Ecken des Bildschirms platzieren oder ein Tabellensystem einführen.
* **Respawn/Restart-Mechanik:** In Multiplayer müssen Abläufe wie "Neustart" oder "Level neu generieren (R-Taste)" koordiniert ablaufen. Der Agent implementiert, dass ein Level-Reset vom Server initiiert wird und an alle Clients gesendet. So wird verhindert, dass nur der lokale Client neu lädt. Ähnlich wird bei einem Spielende (alle Collectibles gesammelt oder Zeit abgelaufen) eine Synchronisation eingeführt, damit alle Spieler gleichzeitig ins nächste Level wechseln oder zurück zur Lobby gehen.
* **Kooperations-Logik:** Falls Koop-Modus, stellt der Agent sicher, dass **Team-Ziele** korrekt berechnet werden (z. B. Level geschafft, wenn *gesamt* alle Collectibles gesammelt sind). Falls kompetitiv, muss die Spielwertung getrennt geführt werden. Der Code im GameManager/LevelManager wird dahingehend erweitert, mehrere Spielerobjekte zu verwalten (evtl. Liste von Spielern statt single Player reference) und Spielzustände pro Spieler (z. B. wer hat wie viele Collectibles) oder global zu unterscheiden.
* **Synchronisations-Test:** Der Agent fügt temporär Debug-UI oder Logs ein, um zu prüfen, ob wichtige Zustände synchron sind (z. B. eine kleine Konsole, die anzeigt, wie viele Spieler verbunden, wie viele Collectibles aus Sicht jedes Clients verbleiben). Diese Hilfen ermöglichen es, Desynchronisationen früh zu erkennen.

#### 4. Multiplayer-Test & Feinschliff

* **Lokaler Mehrspieler-Test:** Der Agent startet das Spiel in zwei Instanzen (im Editor als Host und in einem Build als Client, oder zwei Builds) auf dem gleichen Rechner, um den Multiplayer zu prüfen. Szenarien: Beide Spieler bewegen ihren Ball, sammeln Objekte, erreichen die Ziellinie. Es wird beobachtet, ob alle Ereignisse korrekt bei beiden ankommen. Insbesondere Latenz/Synchronisations-Verhalten (Teleportierender Ball? Rubberbanding?) wird begutachtet. Bei Bedarf schlägt der Agent Verbesserungen vor, z. B. **Interpolation** oder **Prediction** für flüssigere Bewegung der Fremd-Players.
* **Edge-Case Tests:** Spieler verlassen das Spiel vorzeitig (Verbindungstrennung) – der Agent prüft, dass der Spielzustand damit umgehen kann (z. B. verbleibende Spieler können weiter spielen, das Objekt des getrennten Spielers wird entfernt). Ebenso wird ein neuer Client mitten im Spiel getestet, falls unterstützt (Late Joiner synchronisiert aktuellen Spielstand korrekt).
* **Performance & Bandbreite:** Der Agent analysiert, ob die Netzwerklast im Rahmen bleibt. Falls z. B. zu viele Objekte zu häufig Updates senden, empfiehlt er Optimierungen (z. B. Sync seltener, relevancy checks – nur nahe Objekte updaten). Auch wird geschaut, dass keine großen Garbage-Spikes durch Netzwerk erzeugt werden (z. B. durch exzessive Allocations in Update).
* **Dokumentation & Kompatibilität:** Abschließend erstellt der Agent einen Report `MultiplayerIntegrationReport.md`, der zusammenfasst, welche Änderungen vorgenommen wurden, welche Einschränkungen bestehen (z. B. *"Maximal 4 Spieler getestet"*, *"Physics derzeit nicht deterministisch synchron – kann zu Abweichungen führen"*) und welche Schritte für voll robuste Multiplayer-Unterstützung noch empfohlen werden. Außerdem werden im Projekt-README neue Features (Multiplayer Koop/Competitive) vermerkt, und die **Roadmap Phase 5** kann entsprechend als begonnen/teilerfüllt markiert werden.

#### 5. Optional: Erweiterte Mehrspieler-Features

* **Globale Ranglisten:** Als Ausblick kann der Agent schon die Integration eines Leaderboard-Services vorschlagen. Z. B. Nutzung einer einfachen Web-API, an die Scores gemeldet werden. Dieses Feature wird noch nicht implementiert, aber in der Dokumentation als nächster Meilenstein notiert.
* **Tägliche Herausforderungen:** Der Agent skizziert, wie tägliche Herausforderungen im Multiplayer aussehen könnten (z. B. täglich generierter Level-Seed, Koop-Zeitangriff für alle Spieler mit Ranking). Solche Ideen werden gesammelt, um die Weiterentwicklung zu steuern.
* **Plattform-Übergreifend & VR:** Falls relevant, erwähnt der Agent, ob das Multiplayer-System auch für zukünftige VR-Unterstützung oder Cross-Platform (PC/Mobile) verwendbar ist oder welche Anpassungen nötig wären (z. B. andere Steuerung, Networking in VR Kontext mit Motion-Sickness-Vermeidung bei Zuschauer-Modus etc.).

### ✅ Erfolgsbedingungen

Die Multiplayer-Vorbereitung ist erfolgreich, wenn:

* **Mehrere Spieler** gleichzeitig im selben Spiellevel interagieren können, ohne Fehler. Beispielsweise können zwei Spieler in Level1 gemeinsam alle Collectibles sammeln und das Level beenden, wobei beide Clients synchron den Abschluss registrieren.
* **Spielzustand Synchronität:** Wichtige Spielzustände (Positionen der Spieler, verbleibende Collectibles, Punkte) sind bei allen Teilnehmern konsistent. Es treten keine duplizierten Objekte, "Geister-Collectibles" oder widersprüchlichen Anzeigen auf.
* **Stabilität im Netzwerk:** Ein Verbindungsaufbau ist möglich (Spieler können hosten/joinen), und das Spiel läuft über ein paar Minuten ohne Verbindungsabbruch oder mit sauberem Handling, falls doch jemand trennt. Es gibt keine massiven Lags oder unkontrollierte Physik-Effekte durch die Netzwerklatenz.
* **Bestehender Singleplayer unverändert:** Wichtig ist, dass der normale Einzelspielermodus weiterhin einwandfrei funktioniert, falls man solo spielt. Die Multiplayer-Erweiterungen dürfen das Singleplayer-Erlebnis nicht beeinträchtigen (z. B. sollte im Einzelspielermodus kein NetworkManager stören oder Fehler werfen).
* **Skalierbarkeit:** Das System ist so ausgelegt, dass auch mehr als 2 Spieler unterstützt werden könnten (mindestens 4 Spieler in Tests erfolgreich). Zudem sollte es möglich sein, weitere Multiplayer-Features (Chat, Team-Mechaniken) relativ einfach zu integrieren, ohne das Grundgerüst neu zu erfinden.

### 🧠 Agentenlogik

Der **MultiplayerAgent** nutzt OpenAI Codex, um komplexe **Netzwerk-Programmieraufgaben** zu bewältigen:

* **Framework-Kenntnis**: Codex wird mit Dokumentation oder gängigen Mustern des gewählten Netzwerksystems angeleitet, um korrekt z. B. `NetworkBehaviour`-Klassen, RPCs oder Synchronisierungs-Attributes zu schreiben.
* **Code-Refactor**: Viele bestehende Klassen müssen angepasst werden. Der Agent durchforstet die Codebasis und findet Stellen, die aktuell von einem einzelnen Spieler ausgehen (z. B. Referenzen auf "Player" Objekt). Codex hilft dabei, diese so zu generalisieren, dass sie mit einer dynamischen Liste von Spielern umgehen können.
* **Merge & Konfliktlösung**: Sollte das Projekt bereits fortgeschritten sein, kann das Einfügen von Networking-Code Konflikte mit bisherigen Systemen auslösen. Der Agent nutzt Codex’ Kontextverständnis, um Lösungen zu finden (z. B. Bedingungen einzubauen "if (IsClient) return;" für Logik, die nur am Server laufen soll, etc.).
* **Testing Automation**: Der Agent kann kleine Testskripte generieren, die im Editor automatisiert zwei Players simulieren (z. B. zwei Editor PlayModes via Multi-Instance starten, falls unterstützt). Dies ermöglicht es, wiederholt die Grundfunktionalität zu prüfen.
* **KI-Unterstützte Fehlersuche**: Sollten Bugs auftreten (typische Netzwerkrace-Conditions, null references auf Clients, etc.), nutzt der Agent die KI, um die Ursachen im Code zu finden und zu beheben. Mögliche Lösungen zieht er aus bekannten Patterns (z. B. WaitForNetworkObjectSpawn, Scenes als Server synchron laden, etc.).

Nachdem der Agent die Multiplayer-Integration fertiggestellt hat, aktualisiert er die Projektdokumentation. Die **README.md** erhält einen Abschnitt über Multiplayer, und die *Roadmap* Phase 5 Punkte (zumindest "Multiplayer-Unterstützung") können als erledigt markiert werden. Auch der erzeugte `MultiplayerIntegrationReport.md` wird dem Repository hinzugefügt.

---

## 🎯 Agentenauftrag: Performance-Optimierung & Build-Automatisierung (Phase 6)

### 🧠 Agentenname

`PerformanceBuildAgent`

### 🗂️ Projektverzeichnis

`/home/saschi/Games/Roll-a-Ball/`

### 🔍 Ziel

In dieser Phase wird das Spiel für den **Release vorbereitet**. Zwei Hauptaspekte stehen im Fokus: zum einen die **Performance-Optimierung** (das Spiel soll flüssig auf den Zielplattformen laufen, ohne unnötige Ressourcen zu verbrauchen) und zum anderen die **Build-Automatisierung** (reibungslose Erstellung von Builds für verschiedene Plattformen mit minimalem manuellem Aufwand). Der Agent identifiziert Performance-Engpässe im Spiel, verbessert diese und richtet Skripte/Workflows ein, um das Erstellen neuer Builds und Releases effizient zu gestalten.

### 📌 Auftragsschritte

#### 1. Profiling & Analyse

* **Profiler-Durchlauf:** Der Agent führt das Spiel in verschiedenen Szenarien aus (kleines Level, großes Level, OSM-Level, Multiplayer) und sammelt Daten mit dem Unity Profiler. CPU-Auslastung pro Frame, Garbage Collection Spikes, Render-Stallings und GPU-Auslastung werden protokolliert. Insbesondere achtet der Agent auf **kritische Engpässe**: z. B. ob die **Generierungskoroutinen** große Last erzeugen, ob die **Physikberechnungen** (für rollende Bälle, Kollisionen) viel Zeit benötigen oder ob die **Rendering**-Last (Partikeleffekte, Beleuchtung) hoch ist.
* **Bottlenecks identifizieren:** Anhand der Profiler-Daten erstellt der Agent eine Liste der größten Übeltäter. Beispielsweise könnte herauskommen:

  * Hohe CPU-Last durch häufige **Garbage Collection** (viele temporäre Allokierungen pro Frame).
  * Frame-Drops beim Laden neuer Level (vielleicht ungünstiges Timing in der LevelGenerator-Koroutine).
  * Partikelsysteme mit zu vielen Partikeln gleichzeitig.
  * Unnötig hohe **Draw-Call**-Zahlen (zu viele einzelne Objekte ohne Batching).
* **Diagnose-Report:** Diese Befunde werden in einem kurzen `PerformanceReport.md` zusammengefasst, priorisiert nach Impact auf FPS.

#### 2. Code- und Szenenoptimierung

* **Skript-Optimierungen:** Auf Basis der Analyse optimiert der Agent kritische Code-Stellen. Beispielsweise werden temporäre List-Allocations in der Update-Schleife vermieden, indem Lists vorab erstellt und wiederverwendet werden. Koroutinen werden geprüft, ob sie vielleicht zu große Arbeitspakete pro Frame erledigen – ggf. fügt der Agent zusätzliche `yield`-Schritte ein, um die Last zu verteilen. Physik-Abfragen (wie `Physics.OverlapSphere` für Collectibles) könnten gecacht oder seltener ausgeführt werden.
* **Object Pooling:** Der Agent implementiert ein **Object Pooling**-System für häufig erstellte/destroyte Objekte (z. B. Collectibles, Partikelobjekte). Das Ziel ist, teure Instantiierung/Destruction im laufenden Spiel zu reduzieren. Es wird ein Pool angelegt, der z. B. X Collectible-Objekte vorhält und wiederverwendet, anstatt ständig neue zu erzeugen.
* **Grafik-Tuning:** Gemeinsam mit den Entwicklern oder anhand der Zielplattformen passt der Agent die **Quality Settings** und Render-Optionen an. Beispielsweise könnte die Schattenqualität reduziert oder ein kürzerer Schattenabstand gesetzt werden, wenn das Spiel auf schwächeren Geräten laufen soll. Auch wird geprüft, ob **Occlusion Culling** aktiviert und konfiguriert ist (für die Labyrinth-Levels sinnvoll, damit nicht alle Objekte immer gerendert werden).
* **Level-of-Detail (LOD):** Für 3D-Modelle (falls vorhanden, z. B. Steampunk-Deko) richtet der Agent LOD-Stufen ein oder nutzt Unitys **LOD Group**-Komponente, um weit entfernte Objekte günstiger zu rendern. Sollte das Projekt wenige komplexe Modelle haben, ist dies ggf. vernachlässigbar.
* **Mobile Optimierungen:** Falls Android/WebGL Targets geplant sind, sorgt der Agent für plattformspezifische Einstellungen: z. B. Texturkompression für Mobile, begrenzte framerate oder Abschalten von aufwendigen Post-Processing für WebGL.

#### 3. Automatisierte Build-Pipeline einrichten

* **Build-Skripte erstellen:** Der Agent automatisiert den Build-Prozess mittels Unity **Batchmode** oder Editor-Skripten. Beispielsweise wird ein Skript `BuildAll.cs` oder eine Reihe von Skripten erstellt, die per Menü oder CLI alle Zielplattformen bauen. Diese Skripte verwenden die Unity Editor API (`BuildPipeline.BuildPlayer`) und berücksichtigen unterschiedliche Profile:

  * *Standalone (Windows/macOS/Linux)*: Evtl. separate Ordner pro OS, oder zumindest Einstellungen wie x86\_64 Architektur.
  * *Android*: Setzt automatisch den Build auf IL2CPP, ARM64, und signiert ggf. mit einem Keystore (Stub, falls nicht vorhanden).
  * *WebGL*: Aktiviert Kompression, setzt Memory Size passend, etc.
* **One-Click Build:** Im Unity-Menü erscheint unter *Roll-a-Ball → Build* nun Optionen wie *Build Standalone*, *Build Android*, *Build WebGL*. Der Agent stellt sicher, dass vor dem Build alle Szenen in Build Settings eingetragen sind (inkl. neu hinzugekommene *Level\_OSM*, etc.) und dass die Builds in einen definierten Ordnerpfad ausgegeben werden.
* **CI/CD Vorbereitung:** Falls das Projekt auf GitHub oder einem ähnlichen Repository ist, bereitet der Agent eine einfache Continuous Integration vor. Z. B. generiert er ein GitHub Actions Workflow YAML, das bei einem Push einen Unity Build Container startet und das Projekt baut (dies erfordert Unity-Lizenz in CI, was evtl. nur skizziert wird). Der Agent dokumentiert die Schritte, die nötig wären, um das CI zum Laufen zu bringen (viele Open-Source-Projekte nutzen z. B. Game.CI images für Unity in GitHub Actions).

#### 4. Multi-Plattform Tests

* **Build-Verifizierung:** Der Agent führt nach jedem erstellten Build einen kurzen Test durch. Für Standalone könnte er automatisiert das Spiel starten (im Hintergrund) und prüfen, ob es abstürzt oder bestimmte Logs ausgibt. Für Android kann er keinen echten Lauf durchführen, aber er prüft die APK-Größe und ob der Build überhaupt erfolgreich ist. WebGL könnte er lokal im Browser öffnen (falls automatisierbar) oder zumindest sicherstellen, dass die Build-Dateien erzeugt wurden.
* **Performance nach Build:** Gerade auf mobilen/WebGL Plattformen testet der Agent die Performance mit den finalen Einstellungen. Er achtet auf Unterschiede zum Editor-Profiling (z. B. im WebGL-Build könnte die Performance anders ausfallen). Falls schwere Probleme auftauchen (z. B. WebGL memory issues), nimmt er entsprechende Anpassungen vor (z. B. mehr Heap oder Asset Stripping).
* **Fehlerbereinigung:** Sollten Plattform-spezifische Fehler auftreten (z. B. ein Script benutzt eine API, die in WebGL nicht unterstützt wird), identifiziert der Agent diese via Build-Logs und passt den Code an, um die Kompatibilität herzustellen.
* **Abschlusstests:** Am Ende führt der Agent eine vollständige Testrunde der wichtigsten Spielabläufe auf jeder Plattform durch (soweit möglich). Das heißt: einmal Level spielen in Standalone PC, auf Android Gerät (manuell, sofern der Agent Bericht von Testern bekommt), und WebGL im Browser. Dabei wird sichergestellt, dass die Spielerfahrung konsistent und fehlerfrei ist.

#### 5. Optional: Veröffentlichungsvorbereitung

* **Release Build optimieren:** Der Agent schlägt vor, für einen tatsächlichen Release noch Schritte wie **IL2CPP Code-Stripping** zu verfeinern, **Profiler**-Anbindungen zu entfernen, Debug-Logs zu reduzieren, um die Build-Größe klein und die Performance hoch zu halten.
* **Installationspakete:** Optional können Skripte erweitert werden, um Installationsprogramme oder Archive zu erstellen (z. B. ZIP der Standalone-Builds, APK Signing, etc.).
* **Store-Setup:** Der Agent dokumentiert, welche Schritte für eine Veröffentlichung nötig wären (z. B. Vorbereitung einer Itch.io Seite, oder Einreichen in Google Play Store), auch wenn diese Aufgaben außerhalb des direkten Code-Bereichs liegen. Dies dient als Checkliste für die Entwickler.
* **Zukünftige Automatisierung:** Als Ausblick kann der Agent anmerken, wie man die Tests weiter automatisieren könnte (z. B. Integrationstests oder Verwendung von Unity Test Framework, sodass in CI nicht nur gebaut, sondern auch automatisch bestimmte Gameplay-Tests durchlaufen werden).

### ✅ Erfolgsbedingungen

Die Phase Performance & Build ist erfolgreich, wenn:

* **Bildrate und Speicher**: Das Spiel erreicht die angestrebte **Framerate** (z. B. 60 FPS auf Desktop, 30+ FPS auf Mobile) in allen regulären Szenen. Speicherverbrauch bleibt im Rahmen und es gibt keine auffälligen Memory-Leaks oder überlaufenden Garbage Collections im Spielverlauf.
* **Reibungsloser Ablauf**: Levelübergänge, insbesondere das prozedurale Generieren (GeneratedLevel, OSM-Level), verursachen keine merklichen Stotterer mehr. Die Spielerfahrung ist glatt und ohne lange Pausen.
* **Kleine Build-Größe**: Unnötige Dateien sind nicht im Build. Die Gesamtgröße des Builds ist optimiert (z. B. keine inkludierten Library/-Ordner, keine übergroßen ungenutzten Assets). Nach Möglichkeit bleibt das Projekt (vor allem WebGL) leichtgewichtig.
* **Build-Prozess vereinfacht**: Ein Entwickler kann mit minimalem Aufwand einen neuen Build für eine Plattform erzeugen, idealerweise durch einen einzelnen Befehl oder Klick. Alle wichtigen Szenen und Assets werden zuverlässig in die Builds einbezogen.
* **Plattform-Kompatibilität**: Die Builds laufen auf den angegebenen Plattformen ohne Abstürze. Windows, Linux, macOS sollten gleichermaßen bedient werden. Für Android gilt, dass die App auf einem Testgerät installiert und gestartet werden kann, für WebGL, dass es in gängigen Browsern lädt und spielbar ist.
* **Automatisierungsgrad**: Bonus-Ziel ist erreicht, wenn ein CI-System den Build automatisiert durchführen könnte (d. h. Skripte ohne Editor-GUI funktionieren). Auch ohne voll eingerichtetes CI soll zumindest die lokale Automatisierung zuverlässig funktionieren.

### 🧠 Agentenlogik

Der **PerformanceBuildAgent** kombiniert Fähigkeiten in den Bereichen Profiling, Code-Optimierung und Automatisierung:

* **Profiler API & Analytics**: Der Agent kann via Code auf Unitys Profiler-Daten zugreifen (z. B. mit Development Build und ProfilerConnection) oder alternativ die Ausgabe des Profilers interpretieren. Gegebenenfalls werden Editor-integrierte Profiler-Markierungen (ProfilerMarkers) in den Code eingefügt, um Engpässe besser zu erkennen. Die KI hilft dabei, Muster zu erkennen (z. B. wiederkehrende GC.Alloc in bestimmten Funktionen).
* **Code-Verbesserung**: Codex kann auf Performance Best Practices zurückgreifen. Es schlägt z. B. vor, teure LINQ-Ausdrücke durch herkömmliche Schleifen zu ersetzen, bestimmte Update()-Aufrufe zu reduzieren (z. B. durch Zusammenlegen von Tasks, oder Nutzung von Events statt Polling).
* **Parallelisierung**: Wo angebracht, kann der Agent den Einsatz von Unity Jobs oder Burst vorschlagen (z. B. für massenhafte Berechnungen in der Generierung). Falls aber das Projekt nicht darauf ausgelegt ist, wird dies nur als Hinweis notiert, nicht zwingend implementiert.
* **Editor-Scripting für Build**: Der Agent nutzt Codex, um die Unity Editor BuildPipeline korrekt anzusteuern. Dabei achtet er auf häufige Fallen (z. B. dass Scenes in BuildSettings gesetzt sein müssen, dass Pfade existieren). Er testet die geschriebenen Build-Skripte im Editor und fängt etwaige Exceptions ab (z. B. fehlende Berechtigung, volles Laufwerk, usw.).
* **Integrationsskripte**: Für CI kann der Agent Templates nutzen (z. B. eine YAML-Vorlage von Unity CI) und passt Repository-spezifische Parameter an. Er dokumentiert jeden Schritt gut, damit Entwickler Vertrauen in den automatischen Prozess fassen.
* **Validierungs-Checks**: Der Agent implementiert kleine Checks, die vor einem Build laufen, wie z. B. sicherzustellen, dass keine Development-Settings aktiv sind (Debug-Modus, Profiler attached) im Release-Build, oder dass die Versionnummer inkrementiert wurde. Diese helfen, menschliche Fehler vor Release zu minimieren.

Am Ende dieser Phase sollten alle Kernsysteme stabil und optimiert sein. Der Agent aktualisiert **README.md** (Performance-Metriken, Systemanforderungen) mit den neuesten Messwerten und fügt ggf. eine *FINAL\_SUCCESS\_COMPLETE.md* Dokumentation hinzu, die den Abschluss des Projekts bestätigt (inklusive aller implementierten Features bis Phase 6).

---

📌 **Letzte Aktualisierung:** `{{TODAY}}`
