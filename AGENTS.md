wiki/docs/development/AGENTS-INDEX.md stellt eine AGENTEN-ÜBERSICHT bereit.

// Lokales Arbeitsverzeichnis
WORKING DIR = /home/saschi/Games/Roll-a-Ball 
default_user = Sam Schimmelpfennig/saschi161
Github_User = 161sam


Follow these steps for each interaction:

1. User Identification:
   - You should assume that you are interacting with default_user
   - If you have not identified default_user, proactively try to do so.

2. Memory Retrieval:
   - Always begin your chat by saying only "Remembering..." and retrieve all relevant information from your knowledge graph
   - Always refer to your knowledge graph as your "memory"

3. Memory
   - While conversing with the user, be attentive to any new information that falls into these categories:
     a) Basic Identity (age, gender, location, job title, education level, etc.)
     b) Behaviors (interests, habits, etc.)
     c) Preferences (communication style, preferred language, etc.)
     d) Goals (goals, targets, aspirations, etc.)
     e) Relationships (personal and professional relationships up to 3 degrees of separation)

4. Memory Update:
   - If any new information was gathered during the interaction, update your memory as follows:
     a) Create entities for recurring organizations, people, and significant events
     b) Connect them to the current entities using relations
     c) Store facts about them as observations

---

Ensure that all files and functions are named consistently and clearly. Terms such as “enhanced”, “advanced”, “improved”, “better”, “pro”, ‘v2’, “ultimate” or similar should be avoided; instead, precise and meaningful names should be chosen. Before creating new files, check whether existing code can be improved or extended in order to avoid redundancies. If it is necessary to create new files, replaced or obsolete files are archived centrally and traceably.

---

Use ALL MCP servers and tools available to you.

---

Never say "Ready for production deployment!" or similar, we are stil in development!

---

Links for OSM integration:
https://wiki.openstreetmap.org/wiki/Downloading_data
https://wiki.openstreetmap.org/wiki/API_v0.6#Retrieving_map_data_by_bounding_box:_GET_/api/0.6/map
https://wiki.openstreetmap.org/wiki/API_v0.6

---

Coding Guidelines

Architecture & Modularity

Keep components loosely coupled. Expose dependencies via serialized fields or constructor injection where possible.

Organize code by domain using namespaces such as RollABall.Environment, RollABall.Map, etc. Current scripts without namespaces (e.g. GameManager, PlayerController) should be wrapped accordingly.

Favor ScriptableObjects for shared configuration data (e.g. LevelProfile) and store them under Assets/ScriptableObjects/.

Use events (System.Action or UnityEvent) for cross-component communication. Remove direct scene references wherever possible.

Ensure every reusable object (ground, wall, collectibles, gates, etc.) has an up‑to‑date Prefab located in Assets/Prefabs/ and instantiate these Prefabs at runtime or in scenes.

Naming & Structure

Use PascalCase for public types and members, camelCase for private fields as noted in the README.

Match file names with their contained class names.

Folder layout should follow the structure presented in the README.

Scene files belong in Assets/Scenes/ with optional subfolders for tests or backups. Prefabs go to Assets/Prefabs/ organized by feature.

Syntax & Best Practices

Prefer modern C# syntax: expression‑bodied members and auto‑properties where practical.

Avoid code duplication (many TODO‑OPT items target redundant code). Refactor recurring logic into helper methods.

Use AddComponentMenu, Header and Tooltip attributes for inspector clarity.

Keep OnValidate methods to enforce valid serialized values (see LevelProfile for example).

When possible, use asynchronous operations (async/await) for network or file I/O instead of blocking coroutines (see TODO‑OPT#33).

Documentation & Comments

Provide XML documentation for all public APIs and describe complex logic with inline comments.

Update wiki pages and README when code changes invalidate current documentation to avoid mismatches. Some wiki reports claim finished systems that do not exist—avoid such discrepancies.

Include short usage instructions in script headers when a component requires a specific setup.

Testing & Validation

Implement automated tests where feasible (e.g. Editor tests for map generation or object pooling).

Utilize OnValidate and custom editor scripts to catch missing references at edit time.

Before submitting a PR, run play‑tests for every affected scene and document the result.

Prefabs as Standard

Every object type used more than once must have a Prefab. Scenes should only contain Prefab instances, not manually duplicated objects. Missing Prefab references should be treated as a bug.

Prefab names follow PascalCase and are stored in Assets/Prefabs/FeatureName/.

When modifying a Prefab, update its version number in the comment block or via a versioning script to track changes.

Collaboration Workflow

Create feature branches from main (e.g. feature/xyz) and keep commits concise with clear messages, as recommended in the README’s Pull Request guidelines.

Each PR must include a summary of changes, mention which TODO‑OPT items are addressed (if any), and update relevant documentation.

Reviewers verify that new scripts follow namespace conventions, include XML docs, and use Prefabs consistently.

Future Improvements

Address the open items in TODO_Index.md, prioritizing core gameplay fixes and modularizing input handling.

Complete the OSM parser so that real map data replaces placeholders, as outlined in the “Analyse OSM‑Kartengenerierung” document.

Reassess “FINAL_*” wiki documents once implementations truly match the described status.

Following these guidelines will keep the project maintainable and ensure new features integrate smoothly with existing systems.

---

🌍 Roll-a-Ball – Entwicklungsplan

📌 Überblick

Ein 3D-Spiel im Steampunk-Stil, bei dem der Spieler eine Kugel steuert, um Objekte einzusammeln und Levelziele zu erreichen. Später sollen reale Kartendaten via OpenStreetMap genutzt werden, um das Startlevel automatisch anhand einer eingegebenen Adresse zu generieren.


---

📅 Entwicklungsphasen

🔹 Phase 1: Grundlagen & Setup ✅ (abgeschlossen)

Unity 6.1, Blender, Claude Desktop + MCP eingerichtet

Roll-a-Ball Template implementiert (Spieler, Kamera, Input System)

MVP-Testszene mit PlayerController, KameraFollow und Physik erstellt

Projektstruktur mit /Assets/Scenes, /Assets/Scripts, /Assets/Prefabs definiert


🔹 Phase 2: 3-Level-Struktur mit Steampunk-Thema ✅ (abgeschlossen)

🧩 Level1–3 mit zunehmender Schwierigkeit

🧱 Prefabs: Boden, Wand, Sammelobjekte, Zielzone

🎮 GameManager, UIController, CollectibleController, LevelManager

🎨 Steampunk-Materialien: Metall, Kupfer, Emissive Collectibles

🖥️ UI: Collectible-Counter, Fly-Energy-Bar

🔧 Best Practices (UnityEvents, AddComponentMenu, moderne APIs)


🔹 Phase 3: Prozedurale Levelgenerierung (in Umsetzung)

Ziel: Automatische Erstellung dynamischer Levels mit Gameplay-Elementen, basierend auf einem Level-Profil.

✅ 3.1: Generator-Grundstruktur (erledigt)

LevelGenerator.cs: Generiert modularen Boden, Wände, Collectibles, Zielzone

Parameter: Größe, Anzahl Collectibles, Zielposition, etc.


🔄 3.2: Erweiterung – Hindernisse & Dynamik (aktuell)

ObstaclePrefab: rotierende Zahnräder, bewegliche Steampunk-Elemente

MovingPlatform.cs + MovingPlatformPrefab

Partikelsysteme (SteamEmitter) mit Audio (Zischen)

Optional: Interaktive Tore via Trigger (Prefab GatePrefab)

Modular via LevelProfile.cs steuerbar (obstacleDensity etc.)


🔹 Phase 4: Map-basierte Generierung (OpenStreetMap)

Ziel: Spieler gibt Startadresse ein → Scene wird automatisch aus OSM-Daten generiert.

API-Zugriff auf OSM / Mapbox / TileServer

Umwandlung von Karten-Koordinaten in Unity-Terrain

Platzierung von Gebäuden, Straßen, Zielen, Collectibles nach Geo-Daten

Integration in LevelGenerator.cs mit MapDataAdapter.cs


🔹 Phase 5: Progression, UI & Save System

Fortschrittsverfolgung pro Spieler

Auswahlmenü für Level & Map

Speicherstände lokal sichern

Fly-Energie-Management mit Balken-UI


🔹 Phase 6: Multiplayer & soziale Funktionen (optional/später)

Kooperative oder kompetitive Spielmodi

Scoreboard mit lokalem Speicher oder Serveranbindung


🔹 Phase 7: Polishing & Deployment

Shader & Lighting pass für Steampunk-Flair

Optimierung für Performance (Batching, LOD, QualitySettings)

Build für WebGL & Desktop



---

📁 Projektstruktur (Best Practice)

Assets/
├── Prefabs/
│   ├── GroundPrefab
│   ├── WallPrefab
│   ├── CollectiblePrefab
│   ├── GoalZonePrefab
│   └── ObstaclePrefab, MovingPlatformPrefab
├── Scenes/
│   ├── Level1, Level2, Level3
├── Scripts/
│   ├── Core/: PlayerController, GameManager
│   ├── Level/: LevelGenerator, LevelManager, LevelProfile
│   ├── UI/: UIController
│   ├── Collectibles/: CollectibleController
│   └── Environment/: MovingPlatform.cs, GateController.cs
├── UI/
│   ├── Canvas, TextMeshPro, FlyBar
├── VFX/
│   └── SteamEmitter.prefab
├── Materials/
│   └── SteamGroundMaterial, SteamWallMaterial, Emissives
├── Audio/
│   └── steam_hiss.wav, metal_clank.wav

---
