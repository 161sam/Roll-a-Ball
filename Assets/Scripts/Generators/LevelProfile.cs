using UnityEngine;

/// <summary>
/// ScriptableObject zur Konfiguration von prozedural generierten Leveln
/// Definiert Schwierigkeit, Größe, Hindernisse und visuelle Aspekte
/// Erweitert um Steampunk-spezifische Features
/// </summary>
[CreateAssetMenu(fileName = "LevelProfile", menuName = "Roll-a-Ball/Level Profile", order = 1)]
[System.Serializable]
public class LevelProfile : ScriptableObject
{
    [Header("Level Identity")]
    [SerializeField] private string profileName = "New Level Profile";
    [SerializeField] private string displayName = "Level";
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private Color themeColor = Color.cyan;

    [Header("Level Dimensions")]
    [SerializeField] private int levelSize = 10;
    [SerializeField] private float tileSize = 2f;
    [SerializeField] private int minWalkableArea = 60; // Prozent der Gesamtfläche

    [Header("Collectibles")]
    [SerializeField] private int collectibleCount = 5;
    [SerializeField] private int minCollectibleDistance = 2; // Mindestabstand zwischen Collectibles
    [SerializeField] private float collectibleSpawnHeight = 0.5f;

    [Header("Obstacles & Hazards")]
    [SerializeField] private float obstacleDensity = 0.1f; // 0.0 = keine, 1.0 = maximal
    [SerializeField] private bool enableMovingObstacles = false;
    [SerializeField] private float movingObstacleChance = 0.05f;

    [Header("🔧 Steampunk-Hindernisse")]
    [SerializeField] private bool enableRotatingObstacles = true;
    [SerializeField] private float rotatingObstacleDensity = 0.08f;
    [SerializeField] private bool enableMovingPlatforms = true;
    [SerializeField] private float movingPlatformDensity = 0.05f;
    [SerializeField] private GameObject[] rotatingObstaclePrefabs;
    [SerializeField] private GameObject[] movingPlatformPrefabs;

    [Header("💨 Steam-Atmosphäre")]
    [SerializeField] private bool enableSteamEmitters = true;
    [SerializeField] private float steamEmitterDensity = 0.06f;
    [SerializeField] private GameObject[] steamEmitterPrefabs;
    [SerializeField] private SteamEmitterProfile steamSettings;

    [Header("🚪 Interaktive Elemente")]
    [SerializeField] private bool enableInteractiveGates = false;
    [SerializeField] private float interactiveGateDensity = 0.02f;
    [SerializeField] private GameObject[] interactiveGatePrefabs;

    [Header("Surface Properties")]
    [SerializeField] private float frictionVariance = 0.2f; // Variation der Oberflächenreibung
    [SerializeField] private bool enableSlipperyTiles = false;
    [SerializeField] private float slipperyTileChance = 0.1f;
    [SerializeField] private PhysicsMaterial slipperyMaterial;

    [Header("Visual & Effects")]
    [SerializeField] private Material[] groundMaterials;
    [SerializeField] private Material[] wallMaterials;
    [SerializeField] private Material goalZoneMaterial;
    [SerializeField] private bool enableParticleEffects = true;
    [SerializeField] private GameObject[] decorativeEffects;

    [Header("🎨 Steampunk-Thema")]
    [SerializeField] private SteampunkTheme steampunkTheme = SteampunkTheme.Industrial;
    [SerializeField] private Material[] steampunkGroundMaterials;
    [SerializeField] private Material[] steampunkWallMaterials;
    [SerializeField] private Color ambientLightColor = new Color(1f, 0.9f, 0.7f);
    [SerializeField] private float ambientLightIntensity = 1.2f;

    [Header("Spawn Configuration")]
    [SerializeField] private Vector3 playerSpawnOffset = Vector3.up;
    [SerializeField] private bool randomizeSpawnPosition = false;
    [SerializeField] private float spawnSafeRadius = 3f;

    [Header("Advanced Settings")]
    [SerializeField] private int generationSeed = 0; // 0 = zufällig
    [SerializeField] private bool useTimeBasedSeed = true;
    [SerializeField] private LevelGenerationMode generationMode = LevelGenerationMode.Maze;
    [SerializeField] private float pathComplexity = 0.5f; // 0 = einfache Wege, 1 = komplexe Labyrinthe
    [SerializeField] private bool useDynamicModeSelection = false;

    // Properties für externen Zugriff (Bestehende)
    public string ProfileName => profileName;
    public string DisplayName => displayName;
    public int DifficultyLevel => difficultyLevel;
    public Color ThemeColor => themeColor;
    public int LevelSize => levelSize;
    public float TileSize => tileSize;
    public int MinWalkableArea => minWalkableArea;
    public int CollectibleCount => collectibleCount;
    public int MinCollectibleDistance => minCollectibleDistance;
    public float CollectibleSpawnHeight => collectibleSpawnHeight;
    public float ObstacleDensity => obstacleDensity;
    public bool EnableMovingObstacles => enableMovingObstacles;
    public float MovingObstacleChance => movingObstacleChance;
    public float FrictionVariance => frictionVariance;
    public bool EnableSlipperyTiles => enableSlipperyTiles;
    public float SlipperyTileChance => slipperyTileChance;
    public PhysicsMaterial SlipperyMaterial => slipperyMaterial;
    public Material[] GroundMaterials => groundMaterials;
    public Material[] WallMaterials => wallMaterials;
    public Material GoalZoneMaterial => goalZoneMaterial;
    public bool EnableParticleEffects => enableParticleEffects;
    public GameObject[] DecorativeEffects => decorativeEffects;
    public Vector3 PlayerSpawnOffset => playerSpawnOffset;
    public bool RandomizeSpawnPosition => randomizeSpawnPosition;
    public float SpawnSafeRadius => spawnSafeRadius;
    public int GenerationSeed => generationSeed;
    public bool UseTimeBasedSeed => useTimeBasedSeed;
    public LevelGenerationMode GenerationMode => generationMode;
    public float PathComplexity => pathComplexity;
    public bool UseDynamicModeSelection => useDynamicModeSelection;

    // Neue Properties für Steampunk-Features
    public bool EnableRotatingObstacles => enableRotatingObstacles;
    public float RotatingObstacleDensity => rotatingObstacleDensity;
    public bool EnableMovingPlatforms => enableMovingPlatforms;
    public float MovingPlatformDensity => movingPlatformDensity;
    public GameObject[] RotatingObstaclePrefabs => rotatingObstaclePrefabs;
    public GameObject[] MovingPlatformPrefabs => movingPlatformPrefabs;
    public bool EnableSteamEmitters => enableSteamEmitters;
    public float SteamEmitterDensity => steamEmitterDensity;
    public GameObject[] SteamEmitterPrefabs => steamEmitterPrefabs;
    public SteamEmitterProfile SteamSettings => steamSettings;
    public bool EnableInteractiveGates => enableInteractiveGates;
    public float InteractiveGateDensity => interactiveGateDensity;
    public GameObject[] InteractiveGatePrefabs => interactiveGatePrefabs;
    public SteampunkTheme SteampunkTheme => steampunkTheme;
    public Material[] SteampunkGroundMaterials => steampunkGroundMaterials;
    public Material[] SteampunkWallMaterials => steampunkWallMaterials;
    public Color AmbientLightColor => ambientLightColor;
    public float AmbientLightIntensity => ambientLightIntensity;

    /// <summary>
    /// Berechnet den tatsächlichen Seed für die Levelgenerierung
    /// </summary>
    public int GetActualSeed()
    {
        if (generationSeed != 0)
            return generationSeed;

        if (useTimeBasedSeed)
            return System.DateTime.Now.GetHashCode();

        return Random.Range(int.MinValue, int.MaxValue);
    }

    // Dynamic generation mode selection
    public LevelGenerationMode GetAdaptiveGenerationMode(int seed)
    {
        if (!useDynamicModeSelection)
            return generationMode;

        System.Random rnd = new System.Random(seed);

        bool small = levelSize <= 10;
        bool large = levelSize >= 20;

        if (small)
            return rnd.NextDouble() < 0.5 ? LevelGenerationMode.Simple : LevelGenerationMode.Maze;

        if (large && difficultyLevel >= 3)
            return rnd.NextDouble() < 0.5 ? LevelGenerationMode.HybridOrganicPath : LevelGenerationMode.HybridMazeOpen;

        if (difficultyLevel >= 2)
            return rnd.NextDouble() < 0.5 ? LevelGenerationMode.Maze : LevelGenerationMode.Organic;

        return generationMode;
    }

    /// <summary>
    /// Validiert die Profilkonfiguration und gibt Warnungen aus
    /// </summary>
    public bool ValidateProfile()
    {
        bool isValid = true;

        if (levelSize < 5)
        {
            Debug.LogWarning($"LevelProfile '{profileName}': Level size too small (minimum 5)");
            isValid = false;
        }

        if (collectibleCount > (levelSize * levelSize * minWalkableArea / 100) / 4)
        {
            Debug.LogWarning($"LevelProfile '{profileName}': Too many collectibles for level size");
            isValid = false;
        }

        if (obstacleDensity < 0f || obstacleDensity > 1f)
        {
            Debug.LogWarning($"LevelProfile '{profileName}': Obstacle density must be between 0 and 1");
            isValid = false;
        }

        // Steampunk-Feature Validierung
        float totalDensity = obstacleDensity + rotatingObstacleDensity + movingPlatformDensity + steamEmitterDensity + interactiveGateDensity;
        if (totalDensity > 0.8f)
        {
            Debug.LogWarning($"LevelProfile '{profileName}': Total object density too high ({totalDensity:F2}). Recommended: <0.8");
        }

        if (groundMaterials == null || groundMaterials.Length == 0)
        {
            Debug.LogWarning($"LevelProfile '{profileName}': No ground materials assigned");
        }

        return isValid;
    }

    /// <summary>
    /// Erstellt eine Kopie des Profils mit angepassten Werten
    /// </summary>
    public LevelProfile CreateScaledProfile(float difficultyMultiplier)
    {
        LevelProfile scaledProfile = Instantiate(this);
        
        scaledProfile.collectibleCount = Mathf.RoundToInt(collectibleCount * difficultyMultiplier);
        scaledProfile.obstacleDensity = Mathf.Clamp01(obstacleDensity * difficultyMultiplier);
        scaledProfile.levelSize = Mathf.RoundToInt(levelSize * Mathf.Sqrt(difficultyMultiplier));
        scaledProfile.pathComplexity = Mathf.Clamp01(pathComplexity * difficultyMultiplier);
        
        // Steampunk-Features skalieren
        scaledProfile.rotatingObstacleDensity = Mathf.Clamp01(rotatingObstacleDensity * difficultyMultiplier);
        scaledProfile.movingPlatformDensity = Mathf.Clamp01(movingPlatformDensity * difficultyMultiplier);
        scaledProfile.steamEmitterDensity = Mathf.Clamp01(steamEmitterDensity * difficultyMultiplier);

        return scaledProfile;
    }

    // Automatically adjust densities based on generation mode
    public void AdjustDensitiesForMode()
    {
        switch (generationMode)
        {
            case LevelGenerationMode.Maze:
                obstacleDensity = 0.25f + 0.05f * difficultyLevel;
                rotatingObstacleDensity = 0.08f;
                break;
            case LevelGenerationMode.Organic:
                obstacleDensity = 0.45f;
                steamEmitterDensity = 0.12f;
                rotatingObstacleDensity = 0.03f;
                break;
            case LevelGenerationMode.HybridMazeOpen:
                obstacleDensity = 0.3f;
                rotatingObstacleDensity = 0.06f;
                interactiveGateDensity = 0.04f;
                break;
            case LevelGenerationMode.HybridOrganicPath:
                obstacleDensity = 0.4f;
                steamEmitterDensity = 0.08f;
                break;
        }
    }

    /// <summary>
    /// Berechnet eine Schwierigkeitsbewertung basierend auf allen Parametern
    /// </summary>
    public float CalculateDifficultyScore()
    {
        float score = 0f;
        
        // Basis-Schwierigkeit
        score += difficultyLevel * 20f;
        
        // Level-Größe
        score += (levelSize / 20f) * 10f;
        
        // Objekt-Dichten
        score += obstacleDensity * 15f;
        score += rotatingObstacleDensity * 12f;
        score += movingPlatformDensity * 10f;
        score += steamEmitterDensity * 5f;
        score += interactiveGateDensity * 8f;
        
        // Generationsmodus
        switch (generationMode)
        {
            case LevelGenerationMode.Simple: score += 0f; break;
            case LevelGenerationMode.Maze: score += 15f; break;
            case LevelGenerationMode.Platforms: score += 20f; break;
            case LevelGenerationMode.Organic: score += 18f; break;
            case LevelGenerationMode.HybridMazeOpen: score += 25f; break;
            case LevelGenerationMode.HybridOrganicPath: score += 28f; break;
        }
        
        return Mathf.Clamp(score, 0f, 100f);
    }

    /// <summary>
    /// Gibt eine benutzerfreundliche Schwierigkeitsbeschreibung zurück
    /// </summary>
    public string GetDifficultyDescription()
    {
        float score = CalculateDifficultyScore();
        
        if (score < 25f) return "Einfach";
        if (score < 45f) return "Normal";
        if (score < 70f) return "Schwierig";
        return "Sehr Schwierig";
    }

    void OnValidate()
    {
        // Clamp values in editor
        levelSize = Mathf.Max(5, levelSize);
        collectibleCount = Mathf.Max(1, collectibleCount);
        minCollectibleDistance = Mathf.Max(1, minCollectibleDistance);
        obstacleDensity = Mathf.Clamp01(obstacleDensity);
        frictionVariance = Mathf.Clamp01(frictionVariance);
        slipperyTileChance = Mathf.Clamp01(slipperyTileChance);
        movingObstacleChance = Mathf.Clamp01(movingObstacleChance);
        pathComplexity = Mathf.Clamp01(pathComplexity);
        minWalkableArea = Mathf.Clamp(minWalkableArea, 30, 95);
        spawnSafeRadius = Mathf.Max(1f, spawnSafeRadius);
        
        // Steampunk-Features clampen
        rotatingObstacleDensity = Mathf.Clamp01(rotatingObstacleDensity);
        movingPlatformDensity = Mathf.Clamp01(movingPlatformDensity);
        steamEmitterDensity = Mathf.Clamp01(steamEmitterDensity);
        interactiveGateDensity = Mathf.Clamp01(interactiveGateDensity);
        ambientLightIntensity = Mathf.Max(0f, ambientLightIntensity);
    }
}

/// <summary>
/// Generierungsmodi für verschiedene Leveltypen
/// </summary>
public enum LevelGenerationMode
{
    Simple,            // Einfache offene Fläche mit wenigen Hindernissen
    Maze,              // Labyrinth-ähnliche Struktur
    Platforms,         // Plattform-basiertes Level
    Organic,           // Organische, unregelmäßige Strukturen
    HybridMazeOpen,    // Hybrid aus Maze mit offenen Bereichen
    HybridOrganicPath  // Hybrid aus Organic mit garantiertem Pfad
}

/// <summary>
/// Steampunk-Themen für visuelle Gestaltung
/// </summary>
public enum SteampunkTheme
{
    Industrial,     // Industrielle Fabrik-Atmosphäre
    Victorian,      // Viktorianische Eleganz
    Airship,        // Luftschiff-Thema
    Underground,    // Unterirdische Anlagen
    Clockwork       // Uhrwerk-Mechanik
}

/// <summary>
/// Konfigurationsprofil für Steam-Emitter
/// </summary>
[System.Serializable]
public class SteamEmitterProfile
{
    [SerializeField] private float baseEmissionRate = 50f;
    [SerializeField] private bool useRandomBursts = true;
    [SerializeField] private bool affectPlayerMovement = false;
    [SerializeField] private float windForce = 2f;
    [SerializeField] private AudioClip[] steamSounds;
    [SerializeField] private AudioClip[] mechanicalSounds;

    public float BaseEmissionRate => baseEmissionRate;
    public bool UseRandomBursts => useRandomBursts;
    public bool AffectPlayerMovement => affectPlayerMovement;
    public float WindForce => windForce;
    public AudioClip[] SteamSounds => steamSounds;
    public AudioClip[] MechanicalSounds => mechanicalSounds;
}
