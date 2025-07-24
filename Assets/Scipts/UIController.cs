using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIController : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerController player;

    [Header("Flight UI")]
    [SerializeField] private Slider flyBar;
    [SerializeField] private Image flyBarFill;
    [SerializeField] private GameObject flyPanel;
    [SerializeField] private TextMeshProUGUI flyText;
    [SerializeField] private Color lowEnergyColor = Color.red;
    [SerializeField] private Color normalEnergyColor = Color.cyan;
    [SerializeField] private float lowEnergyThreshold = 0.3f;

    [Header("Status Displays")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI heightText;
    [SerializeField] private GameObject sprintIndicator;
    [SerializeField] private GameObject slideIndicator;
    [SerializeField] private GameObject doubleJumpIndicator;

    [Header("Collectible UI")]
    [SerializeField] private TextMeshProUGUI collectibleText;
    [SerializeField] private GameObject collectiblePanel;
    [SerializeField] private Image collectibleIcon;
    [SerializeField] private Color collectibleTextColor = Color.white;
    [SerializeField] private Color lowCollectibleColor = Color.red;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshair;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color normalCrosshairColor = Color.white;
    [SerializeField] private Color flyingCrosshairColor = Color.cyan;

    [Header("Animations")]
    [SerializeField] private float uiAnimationSpeed = 5f;
    [SerializeField] private AnimationCurve energyPulse = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);

    [Header("Level Profile Display")]
    [SerializeField] private TextMeshProUGUI levelTypeText;
    [SerializeField] private GameObject levelProfilePanel;
    [SerializeField] private Color easyLevelColor = Color.green;
    [SerializeField] private Color mediumLevelColor = Color.yellow;
    [SerializeField] private Color hardLevelColor = Color.red;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private TextMeshProUGUI debugText;

    // Private fields
    private float currentFlyEnergy;
    private float maxFlyEnergy;
    private bool isFlying;
    private bool isGrounded;
    private Coroutine energyAnimationCoroutine;
    private Coroutine pulseCoroutine;
    
    // Collectible tracking
    private int currentCollectibles = 0;
    private int totalCollectibles = 0;
    private LevelManager levelManager;
    
    // Level Profile tracking
    private LevelGenerator levelGenerator;
    private LevelProfile currentProfile;

    // UI Update optimization
    private float lastSpeedUpdate;
    private float lastHeightUpdate;
    private float lastDebugUpdate;
    private const float UI_UPDATE_INTERVAL = 0.1f; // Update every 100ms instead of every frame

    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
        ValidateComponents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeUI()
    {
        // Initialize fly bar
        if (flyBar)
        {
            flyBar.minValue = 0f;
            flyBar.maxValue = 1f;
            flyBar.value = 1f;
        }

        // Initialize colors
        if (flyBarFill)
            flyBarFill.color = normalEnergyColor;

        if (crosshairImage)
            crosshairImage.color = normalCrosshairColor;

        // Hide indicators initially
        SetIndicatorActive(sprintIndicator, false);
        SetIndicatorActive(slideIndicator, false);
        SetIndicatorActive(doubleJumpIndicator, false);

        // Set debug visibility
        if (debugText)
            debugText.gameObject.SetActive(showDebugInfo);
    }

    private void ValidateComponents()
    {
        if (!player)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (!player)
                Debug.LogError("UIController: PlayerController nicht gefunden!");
        }

        if (!flyBar)
            Debug.LogWarning("UIController: Fly Bar nicht zugewiesen!");

        if (!flyText)
            Debug.LogWarning("UIController: Fly Text nicht zugewiesen!");
            
        // Find LevelManager
        if (!levelManager)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
            if (!levelManager)
                Debug.LogWarning("UIController: LevelManager nicht gefunden!");
        }
        
        // Find LevelGenerator
        if (!levelGenerator)
        {
            levelGenerator = FindFirstObjectByType<LevelGenerator>();
            if (levelGenerator)
            {
                currentProfile = levelGenerator.ActiveProfile;
                UpdateLevelProfileDisplay();
            }
        }
        
        // Validate collectible UI
        if (!collectibleText)
            Debug.LogWarning("UIController: Collectible Text nicht zugewiesen!");
    }

    private void SubscribeToEvents()
    {
        if (!player) return;

        player.OnFlyEnergyChanged += OnFlyEnergyChanged;
        player.OnGroundedChanged += OnGroundedChanged;
        player.OnFlyingChanged += OnFlyingChanged;
        
        // Subscribe to LevelManager events
        if (levelManager)
        {
            levelManager.OnCollectibleCountChanged += OnCollectibleCountChanged;
            levelManager.OnLevelStarted += OnLevelStarted;
            levelManager.OnLevelCompleted += OnLevelCompleted;
        }
        
        // Subscribe to LevelGenerator events
        if (levelGenerator)
        {
            levelGenerator.OnLevelGenerationStarted += OnLevelGenerationStarted;
            levelGenerator.OnLevelGenerationCompleted += OnLevelGenerationCompleted;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!player) return;

        player.OnFlyEnergyChanged -= OnFlyEnergyChanged;
        player.OnGroundedChanged -= OnGroundedChanged;
        player.OnFlyingChanged -= OnFlyingChanged;
        
        // Unsubscribe from LevelManager events
        if (levelManager)
        {
            levelManager.OnCollectibleCountChanged -= OnCollectibleCountChanged;
            levelManager.OnLevelStarted -= OnLevelStarted;
            levelManager.OnLevelCompleted -= OnLevelCompleted;
        }
        
        // Unsubscribe from LevelGenerator events
        if (levelGenerator)
        {
            levelGenerator.OnLevelGenerationStarted -= OnLevelGenerationStarted;
            levelGenerator.OnLevelGenerationCompleted -= OnLevelGenerationCompleted;
        }
    }

    void Update()
    {
        if (!player) return;

        UpdateMovementIndicators();
        UpdatePeriodicUI();
        UpdateDebugInfo();
    }

    private void UpdateMovementIndicators()
    {
        // Sprint indicator
        SetIndicatorActive(sprintIndicator, player.IsSprinting);

        // Slide indicator
        SetIndicatorActive(slideIndicator, player.IsSliding);

        // Double jump indicator (show when available)
        bool canDoubleJump = !player.IsGrounded && !player.IsFlying;
        SetIndicatorActive(doubleJumpIndicator, canDoubleJump);
    }

    private void UpdatePeriodicUI()
    {
        float currentTime = Time.time;

        // Update speed display
        if (speedText && currentTime - lastSpeedUpdate > UI_UPDATE_INTERVAL)
        {
            float horizontalSpeed = new Vector3(player.Velocity.x, 0, player.Velocity.z).magnitude;
            speedText.text = $"Speed: {horizontalSpeed:F1} m/s";
            lastSpeedUpdate = currentTime;
        }

        // Update height display
        if (heightText && currentTime - lastHeightUpdate > UI_UPDATE_INTERVAL)
        {
            heightText.text = $"Height: {player.transform.position.y:F1}m";
            lastHeightUpdate = currentTime;
        }
    }

    private void UpdateDebugInfo()
    {
        if (!showDebugInfo || !debugText) return;

        float currentTime = Time.time;
        if (currentTime - lastDebugUpdate < UI_UPDATE_INTERVAL) return;

        string debugInfo = $"Grounded: {player.IsGrounded}\n";
        debugInfo += $"Flying: {player.IsFlying}\n";
        debugInfo += $"Sprinting: {player.IsSprinting}\n";
        debugInfo += $"Sliding: {player.IsSliding}\n";
        debugInfo += $"Velocity: {player.Velocity.magnitude:F2}\n";
        debugInfo += $"Fly Energy: {currentFlyEnergy:F2}/{maxFlyEnergy:F2}";

        debugText.text = debugInfo;
        lastDebugUpdate = currentTime;
    }

    private void SetIndicatorActive(GameObject indicator, bool active)
    {
        if (indicator && indicator.activeSelf != active)
            indicator.SetActive(active);
    }

    // Event handlers
    private void OnFlyEnergyChanged(float energy, float maxEnergy)
    {
        currentFlyEnergy = energy;
        maxFlyEnergy = maxEnergy;

        UpdateFlyBar();
        UpdateEnergyColors();
    }

    private void OnGroundedChanged(bool grounded)
    {
        isGrounded = grounded;
        
        // Show/hide crosshair based on ground state
        if (crosshair)
            crosshair.SetActive(!grounded);
    }

    private void OnFlyingChanged(bool flying)
    {
        isFlying = flying;
        
        UpdateFlyPanelVisibility();
        UpdateCrosshairColor();
        
        if (flying)
            StartEnergyPulseEffect();
        else
            StopEnergyPulseEffect();
    }

    private void UpdateFlyBar()
    {
        if (!flyBar) return;

        float targetValue = maxFlyEnergy > 0 ? currentFlyEnergy / maxFlyEnergy : 0f;
        
        // Smooth bar animation
        if (energyAnimationCoroutine != null)
            StopCoroutine(energyAnimationCoroutine);
        
        energyAnimationCoroutine = StartCoroutine(AnimateFlyBar(targetValue));
    }

    private void UpdateEnergyColors()
    {
        if (!flyBarFill) return;

        float energyPercent = maxFlyEnergy > 0 ? currentFlyEnergy / maxFlyEnergy : 0f;
        Color targetColor = energyPercent <= lowEnergyThreshold ? lowEnergyColor : normalEnergyColor;
        
        flyBarFill.color = Color.Lerp(flyBarFill.color, targetColor, Time.deltaTime * uiAnimationSpeed);
    }

    private void UpdateFlyPanelVisibility()
    {
        if (!flyPanel) return;

        // Show panel when flying or when energy is not full
        bool shouldShow = isFlying || currentFlyEnergy < maxFlyEnergy;
        
        if (flyPanel.activeSelf != shouldShow)
            flyPanel.SetActive(shouldShow);

        // Update fly text
        if (flyText)
        {
            flyText.gameObject.SetActive(isFlying && currentFlyEnergy > 0);
            
            if (isFlying && currentFlyEnergy > 0)
            {
                string flyStatus = currentFlyEnergy <= lowEnergyThreshold ? "LOW ENERGY!" : "FLYING";
                flyText.text = flyStatus;
                flyText.color = currentFlyEnergy <= lowEnergyThreshold ? lowEnergyColor : normalEnergyColor;
            }
        }
    }

    private void UpdateCrosshairColor()
    {
        if (!crosshairImage) return;

        Color targetColor = isFlying ? flyingCrosshairColor : normalCrosshairColor;
        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * uiAnimationSpeed);
    }

    private void StartEnergyPulseEffect()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        
        pulseCoroutine = StartCoroutine(PulseEnergyBar());
    }

    private void StopEnergyPulseEffect()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Reset scale
        if (flyBar)
            flyBar.transform.localScale = Vector3.one;
    }

    // Coroutines
    private IEnumerator AnimateFlyBar(float targetValue)
    {
        float startValue = flyBar.value;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            flyBar.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        flyBar.value = targetValue;
        energyAnimationCoroutine = null;
    }

    private IEnumerator PulseEnergyBar()
    {
        Transform barTransform = flyBar.transform;
        Vector3 originalScale = barTransform.localScale;

        while (isFlying)
        {
            float time = 0f;
            float pulseDuration = 1f;

            while (time < pulseDuration)
            {
                time += Time.deltaTime;
                float pulseValue = energyPulse.Evaluate(time / pulseDuration);
                barTransform.localScale = originalScale * pulseValue;
                yield return null;
            }
        }

        barTransform.localScale = originalScale;
    }

    // Public methods for external access
    public void SetShowDebugInfo(bool show)
    {
        showDebugInfo = show;
        if (debugText)
            debugText.gameObject.SetActive(show);
    }

    // LevelManager Event Handlers
    private void OnCollectibleCountChanged(int remaining, int total)
    {
        currentCollectibles = remaining;
        totalCollectibles = total;
        UpdateCollectibleDisplay();
    }

    private void OnLevelStarted(LevelConfiguration config)
    {
        if (collectiblePanel)
            collectiblePanel.SetActive(true);
        
        currentCollectibles = config.collectiblesRemaining;
        totalCollectibles = config.totalCollectibles;
        UpdateCollectibleDisplay();
    }

    private void OnLevelCompleted(LevelConfiguration config)
    {
        ShowNotification($"Level {config.levelName} Complete!", 2f);
        
        if (collectiblePanel)
        {
            StartCoroutine(FadeOutCollectiblePanel());
        }
    }

    private void UpdateCollectibleDisplay()
    {
        if (!collectibleText) return;

        collectibleText.text = $"Collectibles: {currentCollectibles}";
        
        // Color coding based on remaining collectibles
        if (currentCollectibles <= 1)
        {
            collectibleText.color = lowCollectibleColor;
        }
        else
        {
            collectibleText.color = collectibleTextColor;
        }
        
        // Pulse effect when low
        if (currentCollectibles <= 2 && currentCollectibles > 0)
        {
            StartCoroutine(PulseCollectibleText());
        }
    }

    private System.Collections.IEnumerator FadeOutCollectiblePanel()
    {
        if (!collectiblePanel) yield break;
        
        CanvasGroup canvasGroup = collectiblePanel.GetComponent<CanvasGroup>();
        if (!canvasGroup)
        {
            canvasGroup = collectiblePanel.AddComponent<CanvasGroup>();
        }
        
        float elapsed = 0f;
        float duration = 1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        
        collectiblePanel.SetActive(false);
        canvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator PulseCollectibleText()
    {
        if (!collectibleText) yield break;
        
        Vector3 originalScale = collectibleText.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        // Scale up
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            collectibleText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        // Scale down
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            collectibleText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        collectibleText.transform.localScale = originalScale;
    }

    public void ShowNotification(string message, float duration = 2f)
    {
        StartCoroutine(ShowNotificationCoroutine(message, duration));
    }

    private IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        // This could be expanded with a notification UI element
        Debug.Log($"Notification: {message}");
        yield return new WaitForSeconds(duration);
    }
    
    // Level Generator Event Handlers
    private void OnLevelGenerationStarted(LevelProfile profile)
    {
        currentProfile = profile;
        UpdateLevelProfileDisplay();
        
        if (levelTypeText)
        {
            levelTypeText.text = "Generiere Level...";
        }
    }
    
    private void OnLevelGenerationCompleted(LevelProfile profile)
    {
        currentProfile = profile;
        UpdateLevelProfileDisplay();
        
        ShowNotification($"Level '{profile.DisplayName}' generiert!", 1.5f);
    }
    
    /// <summary>
    /// Aktualisiert die Anzeige des aktiven Levelprofils
    /// </summary>
    private void UpdateLevelProfileDisplay()
    {
        if (!currentProfile || !levelTypeText) return;
        
        // Update text and color based on difficulty
        levelTypeText.text = $"Level: {currentProfile.DisplayName}";
        
        // Set color based on difficulty level
        Color profileColor = GetDifficultyColor(currentProfile.DifficultyLevel);
        levelTypeText.color = profileColor;
        
        // Show/hide profile panel
        if (levelProfilePanel)
        {
            levelProfilePanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Gibt die Farbe basierend auf dem Schwierigkeitsgrad zurück
    /// </summary>
    private Color GetDifficultyColor(int difficultyLevel)
    {
        switch (difficultyLevel)
        {
            case 1:
                return easyLevelColor;
            case 2:
                return mediumLevelColor;
            case 3:
            default:
                return hardLevelColor;
        }
    }
    
    /// <summary>
    /// Setzt das aktuelle Levelprofil (für externe Nutzung)
    /// </summary>
    public void SetCurrentProfile(LevelProfile profile)
    {
        currentProfile = profile;
        UpdateLevelProfileDisplay();
    }
    
    /// <summary>
    /// Zeigt/versteckt das Levelprofil-Panel
    /// </summary>
    public void ToggleLevelProfileDisplay(bool show)
    {
        if (levelProfilePanel)
        {
            levelProfilePanel.SetActive(show);
        }
    }
}