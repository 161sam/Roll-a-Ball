using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIController : MonoBehaviour
{
    [Header("Player UI")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Slider flyBar;
    [SerializeField] private TextMeshProUGUI flyText;
    [SerializeField] private TextMeshProUGUI collectibleText;
    [SerializeField] private TextMeshProUGUI levelTypeText;
    [SerializeField] private TextMeshProUGUI notificationText;
    
    [Header("Map UI")]
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private GameObject gameUIPanel;
    [SerializeField] private Button regenerateMapButton;
    [SerializeField] private Button backToMenuButton;

    private LevelManager levelManager;
    private LevelGenerator levelGenerator;
    private LevelProfile currentProfile;
    private Coroutine notificationRoutine;

    void Start()
    {
        ValidateComponents();
        SetupMapUI();
    }

    private void ValidateComponents()
    {
        if (!player)
            player = FindFirstObjectByType<PlayerController>();

        if (!flyBar)
        {
            GameObject flyBarGO = GameObject.Find("FlyBar");
            if (flyBarGO)
                flyBar = flyBarGO.GetComponent<Slider>();
        }

        if (!flyText)
        {
            GameObject flyTextGO = GameObject.Find("FlyText");
            if (flyTextGO)
                flyText = flyTextGO.GetComponent<TextMeshProUGUI>();
        }

        if (!collectibleText)
        {
            GameObject collectibleTextGO = GameObject.Find("CollectibleText");
            if (collectibleTextGO)
                collectibleText = collectibleTextGO.GetComponent<TextMeshProUGUI>();
        }

        if (!levelTypeText)
        {
            GameObject levelTypeTextGO = GameObject.Find("LevelTypeText");
            if (levelTypeTextGO)
                levelTypeText = levelTypeTextGO.GetComponent<TextMeshProUGUI>();
        }

        if (!notificationText)
        {
            GameObject notifGO = GameObject.Find("NotificationText");
            if (notifGO)
            {
                notificationText = notifGO.GetComponent<TextMeshProUGUI>();
                notificationText.gameObject.SetActive(false);
            }
        }

        if (!locationText)
        {
            GameObject locationGO = GameObject.Find("LocationText");
            if (locationGO)
                locationText = locationGO.GetComponent<TextMeshProUGUI>();
        }

        if (!gameUIPanel)
        {
            GameObject gameUIPanelGO = GameObject.Find("GameUIPanel");
            if (gameUIPanelGO)
                gameUIPanel = gameUIPanelGO;
        }

        levelManager = FindFirstObjectByType<LevelManager>();
        levelGenerator = FindFirstObjectByType<LevelGenerator>();

        if (levelGenerator)
        {
            currentProfile = levelGenerator.ActiveProfile;
            UpdateLevelDisplay();
        }
    }

    private void SetupMapUI()
    {
        if (regenerateMapButton != null)
        {
            regenerateMapButton.onClick.RemoveAllListeners();
            regenerateMapButton.onClick.AddListener(OnRegenerateMapClicked);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }
    }

    private void UpdateLevelDisplay()
    {
        if (levelTypeText && currentProfile)
        {
            levelTypeText.text = $"Level: {currentProfile.DisplayName}";

            Color profileColor = Color.green;
            if (currentProfile.DifficultyLevel == 2) profileColor = Color.yellow;
            else if (currentProfile.DifficultyLevel >= 3) profileColor = Color.red;

            levelTypeText.color = profileColor;
        }
    }

    public void UpdateCollectibleDisplay(int count)
    {
        if (collectibleText)
            collectibleText.text = $"Collectibles: {count}";
    }

    public void UpdateFlyEnergy(float energy, float maxEnergy)
    {
        if (flyBar)
            flyBar.value = energy / maxEnergy;

        if (flyText)
        {
            if (energy > 0)
            {
                flyText.gameObject.SetActive(true);
                flyText.text = energy < maxEnergy * 0.3f ? "LOW ENERGY!" : "FLYING";
            }
            else
            {
                flyText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowNotification(string message, float duration = 2f)
    {
        if (notificationText == null) return;

        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);

        notificationRoutine = StartCoroutine(ShowNotificationRoutine(message, duration));
    }

    private IEnumerator ShowNotificationRoutine(string message, float duration)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        notificationText.gameObject.SetActive(false);
    }

    // New Map-specific UI methods
    public void UpdateLocationDisplay(string locationName)
    {
        if (locationText != null)
        {
            locationText.text = $"Ort: {locationName}";
        }
    }

    public void ShowGameUI()
    {
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(true);
        }
    }

    public void HideGameUI()
    {
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(false);
        }
    }

    private void OnRegenerateMapClicked()
    {
        RollABall.Map.MapStartupController mapController = FindFirstObjectByType<RollABall.Map.MapStartupController>();
        if (mapController != null)
        {
            mapController.RegenerateCurrentMap();
        }
    }

    private void OnBackToMenuClicked()
    {
        RollABall.Map.MapStartupController mapController = FindFirstObjectByType<RollABall.Map.MapStartupController>();
        if (mapController != null)
        {
            mapController.ResetController();
        }
    }
}
