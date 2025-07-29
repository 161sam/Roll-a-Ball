### TODO-Index für Codeoptimierung

| ID | Datei | Ort (Zeile/Funktion) | Beschreibung |
| --- | --- | --- | --- |
| TODO-OPT#1 | Assets/Scripts/Generators/LevelGenerator.cs | CreateGroundTile(), Zeile 1350 | Redundante Materialauswahl mit CreateWallTile | **erledigt** |
| TODO-OPT#2 | Assets/Scripts/Generators/LevelGenerator.cs | InstantiateLevelObjects(), Zeile 1309 | Wiederholte Prefab-Existenzprüfung |
| TODO-OPT#3 | Assets/Scripts/Generators/LevelGenerator.cs | SetupPlayerSpawn(), Zeile 1651 | Duplizierter Velocity-Reset | **erledigt** |
| TODO-OPT#4 | Assets/Scripts/Generators/LevelGenerator.cs | Start(), Zeile 90 | Mehrfache Coroutine-Aufrufe konsolidieren | **erledigt** |
| TODO-OPT#5 | Assets/Scripts/Generators/LevelProfile.cs | OnValidate(), Zeile 309 | Mehrfaches Clampen zusammenfassen | **erledigt** |
| TODO-OPT#6 | Assets/Scripts/Generators/LevelProfile.cs | CreateScaledProfile(), Zeile 218 | Skalierungslogik vereinheitlichen | **erledigt** |
| TODO-OPT#7 | Assets/Scripts/LevelManager.cs | SubscribeToCollectibleEvents(), Zeile 212 | Event-Registrierung auslagern | **erledigt** |
| TODO-OPT#8 | Assets/Scripts/LevelManager.cs | DetermineNextScene(), Zeile 384 | Szenenreihenfolge konfigurierbar machen |
| TODO-OPT#9 | Assets/Scripts/GameManager.cs | ResetGame(), Zeile 322 | Velocity-Reset in Hilfsmethode verlagern | **erledigt** |
| TODO-OPT#10 | Assets/Scripts/GameManager.cs | HandleInput(), Zeile 174 | Zentrale Eingabeverwaltung einrichten |
| TODO-OPT#11 | Assets/Scripts/EmergencySceneBuilder.cs | BuildMinimalGeneratedLevel(), Zeile 105 | Wiederholte Szenen-Setup-Schritte zusammenfassen |
| TODO-OPT#12 | Assets/Scripts/Map/OSMUIConnector.cs | SetupMapController(), Zeile 109 | Find-or-Create Muster zentralisieren |
| TODO-OPT#13 | Assets/Scripts/SceneConsolidationEngine.cs | RepairLevel1(), Zeile 251 | GetOrCreate-Helfer für Manager nutzen |
| TODO-OPT#14 | Assets/Scripts/AutoSceneRepair.cs | Zeile 171 | EnsureComponent<T>()-Methode einführen |
| TODO-OPT#15 | Assets/Scripts/AutoSceneSetup.cs | Zeile 94 | AddIfMissing<T>() für Komponenten verwenden |
| TODO-OPT#16 | Assets/Scripts/PlayerController.cs | HandleInput(), Zeile 160 | Zentrale Inputverwaltung nutzen |
| TODO-OPT#17 | Assets/Scripts/UIController.cs | ShowMainMenu(), Zeile 288 | Show/Hide-Methoden vereinheitlichen |
| TODO-OPT#18 | Assets/Scripts/Generators/LevelSetupHelper.cs | CreateLevelProfile(), Zeile 144 | Reflection-Assignments via Dictionary bündeln |
| TODO-OPT#19 | Assets/Scripts/Generators/LevelProfileCreator.cs | CreateEasyProfile(), Zeile 34 | Mehrfaches SetPrivateField vereinheitlichen |
| TODO-OPT#20 | Assets/Scripts/PlayerController.cs | ResetBall(), Zeile 448 | Velocity-Zurücksetzung in Hilfsmethode bündeln | **erledigt** |
| TODO-OPT#21 | Assets/Scripts/Map/MapGenerator.cs | GenerateCollectiblePositions(), Zeile 499 | Platzierungslogik für Collectibles/Goal vereinheitlichen |
| TODO-OPT#22 | Assets/Scripts/Map/MapGenerator_Original.cs | PlaceGoalZone(), Zeile 305 | Gemeinsame Platzierungsfunktion nutzen |
| TODO-OPT#23 | Assets/Scripts/Map/MapStartupController.cs | InitializeUI(), Zeile 52 | Find-or-create Logik mit OSMUIConnector teilen |
| TODO-OPT#24 | Assets/Scripts/Map/OSMSceneCompleter.cs | SetupUIComponents(), Zeile 67 | UI-Setup-Methoden mit generischen Buildern vereinheitlichen |
| TODO-OPT#25 | Assets/Editor/ProjectCleanupAndFix.cs | CreateProperLevelProfiles(), Zeile 100 | Wiederholte Folder-Checks in Hilfsmethode auslagern |
| TODO-OPT#26 | Assets/Scripts/LevelManager.cs | OnDestroy(), Zeile 116 | Events vor Zerstörung abmelden |
| TODO-OPT#27 | Assets/Scripts/Map/MapGenerator.cs | CreateSteamEmitter(), Zeile 1565 | SteamEmitter-Pooling einführen |
| TODO-OPT#28 | Assets/Scripts/PlayerController.cs | CheckGrounded(), Zeile 206 | LayerMask-Abfrage cachen oder CharacterController nutzen |
| TODO-OPT#29 | Assets/Scripts/PlayerController.cs | SlideRoutine(), Zeile 427 | Slide-Impulse konfigurierbar machen |
| TODO-OPT#30 | Assets/Scripts/OSMGoalZoneTrigger.cs | SetupGoalZone(), Zeile 40 | Fallback bei fehlendem LevelManager einbauen |
| TODO-OPT#31 | Assets/Scripts/Map/MapStartupController.cs | GetCoordsFromAddress(), Zeile 403 | Geocoding-Service integrieren |
| TODO-OPT#32 | Assets/Scripts/Map/MapGeneratorBatched.cs | CreateSeparateColliders(), Zeile 548 | Collider-Pooling zur GC-Reduktion |
| TODO-OPT#33 | Assets/Scripts/SaveSystem.cs | SaveEncrypted()/SaveUnencrypted(), Zeile 360/348 | Async File IO verwenden |
| TODO-OPT#34 | Assets/Scripts/AchievementSystem.cs | OnLevelCompleted(), Zeile 332 | Levelnamen nicht per String vergleichen |
| TODO-OPT#35 | Assets/Scripts/AchievementSystem.cs | DisplayNotificationCoroutine(), Zeile 558 | Notification-Objekte poolen |
| TODO-OPT#36 | Assets/Scripts/AudioManager.cs | GetAvailableSource(), Zeile 359 | Pool dynamisch vergrößern |
| TODO-OPT#37 | Assets/Scripts/EmergencySceneBuilder.cs | BuildAllMinimalScenes(), Zeile 87 | Ursprüngliche Szene wiederherstellen |
| TODO-OPT#38 | Assets/Scripts/CollectibleController.cs | FlashLight(), Zeile 251 | Blitz-Parameter als Felder exposen |
| TODO-OPT#39 | Assets/Scripts/CollectibleController.cs | ResetCollectible(), Zeile 359 | Collectible in Pool zurücklegen |
| TODO-OPT#40 | Assets/Scripts/SaveSystem.cs | encryptionKey, Zeile 84 | Schlüssel aus externer Konfiguration laden |
| TODO-OPT#41 | Assets/Scripts/Map/MapStartupController.cs | LoadMapFromAddress(), Zeile 244 | Adressauflösung asynchron ausführen |
| TODO-OPT#42 | Assets/Scripts/ProgressionManager.cs | CreateDefaultLevels(), Zeile 203 | Leveldaten extern speichern |

