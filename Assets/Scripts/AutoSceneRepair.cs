using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Automated Scene Repair Tool - Systematische Reparatur aller Roll-a-Ball Szenen
/// Führt die in der Analyse identifizierten Korrekturen automatisch durch
/// </summary>
public class AutoSceneRepair : EditorWindow
{
    private Vector2 scrollPosition;
    private bool repairInProgress = false;
    private List<string> repairLog = new List<string>();
    
    [MenuItem("Roll-a-Ball/Auto Scene Repair")]
    public static void ShowWindow()
    {
        GetWindow<AutoSceneRepair>("Auto Scene Repair");
    }

    void OnGUI()
    {
        GUILayout.Label("Unity-Szenen Automatische Reparatur", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("🚀 ALLE SZENEN REPARIEREN", GUILayout.Height(40)))
        {
            RepairAllScenes();
        }

        GUILayout.Space(10);
        
        // Einzelne Szenen-Reparatur
        GUILayout.Label("Einzelne Szenen:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Repariere GeneratedLevel.unity"))
            RepairScene("Assets/Scenes/GeneratedLevel.unity");
            
        if (GUILayout.Button("Repariere Level1.unity"))
            RepairScene("Assets/Scenes/Level1.unity");
            
        if (GUILayout.Button("Repariere Level2.unity"))
            RepairScene("Assets/Scenes/Level2.unity");
            
        if (GUILayout.Button("Repariere Level3.unity"))
            RepairScene("Assets/Scenes/Level3.unity");
            
        if (GUILayout.Button("Repariere Level_OSM.unity"))
            RepairScene("Assets/Scenes/Level_OSM.unity");
            
        if (GUILayout.Button("Repariere MiniGame.unity"))
            RepairScene("Assets/Scenes/MiniGame.unity");

        GUILayout.Space(10);
        
        // Progress & Log
        if (repairInProgress)
        {
            GUILayout.Label("Reparatur läuft...", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Bitte warten Sie, bis alle Reparaturen abgeschlossen sind.", MessageType.Info);
        }

        GUILayout.Label("Reparatur-Log:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        foreach (string logEntry in repairLog)
        {
            GUILayout.Label(logEntry);
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Log löschen"))
        {
            repairLog.Clear();
        }
    }

    private void RepairAllScenes()
    {
        repairInProgress = true;
        repairLog.Clear();
        
        string[] scenePaths = {
            "Assets/Scenes/GeneratedLevel.unity",
            "Assets/Scenes/Level1.unity", 
            "Assets/Scenes/Level2.unity",
            "Assets/Scenes/Level3.unity",
            "Assets/Scenes/Level_OSM.unity",
            "Assets/Scenes/MiniGame.unity"
        };

        LogMessage("🚀 Starte automatische Reparatur aller Szenen...");

        foreach (string scenePath in scenePaths)
        {
            RepairScene(scenePath);
        }

        LogMessage("✅ Alle Szenen-Reparaturen abgeschlossen!");
        repairInProgress = false;
    }

    private void RepairScene(string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            LogMessage($"❌ Szene nicht gefunden: {scenePath}");
            return;
        }

        LogMessage($"🔧 Repariere: {Path.GetFileNameWithoutExtension(scenePath)}");
        
        // Scene laden
        Scene currentScene = EditorSceneManager.OpenScene(scenePath);
        
        // Basis-Reparaturen durchführen
        PerformBasicRepairs();
        
        // Szenen-spezifische Reparaturen
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        switch (sceneName)
        {
            case "GeneratedLevel":
                RepairGeneratedLevel();
                break;
            case "Level1":
                RepairLevel1();
                break;
            case "Level2":
                RepairLevel2();
                break;
            case "Level3":
                RepairLevel3();
                break;
            case "Level_OSM":
                RepairLevelOSM();
                break;
            case "MiniGame":
                RepairMiniGame();
                break;
        }
        
        // Scene speichern
        EditorSceneManager.SaveScene(currentScene);
        LogMessage($"✅ {sceneName} repariert und gespeichert");
    }

    private void PerformBasicRepairs()
    {
        LogMessage("   🔧 Basis-Reparaturen...");
        
        // 1. UniversalSceneFixture hinzufügen falls nicht vorhanden
        if (FindFirstObjectByType<UniversalSceneFixture>() == null)
        {
            GameObject fixtureGO = new GameObject("UniversalSceneFixture");
            UniversalSceneFixture fixture = fixtureGO.AddComponent<UniversalSceneFixture>();
            LogMessage("   ✅ UniversalSceneFixture hinzugefügt");
        }

        // 2. GameManager prüfen/erstellen
        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
            LogMessage("   ✅ GameManager erstellt");
        }

        // 3. LevelManager prüfen/erstellen
        // TODO-OPT#14: Create common EnsureComponent<T>() to avoid repetition
        if (FindFirstObjectByType<LevelManager>() == null)
        {
            GameObject lmGO = new GameObject("LevelManager");
            lmGO.AddComponent<LevelManager>();
            LogMessage("   ✅ LevelManager erstellt");
        }

        // 4. UIController prüfen/erstellen
        if (FindFirstObjectByType<UIController>() == null)
        {
            GameObject uiGO = new GameObject("UIController");
            uiGO.AddComponent<UIController>();
            LogMessage("   ✅ UIController erstellt");
        }

        // 5. Canvas-System prüfen
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            CreateStandardUISystem();
        }
        else
        {
            RepairCanvasSystem(canvas);
        }

        // 6. EventSystem prüfen
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            LogMessage("   ✅ EventSystem erstellt");
        }
    }

    private void CreateStandardUISystem()
    {
        LogMessage("   🎨 Erstelle Standard-UI-System...");
        
        // Canvas erstellen
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        
        // CanvasScaler hinzufügen
        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // GraphicRaycaster hinzufügen
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Game UI Panel erstellen
        CreateGameUIPanel(canvasGO.transform);
        
        LogMessage("   ✅ Standard-UI-System erstellt");
    }

    private void CreateGameUIPanel(Transform canvasTransform)
    {
        // Game UI Panel
        GameObject panelGO = new GameObject("GameUIPanel");
        panelGO.transform.SetParent(canvasTransform);
        
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // Collectible Text (oben links)
        CreateUIText("CollectibleText", "Collectibles: 0/0", panelGO.transform, 
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20));

        // Fly Bar (oben rechts)
        CreateFlyBar(panelGO.transform);

        // Fly Text (unter Fly Bar)
        CreateUIText("FlyText", "FLYING", panelGO.transform,
                    new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -80));

        // Notification Text (center)
        CreateUIText("NotificationText", "", panelGO.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, false);
    }

    private void CreateUIText(string name, string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, bool active = true)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent);
        textGO.SetActive(active);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = new Vector2(200, 50);

        TMPro.TextMeshProUGUI textComponent = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 24;
        textComponent.color = Color.white;
        textComponent.fontStyle = TMPro.FontStyles.Bold;
    }

    private void CreateFlyBar(Transform parent)
    {
        GameObject sliderGO = new GameObject("FlyBar");
        sliderGO.transform.SetParent(parent);

        RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(1, 1);
        sliderRect.anchorMax = new Vector2(1, 1);
        sliderRect.anchoredPosition = new Vector2(-20, -20);
        sliderRect.sizeDelta = new Vector2(200, 20);

        UnityEngine.UI.Slider slider = sliderGO.AddComponent<UnityEngine.UI.Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform);
        UnityEngine.UI.Image bgImage = bg.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = Color.cyan;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
    }

    private void RepairCanvasSystem(Canvas canvas)
    {
        LogMessage("   🔧 Repariere Canvas-System...");
        
        // CanvasScaler prüfen/reparieren
        UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        }
        
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // GraphicRaycaster prüfen
        if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        LogMessage("   ✅ Canvas-System repariert");
    }

    // Szenen-spezifische Reparaturen
    private void RepairGeneratedLevel()
    {
        LogMessage("   🎮 GeneratedLevel-spezifische Reparaturen...");
        
        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator == null)
        {
            GameObject genGO = new GameObject("LevelGenerator");
            generator = genGO.AddComponent<LevelGenerator>();
            LogMessage("   ✅ LevelGenerator erstellt");
        }

        // Prefab-Referenzen prüfen und zuweisen
        AssignPrefabReferences(generator);
        
        // Level Profile zuweisen
        if (generator.ActiveProfile == null)
        {
            LevelProfile easyProfile = Resources.Load<LevelProfile>("EasyProfile");
            if (easyProfile != null)
            {
                // Da ActiveProfile ein Property ist, müssen wir Reflection verwenden
                var field = typeof(LevelGenerator).GetField("activeProfile", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(generator, easyProfile);
                    LogMessage("   ✅ EasyProfile zugewiesen");
                }
            }
        }

        // Container-Hierarchie erstellen
        CreateLevelContainers();
    }

    private void AssignPrefabReferences(LevelGenerator generator)
    {
        // Prefabs laden
        GameObject groundPrefab = Resources.Load<GameObject>("GroundPrefab");
        GameObject wallPrefab = Resources.Load<GameObject>("WallPrefab");
        GameObject collectiblePrefab = Resources.Load<GameObject>("CollectiblePrefab");
        GameObject goalZonePrefab = Resources.Load<GameObject>("GoalZonePrefab");
        GameObject playerPrefab = Resources.Load<GameObject>("Player");

        // Alternative: Pfade verwenden
        if (groundPrefab == null)
            groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GroundPrefab.prefab");
        if (wallPrefab == null)
            wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/WallPrefab.prefab");
        if (collectiblePrefab == null)
            collectiblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CollectiblePrefab.prefab");
        if (goalZonePrefab == null)
            goalZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GoalZonePrefab.prefab");
        if (playerPrefab == null)
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");

        // Reflection verwenden, um private Felder zu setzen
        var generatorType = typeof(LevelGenerator);
        
        SetPrivateField(generator, "groundPrefab", groundPrefab);
        SetPrivateField(generator, "wallPrefab", wallPrefab);
        SetPrivateField(generator, "collectiblePrefab", collectiblePrefab);
        SetPrivateField(generator, "goalZonePrefab", goalZonePrefab);
        SetPrivateField(generator, "playerPrefab", playerPrefab);

        LogMessage("   ✅ Prefab-Referenzen zugewiesen");
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && value != null)
        {
            field.SetValue(obj, value);
        }
    }

    private void CreateLevelContainers()
    {
        if (GameObject.Find("LevelContainer") == null)
        {
            GameObject levelContainer = new GameObject("LevelContainer");
            
            new GameObject("GroundContainer").transform.SetParent(levelContainer.transform);
            new GameObject("WallContainer").transform.SetParent(levelContainer.transform);
            new GameObject("CollectibleContainer").transform.SetParent(levelContainer.transform);
            new GameObject("EffectsContainer").transform.SetParent(levelContainer.transform);
            
            LogMessage("   ✅ Level-Container erstellt");
        }
    }

    private void RepairLevel1()
    {
        LogMessage("   📚 Level1-spezifische Reparaturen...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            // LevelConfiguration setzen
            var configField = typeof(LevelManager).GetField("levelConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                var config = configField.GetValue(levelManager) as LevelConfiguration;
                if (config == null)
                {
                    config = new LevelConfiguration();
                    configField.SetValue(levelManager, config);
                }
                
                config.levelName = "Level 1 - Tutorial";
                config.levelIndex = 1;
                config.totalCollectibles = 5;
                config.nextSceneName = "Level2";
                config.difficultyMultiplier = 1.0f;
                
                LogMessage("   ✅ Level1-Konfiguration gesetzt");
            }
        }
    }

    private void RepairLevel2()
    {
        LogMessage("   ⚙️ Level2-spezifische Reparaturen...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            var configField = typeof(LevelManager).GetField("levelConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                var config = configField.GetValue(levelManager) as LevelConfiguration;
                if (config == null)
                {
                    config = new LevelConfiguration();
                    configField.SetValue(levelManager, config);
                }
                
                config.levelName = "Level 2 - Steampunk Intro";
                config.levelIndex = 2;
                config.totalCollectibles = 8;
                config.nextSceneName = "Level3";
                config.difficultyMultiplier = 1.5f;
                
                LogMessage("   ✅ Level2-Konfiguration gesetzt");
            }
        }
    }

    private void RepairLevel3()
    {
        LogMessage("   🏭 Level3-spezifische Reparaturen...");
        
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            var configField = typeof(LevelManager).GetField("levelConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                var config = configField.GetValue(levelManager) as LevelConfiguration;
                if (config == null)
                {
                    config = new LevelConfiguration();
                    configField.SetValue(levelManager, config);
                }
                
                config.levelName = "Level 3 - Steampunk Master";
                config.levelIndex = 3;
                config.totalCollectibles = 12;
                config.nextSceneName = "GeneratedLevel";
                config.difficultyMultiplier = 2.0f;
                
                LogMessage("   ✅ Level3-Konfiguration gesetzt");
            }
        }
    }

    private void RepairLevelOSM()
    {
        LogMessage("   🗺️ Level_OSM-spezifische Reparaturen...");
        
        // OSM-spezifische Komponenten prüfen
        var mapController = FindFirstObjectByType<RollABall.Map.MapStartupController>();
        if (mapController == null)
        {
            GameObject mapGO = new GameObject("MapStartupController");
            mapController = mapGO.AddComponent<RollABall.Map.MapStartupController>();
            LogMessage("   ✅ MapStartupController erstellt");
        }
    }

    private void RepairMiniGame()
    {
        LogMessage("   🎲 MiniGame-spezifische Reparaturen...");
        
        // MiniGame-spezifische Logik hier
        LogMessage("   ℹ️ MiniGame-Design muss noch definiert werden");
    }

    private void LogMessage(string message)
    {
        repairLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        Debug.Log($"[AutoSceneRepair] {message}");
        Repaint(); // UI aktualisieren
    }
}
