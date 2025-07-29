using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Advanced level selection system with progression integration
/// Provides visual level overview with unlock status and requirements
/// </summary>
[AddComponentMenu("UI/LevelSelectionUI")]
public class LevelSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject levelSelectionPanel;
    [SerializeField] private Transform levelGridContainer;
    [SerializeField] private GameObject levelItemPrefab;
    [SerializeField] private ScrollRect levelScrollRect;
    
    [Header("Level Details Panel")]
    [SerializeField] private GameObject levelDetailsPanel;
    [SerializeField] private Image levelPreviewImage;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI levelDescriptionText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI estimatedTimeText;
    [SerializeField] private TextMeshProUGUI recommendedCollectiblesText;
    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private Button playLevelButton;
    [SerializeField] private Button backButton;
    
    [Header("Filtering")]
    [SerializeField] private TMP_Dropdown difficultyFilter;
    [SerializeField] private TMP_Dropdown statusFilter;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private Button resetFiltersButton;
    
    [Header("Progression Display")]
    [SerializeField] private TextMeshProUGUI progressionText;
    [SerializeField] private Slider progressionSlider;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private TextMeshProUGUI experienceText;
    
    [Header("Unlock Requirements")]
    [SerializeField] private GameObject requirementsPanel;
    [SerializeField] private TextMeshProUGUI requirementsTitle;
    [SerializeField] private Transform requirementsList;
    [SerializeField] private GameObject requirementItemPrefab;
    
    [Header("Navigation")]
    [SerializeField] private Button quickPlayButton;
    [SerializeField] private Button randomLevelButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Visual Effects")]
    [SerializeField] private float levelItemAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve levelItemAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private ParticleSystem unlockEffect;
    
    [Header("Audio")]
    [SerializeField] private AudioClip levelSelectSound;
    [SerializeField] private AudioClip levelUnlockSound;
    [SerializeField] private AudioClip uiNavigationSound;
    
    // Private fields
    private List<LevelItemUI> levelItems = new List<LevelItemUI>();
    private LevelInfo selectedLevel;
    private LevelDifficulty currentDifficultyFilter = (LevelDifficulty)(-1); // -1 = All
    private LevelStatusFilter currentStatusFilter = LevelStatusFilter.All;
    private string currentSearchTerm = "";
    
    // Events
    public System.Action<LevelInfo> OnLevelSelected;
    public System.Action<LevelInfo> OnLevelStarted;
    
    // Properties
    public bool IsVisible => levelSelectionPanel && levelSelectionPanel.activeSelf;
    public LevelInfo SelectedLevel => selectedLevel;
    
    void Start()
    {
        InitializeLevelSelection();
        SetupEventListeners();
    }
    
    private void InitializeLevelSelection()
    {
        // Setup filters
        SetupFilters();
        
        // Hide details panel initially
        if (levelDetailsPanel)
            levelDetailsPanel.SetActive(false);
        
        if (requirementsPanel)
            requirementsPanel.SetActive(false);
        
        // Subscribe to progression events
        if (ProgressionManager.Instance)
        {
            ProgressionManager.Instance.OnLevelUnlocked += OnLevelUnlocked;
            ProgressionManager.Instance.OnLevelCompleted += OnLevelCompleted;
            ProgressionManager.Instance.OnProgressionUpdated += OnProgressionUpdated;
            ProgressionManager.Instance.OnPlayerLevelChanged += OnPlayerLevelChanged;
            ProgressionManager.Instance.OnExperienceChanged += OnExperienceChanged;
        }
        
        // Initial UI update
        UpdateUI();
    }
    
    private void SetupFilters()
    {
        // Setup difficulty filter
        if (difficultyFilter)
        {
            difficultyFilter.ClearOptions();
            var difficulties = new List<string> { "All Difficulties" };
            difficulties.AddRange(System.Enum.GetNames(typeof(LevelDifficulty)));
            difficultyFilter.AddOptions(difficulties);
            difficultyFilter.onValueChanged.AddListener(OnDifficultyFilterChanged);
        }
        
        // Setup status filter
        if (statusFilter)
        {
            statusFilter.ClearOptions();
            statusFilter.AddOptions(new List<string> 
            { 
                "All Levels", 
                "Available", 
                "Locked", 
                "Completed", 
                "Incomplete" 
            });
            statusFilter.onValueChanged.AddListener(OnStatusFilterChanged);
        }
        
        // Setup search field
        if (searchField)
        {
            searchField.onValueChanged.AddListener(OnSearchTermChanged);
        }
    }
    
    private void SetupEventListeners()
    {
        // Navigation buttons
        if (quickPlayButton) quickPlayButton.onClick.AddListener(QuickPlay);
        if (randomLevelButton) randomLevelButton.onClick.AddListener(PlayRandomLevel);
        if (continueButton) continueButton.onClick.AddListener(ContinueProgression);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        
        // Level details buttons
        if (playLevelButton) playLevelButton.onClick.AddListener(PlaySelectedLevel);
        if (backButton) backButton.onClick.AddListener(HideLevelDetails);
        
        // Filter buttons
        if (resetFiltersButton) resetFiltersButton.onClick.AddListener(ResetFilters);
    }
    
    #region UI Updates
    
    public void UpdateUI()
    {
        UpdateProgressionDisplay();
        UpdateLevelGrid();
        UpdateNavigationButtons();
    }
    
    private void UpdateProgressionDisplay()
    {
        if (!ProgressionManager.Instance) return;
        
        var stats = ProgressionManager.Instance.GetProgressionStats();
        
        // Update progression text and slider
        if (progressionText)
        {
            progressionText.text = $"Progress: {stats.completedLevels}/{stats.totalLevels} levels ({stats.completionPercentage:F1}%)";
        }
        
        if (progressionSlider)
        {
            progressionSlider.value = stats.completionPercentage / 100f;
        }
        
        // Update player level display
        if (playerLevelText)
        {
            playerLevelText.text = $"Level {stats.currentPlayerLevel}";
        }
        
        // Update experience display
        if (experienceSlider)
        {
            int expToNext = ProgressionManager.Instance.ExperienceToNextLevel;
            int currentLevelExp = stats.totalExperience - GetExperienceForLevel(stats.currentPlayerLevel);
            int nextLevelTotalExp = GetExperienceForLevel(stats.currentPlayerLevel + 1) - GetExperienceForLevel(stats.currentPlayerLevel);
            
            if (nextLevelTotalExp > 0)
            {
                experienceSlider.value = (float)currentLevelExp / nextLevelTotalExp;
            }
        }
        
        if (experienceText)
        {
            experienceText.text = $"XP: {stats.totalExperience} (+{ProgressionManager.Instance.ExperienceToNextLevel} to next level)";
        }
    }
    
    private void UpdateLevelGrid()
    {
        if (!ProgressionManager.Instance || !levelGridContainer) return;
        
        // Clear existing items
        foreach (LevelItemUI item in levelItems)
        {
            if (item && item.gameObject)
                Destroy(item.gameObject);
        }
        levelItems.Clear();
        
        // Get filtered levels
        var levels = GetFilteredLevels();
        
        // Create level items
        foreach (LevelInfo level in levels)
        {
            CreateLevelItem(level);
        }
        
        // Animate level items
        StartCoroutine(AnimateLevelItems());
    }
    
    private List<LevelInfo> GetFilteredLevels()
    {
        if (!ProgressionManager.Instance) return new List<LevelInfo>();
        
        var levels = ProgressionManager.Instance.AllLevels.AsEnumerable();
        
        // Apply difficulty filter
        if (currentDifficultyFilter != (LevelDifficulty)(-1))
        {
            levels = levels.Where(l => l.difficulty == currentDifficultyFilter);
        }
        
        // Apply status filter
        switch (currentStatusFilter)
        {
            case LevelStatusFilter.Available:
                levels = levels.Where(l => l.IsAvailable);
                break;
            case LevelStatusFilter.Locked:
                levels = levels.Where(l => !l.IsAvailable);
                break;
            case LevelStatusFilter.Completed:
                levels = levels.Where(l => l.isCompleted);
                break;
            case LevelStatusFilter.Incomplete:
                levels = levels.Where(l => l.IsAvailable && !l.isCompleted);
                break;
        }
        
        // Apply search filter
        if (!string.IsNullOrEmpty(currentSearchTerm))
        {
            string searchLower = currentSearchTerm.ToLower();
            levels = levels.Where(l => 
                l.displayName.ToLower().Contains(searchLower) ||
                l.description.ToLower().Contains(searchLower));
        }
        
        return levels.OrderBy(l => GetLevelOrder(l)).ToList();
    }
    
    private int GetLevelOrder(LevelInfo level)
    {
        // Custom ordering: tutorial first, then by difficulty, then by name
        if (level.difficulty == LevelDifficulty.Tutorial) return 0;
        return (int)level.difficulty * 1000 + level.displayName.GetHashCode() % 1000;
    }
    
    private void CreateLevelItem(LevelInfo level)
    {
        if (!levelItemPrefab) return;
        
        GameObject itemObj = Instantiate(levelItemPrefab, levelGridContainer);
        LevelItemUI levelItem = itemObj.GetComponent<LevelItemUI>();
        
        if (levelItem)
        {
            levelItem.Setup(level, OnLevelItemClicked);
            levelItems.Add(levelItem);
        }
    }
    
    private IEnumerator AnimateLevelItems()
    {
        foreach (LevelItemUI item in levelItems)
        {
            if (item)
            {
                StartCoroutine(AnimateLevelItem(item));
                yield return new WaitForSeconds(0.05f); // Stagger animations
            }
        }
    }
    
    private IEnumerator AnimateLevelItem(LevelItemUI item)
    {
        if (!item) yield break;
        
        // Start with scale 0
        item.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < levelItemAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / levelItemAnimationDuration;
            float scale = levelItemAnimationCurve.Evaluate(t);
            
            if (item && item.transform)
                item.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        if (item && item.transform)
            item.transform.localScale = Vector3.one;
    }
    
    private void UpdateNavigationButtons()
    {
        if (!ProgressionManager.Instance) return;
        
        // Update continue button
        if (continueButton)
        {
            var nextLevel = ProgressionManager.Instance.GetNextRecommendedLevel();
            continueButton.interactable = nextLevel != null;
            
            if (nextLevel != null)
            {
                var buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText)
                {
                    buttonText.text = $"Continue: {nextLevel.displayName}";
                }
            }
        }
        
        // Update quick play button
        if (quickPlayButton)
        {
            var availableLevels = ProgressionManager.Instance.UnlockedLevels;
            quickPlayButton.interactable = availableLevels.Count > 0;
        }
        
        // Update random level button
        if (randomLevelButton)
        {
            var availableLevels = ProgressionManager.Instance.UnlockedLevels;
            randomLevelButton.interactable = availableLevels.Count > 0;
        }
    }
    
    #endregion
    
    #region Level Selection
    
    private void OnLevelItemClicked(LevelInfo level)
    {
        PlayUISound(levelSelectSound);
        SelectLevel(level);
    }
    
    public void SelectLevel(LevelInfo level)
    {
        selectedLevel = level;
        ShowLevelDetails(level);
        OnLevelSelected?.Invoke(level);
    }
    
    private void ShowLevelDetails(LevelInfo level)
    {
        if (!levelDetailsPanel) return;
        
        levelDetailsPanel.SetActive(true);
        
        // Update level details
        if (levelTitleText) levelTitleText.text = level.displayName;
        if (levelDescriptionText) levelDescriptionText.text = level.description;
        if (difficultyText) 
        {
            difficultyText.text = $"Difficulty: {level.difficulty}";
            difficultyText.color = GetDifficultyColor(level.difficulty);
        }
        if (estimatedTimeText) 
        {
            int minutes = level.estimatedCompletionTime / 60;
            int seconds = level.estimatedCompletionTime % 60;
            estimatedTimeText.text = $"Est. Time: {minutes}:{seconds:00}";
        }
        if (recommendedCollectiblesText) 
            recommendedCollectiblesText.text = $"Collectibles: {level.recommendedCollectibles}";
        
        // Update best records
        if (bestTimeText)
        {
            if (level.bestTime < float.MaxValue)
            {
                int minutes = Mathf.FloorToInt(level.bestTime / 60f);
                int seconds = Mathf.FloorToInt(level.bestTime % 60f);
                bestTimeText.text = $"Best Time: {minutes}:{seconds:00}";
            }
            else
            {
                bestTimeText.text = "Best Time: --:--";
            }
        }
        
        if (bestScoreText)
        {
            bestScoreText.text = level.bestScore > 0 ? $"Best Score: {level.bestScore:N0}" : "Best Score: ---";
        }
        
        // Update play button
        if (playLevelButton)
        {
            playLevelButton.interactable = level.IsAvailable;
            var buttonText = playLevelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText)
            {
                if (level.IsAvailable)
                {
                    buttonText.text = level.isCompleted ? "Play Again" : "Play Level";
                }
                else
                {
                    buttonText.text = "Locked";
                }
            }
        }
        
        // Show unlock requirements if locked
        if (!level.IsAvailable)
        {
            ShowUnlockRequirements(level);
        }
        else if (requirementsPanel)
        {
            requirementsPanel.SetActive(false);
        }
        
        // Update preview image
        if (levelPreviewImage && level.levelIcon)
        {
            // TODO: Cache sprites and load asynchronously for large levels
            levelPreviewImage.sprite = level.levelIcon;
        }
    }
    
    private void ShowUnlockRequirements(LevelInfo level)
    {
        if (!requirementsPanel || !requirementsList || !requirementItemPrefab) return;
        
        requirementsPanel.SetActive(true);
        
        if (requirementsTitle)
        {
            requirementsTitle.text = $"Unlock Requirements for {level.displayName}";
        }
        
        // Clear existing requirements
        for (int i = 0; i < requirementsList.childCount; i++)
        {
            Destroy(requirementsList.GetChild(i).gameObject);
        }
        
        // Get requirements
        var requirements = ProgressionManager.Instance.GetUnlockRequirements(level.levelId);
        
        // Create requirement items
        foreach (string requirement in requirements)
        {
            GameObject reqItem = Instantiate(requirementItemPrefab, requirementsList);
            var reqText = reqItem.GetComponentInChildren<TextMeshProUGUI>();
            if (reqText)
            {
                reqText.text = "• " + requirement;
            }
        }
    }
    
    private void HideLevelDetails()
    {
        if (levelDetailsPanel)
            levelDetailsPanel.SetActive(false);
        
        if (requirementsPanel)
            requirementsPanel.SetActive(false);
        
        selectedLevel = null;
        PlayUISound(uiNavigationSound);
    }
    
    #endregion
    
    #region Navigation Actions
    
    public void QuickPlay()
    {
        var nextLevel = ProgressionManager.Instance?.GetNextRecommendedLevel();
        if (nextLevel != null)
        {
            PlayLevel(nextLevel);
        }
    }
    
    public void PlayRandomLevel()
    {
        var availableLevels = ProgressionManager.Instance?.UnlockedLevels;
        if (availableLevels != null && availableLevels.Count > 0)
        {
            var randomLevel = availableLevels[Random.Range(0, availableLevels.Count)];
            PlayLevel(randomLevel);
        }
    }
    
    public void ContinueProgression()
    {
        var nextLevel = ProgressionManager.Instance?.GetNextRecommendedLevel();
        if (nextLevel != null)
        {
            PlayLevel(nextLevel);
        }
    }
    
    public void PlaySelectedLevel()
    {
        if (selectedLevel != null && selectedLevel.IsAvailable)
        {
            PlayLevel(selectedLevel);
        }
    }
    
    private void PlayLevel(LevelInfo level)
    {
        OnLevelStarted?.Invoke(level);
        ProgressionManager.Instance?.LoadLevel(level.levelId);
    }
    
    public void ReturnToMainMenu()
    {
        PlayUISound(uiNavigationSound);
        
        // Hide level selection
        if (levelSelectionPanel)
            levelSelectionPanel.SetActive(false);
        
        // Show main menu
        var uiController = FindFirstObjectByType<UIController>();
        if (uiController)
        {
            uiController.ShowMainMenu();
        }
    }
    
    #endregion
    
    #region Filters
    
    private void OnDifficultyFilterChanged(int index)
    {
        currentDifficultyFilter = index == 0 ? (LevelDifficulty)(-1) : (LevelDifficulty)(index - 1);
        UpdateLevelGrid();
        PlayUISound(uiNavigationSound);
    }
    
    private void OnStatusFilterChanged(int index)
    {
        currentStatusFilter = (LevelStatusFilter)index;
        UpdateLevelGrid();
        PlayUISound(uiNavigationSound);
    }
    
    private void OnSearchTermChanged(string searchTerm)
    {
        currentSearchTerm = searchTerm;
        UpdateLevelGrid();
    }
    
    private void ResetFilters()
    {
        if (difficultyFilter) difficultyFilter.value = 0;
        if (statusFilter) statusFilter.value = 0;
        if (searchField) searchField.text = "";
        
        currentDifficultyFilter = (LevelDifficulty)(-1);
        currentStatusFilter = LevelStatusFilter.All;
        currentSearchTerm = "";
        
        UpdateLevelGrid();
        PlayUISound(uiNavigationSound);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnLevelUnlocked(LevelInfo level)
    {
        PlayUISound(levelUnlockSound);
        
        if (unlockEffect)
        {
            unlockEffect.Play();
        }
        
        UpdateUI();
    }
    
    private void OnLevelCompleted(LevelInfo level)
    {
        UpdateUI();
    }
    
    private void OnProgressionUpdated(List<LevelInfo> levels)
    {
        UpdateUI();
    }
    
    private void OnPlayerLevelChanged(int newLevel, int oldLevel)
    {
        UpdateProgressionDisplay();
    }
    
    private void OnExperienceChanged(int current, int toNext, int total)
    {
        UpdateProgressionDisplay();
    }
    
    #endregion
    
    #region Public API
    
    public void ShowLevelSelection()
    {
        if (levelSelectionPanel)
            levelSelectionPanel.SetActive(true);
        
        UpdateUI();
    }
    
    public void HideLevelSelection()
    {
        if (levelSelectionPanel)
            levelSelectionPanel.SetActive(false);
        
        HideLevelDetails();
    }
    
    #endregion
    
    #region Utility Methods
    
    private Color GetDifficultyColor(LevelDifficulty difficulty)
    {
        return difficulty switch
        {
            LevelDifficulty.Tutorial => Color.green,
            LevelDifficulty.Easy => Color.cyan,
            LevelDifficulty.Medium => Color.yellow,
            LevelDifficulty.Hard => Color.orange,
            LevelDifficulty.Expert => Color.red,
            LevelDifficulty.Master => Color.magenta,
            _ => Color.white
        };
    }
    
    private int GetExperienceForLevel(int level)
    {
        // This should match ProgressionManager's calculation
        // Simplified version for UI display
        int baseExp = 100;
        float multiplier = 1.5f;
        
        int totalExp = 0;
        int expRequired = baseExp;
        
        for (int i = 1; i < level; i++)
        {
            totalExp += expRequired;
            expRequired = Mathf.RoundToInt(baseExp * Mathf.Pow(multiplier, i));
        }
        
        return totalExp;
    }
    
    private void PlayUISound(AudioClip clip)
    {
        if (clip && AudioSource.FindFirstObjectByType<AudioSource>())
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 0.5f);
        }
    }
    
    #endregion
}

public enum LevelStatusFilter
{
    All,
    Available,
    Locked,
    Completed,
    Incomplete
}

/// <summary>
/// Individual level item in the level selection grid
/// </summary>
[System.Serializable]
public class LevelItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private GameObject completedIndicator;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button levelButton;
    [SerializeField] private Image difficultyBorder;
    [SerializeField] private GameObject perfectCompletionStar;
    
    [Header("Visual States")]
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color lockedColor = Color.gray;
    
    private LevelInfo levelInfo;
    private System.Action<LevelInfo> onClickCallback;
    
    public void Setup(LevelInfo level, System.Action<LevelInfo> onClick)
    {
        levelInfo = level;
        onClickCallback = onClick;
        
        UpdateVisuals();
        
        if (levelButton)
        {
            levelButton.onClick.RemoveAllListeners();
            levelButton.onClick.AddListener(() => onClickCallback?.Invoke(levelInfo));
        }
    }
    
    private void UpdateVisuals()
    {
        if (levelInfo == null) return;
        
        // Update texts
        if (titleText) titleText.text = levelInfo.displayName;
        if (difficultyText) difficultyText.text = levelInfo.difficulty.ToString();
        
        // Update icon
        if (iconImage && levelInfo.levelIcon)
        {
            iconImage.sprite = levelInfo.levelIcon;
        }
        
        // Update visual state
        bool isAvailable = levelInfo.IsAvailable;
        bool isCompleted = levelInfo.isCompleted;
        
        // Background color
        if (backgroundImage)
        {
            if (!isAvailable)
                backgroundImage.color = lockedColor;
            else if (isCompleted)
                backgroundImage.color = completedColor;
            else
                backgroundImage.color = availableColor;
        }
        
        // Difficulty border
        if (difficultyBorder)
        {
            difficultyBorder.color = GetDifficultyColor(levelInfo.difficulty);
        }
        
        // Completed indicator
        if (completedIndicator)
            completedIndicator.SetActive(isCompleted);
        
        // Perfect completion star
        if (perfectCompletionStar)
            perfectCompletionStar.SetActive(levelInfo.perfectCompletion);
        
        // Locked overlay
        if (lockedOverlay)
            lockedOverlay.SetActive(!isAvailable);
        
        // Button interactable
        if (levelButton)
            levelButton.interactable = isAvailable;
    }
    
    private Color GetDifficultyColor(LevelDifficulty difficulty)
    {
        return difficulty switch
        {
            LevelDifficulty.Tutorial => Color.green,
            LevelDifficulty.Easy => Color.cyan,
            LevelDifficulty.Medium => Color.yellow,
            LevelDifficulty.Hard => Color.orange,
            LevelDifficulty.Expert => Color.red,
            LevelDifficulty.Master => Color.magenta,
            _ => Color.white
        };
    }
}
