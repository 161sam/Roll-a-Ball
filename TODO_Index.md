### TODO-Index für Codeoptimierung

| ID | Datei | Ort (Zeile/Funktion) | Beschreibung | Status | 
| --- | --- | --- | --- | --- |
| TODO-OPT#1 | Assets/Scripts/Generators/LevelGenerator.cs | CreateGroundTile(), Zeile 1350 | Redundante Materialauswahl mit CreateWallTile | **erledigt** |
| TODO-OPT#2 | Assets/Scripts/Generators/LevelGenerator.cs | InstantiateLevelObjects(), Zeile 1309 | Wiederholte Prefab-Existenzprüfung | **erledigt** |
| TODO-OPT#3 | Assets/Scripts/Generators/LevelGenerator.cs | SetupPlayerSpawn(), Zeile 1651 | Duplizierter Velocity-Reset | **erledigt** |
| TODO-OPT#4 | Assets/Scripts/Generators/LevelGenerator.cs | Start(), Zeile 90 | Mehrfache Coroutine-Aufrufe konsolidieren | **erledigt** |
| TODO-OPT#5 | Assets/Scripts/Generators/LevelProfile.cs | OnValidate(), Zeile 309 | Mehrfaches Clampen zusammenfassen | **erledigt** |
| TODO-OPT#6 | Assets/Scripts/Generators/LevelProfile.cs | CreateScaledProfile(), Zeile 218 | Skalierungslogik vereinheitlichen | **erledigt** |
| TODO-OPT#7 | Assets/Scripts/LevelManager.cs | SubscribeToCollectibleEvents(), Zeile 212 | Event-Registrierung auslagern | **erledigt** |
| TODO-OPT#8 | Assets/Scripts/LevelManager.cs | DetermineNextScene(), Zeile 384 | Szenenreihenfolge konfigurierbar machen | **erledigt** |
| TODO-OPT#9 | Assets/Scripts/GameManager.cs | ResetGame(), Zeile 322 | Velocity-Reset in Hilfsmethode verlagern | **erledigt** |
| TODO-OPT#10 | Assets/Scripts/GameManager.cs | HandleInput(), Zeile 174 | Zentrale Eingabeverwaltung einrichten | **erledigt** |
| TODO-OPT#11 | Assets/Scripts/EmergencySceneBuilder.cs | BuildMinimalGeneratedLevel(), Zeile 105 | Wiederholte Szenen-Setup-Schritte zusammenfassen | **erledigt** |
| TODO-OPT#12 | Assets/Scripts/Map/OSMUIConnector.cs | SetupMapController(), Zeile 109 | Find-or-Create Muster zentralisieren | **erledigt** |
| TODO-OPT#13 | Assets/Scripts/SceneConsolidationEngine.cs | RepairLevel1(), Zeile 251 | GetOrCreate-Helfer für Manager nutzen | **erledigt** |
| TODO-OPT#14 | Assets/Scripts/AutoSceneRepair.cs | Zeile 171 | EnsureComponent<T>()-Methode einführen |
| TODO-OPT#15 | Assets/Scripts/AutoSceneSetup.cs | Zeile 94 | AddIfMissing<T>() für Komponenten verwenden |
| TODO-OPT#16 | Assets/Scripts/PlayerController.cs | HandleInput(), Zeile 160 | Zentrale Inputverwaltung nutzen | **erledigt** |
| TODO-OPT#17 | Assets/Scripts/UIController.cs | ShowMainMenu(), Zeile 288 | Show/Hide-Methoden vereinheitlichen | **erledigt** |
| TODO-OPT#18 | Assets/Scripts/Generators/LevelSetupHelper.cs | CreateLevelProfile(), Zeile 144 | Reflection-Assignments via Dictionary bündeln | **erledigt** |
| TODO-OPT#19 | Assets/Scripts/Generators/LevelProfileCreator.cs | CreateEasyProfile(), Zeile 34 | Mehrfaches SetPrivateField vereinheitlichen | **erledigt** |
| TODO-OPT#20 | Assets/Scripts/PlayerController.cs | ResetBall(), Zeile 448 | Velocity-Zurücksetzung in Hilfsmethode bündeln | **erledigt** |
| TODO-OPT#21 | Assets/Scripts/Map/MapGenerator.cs | GenerateCollectiblePositions(), Zeile 499 | Platzierungslogik für Collectibles/Goal vereinheitlichen | **erledigt** |
| TODO-OPT#22 | Assets/Scripts/Map/MapGenerator_Original.cs | PlaceGoalZone(), Zeile 305 | Gemeinsame Platzierungsfunktion nutzen |
| TODO-OPT#23 | Assets/Scripts/Map/MapStartupController.cs | InitializeUI(), Zeile 52 | Find-or-create Logik mit OSMUIConnector teilen | **erledigt** |
| TODO-OPT#24 | Assets/Scripts/Map/OSMSceneCompleter.cs | SetupUIComponents(), Zeile 67 | UI-Setup-Methoden mit generischen Buildern vereinheitlichen | **erledigt** |
| TODO-OPT#25 | Assets/Editor/ProjectCleanupAndFix.cs | CreateProperLevelProfiles(), Zeile 100 | Wiederholte Folder-Checks in Hilfsmethode auslagern | **erledigt** |
| TODO-OPT#26 | Assets/Scripts/LevelManager.cs | OnDestroy(), Zeile 116 | Events vor Zerstörung abmelden | **erledigt** |
| TODO-OPT#27 | Assets/Scripts/Map/MapGenerator.cs | CreateSteamEmitter(), Zeile 1565 | SteamEmitter-Pooling einführen | **erledigt** |
| TODO-OPT#28 | Assets/Scripts/PlayerController.cs | CheckGrounded(), Zeile 206 | LayerMask-Abfrage cachen oder CharacterController nutzen | **erledigt** |
| TODO-OPT#29 | Assets/Scripts/PlayerController.cs | SlideRoutine(), Zeile 427 | Slide-Impulse konfigurierbar machen | **erledigt** |
| TODO-OPT#30 | Assets/Scripts/OSMGoalZoneTrigger.cs | SetupGoalZone(), Zeile 40 | Fallback bei fehlendem LevelManager einbauen | **erledigt** |
| TODO-OPT#31 | Assets/Scripts/Map/MapStartupController.cs | GetCoordsFromAddress(), Zeile 403 | Geocoding-Service integrieren | **erledigt** |
| TODO-OPT#32 | Assets/Scripts/Map/MapGeneratorBatched.cs | CreateSeparateColliders(), Zeile 548 | Collider-Pooling zur GC-Reduktion | **erledigt** |
| TODO-OPT#33 | Assets/Scripts/SaveSystem.cs | SaveEncrypted()/SaveUnencrypted(), Zeile 360/348 | Async File IO verwenden | **erledigt** |
| TODO-OPT#34 | Assets/Scripts/AchievementSystem.cs | OnLevelCompleted(), Zeile 332 | Levelnamen nicht per String vergleichen | **erledigt** |
| TODO-OPT#35 | Assets/Scripts/AchievementSystem.cs | DisplayNotificationCoroutine(), Zeile 558 | Notification-Objekte poolen | **erledigt** |
| TODO-OPT#36 | Assets/Scripts/AudioManager.cs | GetAvailableSource(), Zeile 359 | Pool dynamisch vergrößern | **erledigt** |
| TODO-OPT#37 | Assets/Scripts/EmergencySceneBuilder.cs | BuildAllMinimalScenes(), Zeile 87 | Ursprüngliche Szene wiederherstellen | **erledigt** |
| TODO-OPT#38 | Assets/Scripts/CollectibleController.cs | FlashLight(), Zeile 251 | Blitz-Parameter als Felder exposen | **erledigt** |
| TODO-OPT#39 | Assets/Scripts/CollectibleController.cs | ResetCollectible(), Zeile 359 | Collectible in Pool zurücklegen | **erledigt** |
| TODO-OPT#40 | Assets/Scripts/SaveSystem.cs | encryptionKey, Zeile 84 | Schlüssel aus externer Konfiguration laden | **erledigt** |
| TODO-OPT#41 | Assets/Scripts/Map/MapStartupController.cs | LoadMapFromAddress(), Zeile 244 | Adressauflösung asynchron ausführen | **erledigt** |
| TODO-OPT#42 | Assets/Scripts/ProgressionManager.cs | CreateDefaultLevels(), Zeile 203 | Leveldaten extern speichern | **erledigt** |
| TODO-OPT#43 | Assets/Scripts/AchievementSystem.cs | CreateDefaultAchievements(), Zeile 186 | Achievements aus externer Konfiguration laden | **erledigt** |
| TODO-OPT#44 | Assets/Scripts/AchievementSystem.cs | SubscribeToGameEvents(), Zeile 295 | PlayerController-Referenz cachen | **erledigt** |
| TODO-OPT#45 | Assets/Scripts/AchievementSystem.cs | OnDestroy(), Zeile 720 | Von GameEvents abmelden | **erledigt** |
| TODO-OPT#46 | Assets/Scripts/GameManager.cs | pauseKey, Zeile 32 | Pause-Taste im Einstellungsmenü konfigurierbar machen | **erledigt** |
| TODO-OPT#47 | Assets/Scripts/GameManager.cs | TrackStatistics(), Zeile 406 | Update-Intervall einstellbar machen | **erledigt** |
| TODO-OPT#48 | Assets/Scripts/Map/MapGenerator.cs | GenerateCollectiblePositions(), Zeile 708 | Offsetbereich als Felder exposen | **erledigt** |
| TODO-OPT#49 | Assets/Scripts/Map/MapGenerator.cs | FindOptimalGoalPosition(), Zeile 736 | Pfadfindung zur Zielplatzierung nutzen | **erledigt** |
| TODO-OPT#50 | Assets/Scripts/Map/MapStartupController.cs | endlessModeAddresses, Zeile 41 | Adressliste extern speichern | **erledigt** |
| TODO-OPT#51 | Assets/Scripts/Map/AddressResolver.cs | ResolveAddressCoroutine(), Zeile 105 | Geocode-Cache implementieren | **erledigt** |
| TODO-OPT#52 | Assets/Scripts/UIController.cs | UpdateMainMenuUI(), Zeile 313 | UI-Texte lokalisieren | **erledigt** |
| TODO-OPT#53 | Assets/Scripts/UIController.cs | ShowNotificationCoroutine(), Zeile 820 | Notification-Pool verwenden | **erledigt** |
| TODO-OPT#54 | Assets/Scripts/Map/MapGeneratorBatched.cs | InitializeBatchingCollections(), Zeile 114 | String-Keys durch Enum ersetzen | **erledigt** |
| TODO-OPT#55 | Assets/Scripts/Environment/SteampunkGateController.cs | InitializeGate(), Zeile 122 | Manager-Referenzen per Inspector setzen | **erledigt** |
| TODO-OPT#56 | Assets/Scripts/Environment/SteampunkGateController.cs | Zeile 665 | OnDestroy zur Coroutine-Aufräumung einführen | **erledigt** |
| TODO-OPT#57 | Assets/Scripts/Environment/MovingPlatform.cs | BounceEaseOut(), Zeile 241 | Magic Numbers durch Konstanten ersetzen | **erledigt** |
| TODO-OPT#58 | Assets/Scripts/Environment/MovingPlatform.cs | OnTriggerEnter(), Zeile 349 | CharacterController-Unterstützung prüfen | **erledigt** |
| TODO-OPT#59 | Assets/Scripts/Map/MapStartupController.cs | CreateMinimalLevel(), Zeile 510 | Fallback-Level als Prefab realisieren | **erledigt** |
| TODO-OPT#60 | Assets/Scripts/PlayerController.cs | OnDestroy(), Zeile 528 | Von Events abmelden | **erledigt** |
| TODO-OPT#61 | Assets/Scripts/Generators/LevelGenerator.cs | ApplyMaterialsAndEffects(), Zeile 1461 | Steam-Emitter pooling umsetzen | **erledigt** |
| TODO-OPT#62 | Assets/Scripts/Map/MapGenerator.cs | AddSteampunkAtmosphere(), Zeile 1590 | Nebel-Parameter konfigurierbar machen | **erledigt** |
| TODO-OPT#63 | Assets/Scripts/UIController.cs | Zeile 983 | OnDestroy zum Deregistrieren der Events schreiben | **erledigt** |
| TODO-OPT#64 | Assets/Scripts/PlayerController.cs | HandleInput(), Zeile 174 | Legacy Input System durch New Input System ersetzen | **erledigt** |
| TODO-OPT#65 | Assets/Scripts/AudioManager.cs | OnEnable(), Zeile 420 | PlayerController-Referenz cachen | **erledigt** |
| TODO-OPT#66 | Assets/Scripts/VFX/RotatingGear.cs | Start(), Zeile 27 | Range für Rotationsvarianz exponieren | **erledigt** |
| TODO-OPT#67 | Assets/Scripts/VFX/SteamEmitter.cs | RandomizeSettings(), Zeile 249 | Zufallsbereiche per Inspector konfigurierbar machen | **erledigt** |
| TODO-OPT#68 | Assets/Scripts/SceneTypeDetector.cs | Zeile 18 | Szenenlisten aus Konfiguration laden | **erledigt** |
| TODO-OPT#69 | Assets/Scripts/CollectibleDiagnosticTool.cs | FindAllCollectibles(), Zeile 94 | Suchergebnisse cachen, um Allokationen zu vermeiden | **erledigt** |
| TODO-OPT#70 | Assets/Scripts/Map/MapStartupController.cs | leipzigCoords, Zeile 53 | Fallback-Koordinaten im Inspector einstellbar machen | **erledigt** |
| TODO-OPT#71 | Assets/Scripts/Environment/GateController.cs | Awake(), Zeile 26 | Null-check warning for gateObject in OnValidate | **erledigt** |
| TODO-OPT#72 | Assets/Scripts/Environment/GateController.cs | TriggerOpen(), Zeile 69 | Provide TriggerClose logic for reversible puzzles | **erledigt** |
| TODO-OPT#73 | Assets/Scripts/Environment/GateController.cs | TriggerOpen(), Zeile 70 | Fire gate opened event for other systems | **erledigt** |
| TODO-OPT#74 | Assets/Scripts/Environment/GroundMaterialController.cs | Zeile 19 | Material paths via ScriptableObject | **erledigt** |
| TODO-OPT#75 | Assets/Scripts/Environment/GroundMaterialController.cs | InitializeMaterialSystem(), Zeile 74 | Validate material sources in OnValidate | **erledigt** |
| TODO-OPT#76 | Assets/Scripts/Environment/GroundMaterialController.cs | ApplyMaterialToObject(), Zeile 313 | Cache renderer references | **erledigt** |
| TODO-OPT#77 | Assets/Scripts/Environment/GroundMaterialController.cs | OnDrawGizmosSelected(), Zeile 417 | Disable gizmo drawing in production | **erledigt** |
| TODO-OPT#78 | Assets/Scripts/Environment/MovingPlatform.cs | Zeile 55 | Init from ScriptableObject profile | **erledigt** |
| TODO-OPT#79 | Assets/Scripts/Environment/MovingPlatform.cs | OnValidate(), Zeile 352 | Warn if start and end positions match | **erledigt** |
| TODO-OPT#80 | Assets/Scripts/Environment/MovingPlatform.cs | OnTriggerEnter(), Zeile 365 | Configurable passenger tag | **erledigt** |
| TODO-OPT#81 | Assets/Scripts/Environment/RotatingObstacle.cs | Zeile 14 | Tune rotation via ScriptableObject | **erledigt** |
| TODO-OPT#82 | Assets/Scripts/Environment/RotatingObstacle.cs | OnValidate(), Zeile 32 | Expose min/max rotation speed | **erledigt** |
| TODO-OPT#83 | Assets/Scripts/Environment/RotatingObstacle.cs | OnTriggerEnter(), Zeile 56 | Integrate damage system on contact | **erledigt** |
| TODO-OPT#84 | Assets/Scripts/Environment/SteampunkGateController.cs | Zeile 80 | Replace FindFirstObjectByType with injection | **erledigt** |
| TODO-OPT#85 | Assets/Scripts/Environment/SteampunkGateController.cs | Start(), Zeile 96 | Delay initialization until managers ready | **erledigt** |
| TODO-OPT#86 | Assets/Scripts/Environment/SteampunkGateController.cs | CheckSequentialRequirement(), Zeile 303 | Dedicated sequence controller | **erledigt** |
| TODO-OPT#87 | Assets/Scripts/Environment/SteampunkGateController.cs | OnDestroy(), Zeile 678 | Deregister from global events | reviewed |
| TODO-OPT#88 | Assets/Scripts/Environment/SwitchTrigger.cs | Zeile 17 | Expose activation requirements | **erledigt** |
| TODO-OPT#89 | Assets/Scripts/Environment/SwitchTrigger.cs | Awake(), Zeile 32 | Warn if gate missing | **erledigt** |
| TODO-OPT#90 | Assets/Scripts/Environment/SwitchTrigger.cs | ActivateSwitch(), Zeile 83 | Reset/deactivation logic | **erledigt** |
| TODO-OPT#91 | Assets/Scripts/Environment/SwitchTrigger.cs | OnValidate(), Zeile 93 | Highlight object when gate missing | **erledigt** |
| TODO-OPT#92 | Assets/Scripts/Map/MapStartupController.cs | ClearExistingLevel(), Zeile 758 | Replace destructive cleanup with pooled map objects |
| TODO-OPT#93 | Assets/Scripts/Map/MapStartupController.cs | HideMapUI(), Zeile 789 | Ensure UI references are serialized rather than searched |
| TODO-OPT#94 | Assets/Scripts/Map/AddressResolver.cs | ParseOSMResponse(), Zeile 567 | Make map scale configurable via ScriptableObject |
| TODO-OPT#95 | Assets/Scripts/Generators/LevelTerrainGenerator.cs | GeneratePrimMaze(), Zeile 358 | Evaluate Prim algorithm performance for large level sizes |
