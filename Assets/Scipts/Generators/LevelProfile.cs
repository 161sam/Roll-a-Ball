using UnityEngine;

/// <summary>
/// ScriptableObject zur Konfiguration von prozedural generierten Leveln
/// Definiert Schwierigkeit, Größe, Hindernisse und visuelle Aspekte
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

    [Header("Spawn Configuration")]
    [SerializeField] private Vector3 playerSpawnOffset = Vector3.up;
    [SerializeField] private bool randomizeSpawnPosition = false;
    [SerializeField] private float spawnSafeRadius = 3f;

    [Header("Advanced Settings")]
    [SerializeField] private int generationSeed = 0; // 0 = zufällig
    [SerializeField] private bool useTimeBasedSeed = true;
    [SerializeField] private LevelGenerationMode generationMode = LevelGenerationMode.Maze;
    [SerializeField] private float pathComplexity = 0.5f; // 0 = einfache Wege, 1 = komplexe Labyrinthe

    // Properties für externen Zugriff
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
        
        return scaledProfile;
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
    }
}

/// <summary>
/// Generierungsmodi für verschiedene Leveltypen
/// </summary>
public enum LevelGenerationMode
{
    Simple,      // Einfache offene Fläche mit wenigen Hindernissen
    Maze,        // Labyrinth-ähnliche Struktur
    Platforms,   // Plattform-basiertes Level
    Organic,     // Organische, unregelmäßige Strukturen
    Hybrid       // Mischung verschiedener Modi
}
