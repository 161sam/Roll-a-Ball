using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using RollABall.Utility;

[System.Serializable]
public class CollectibleData
{
    [Header("Collectible Properties")]
    public string itemName = "Collectible";
    public string itemType = "standard";
    public int pointValue = 1;

    [Header("Visual Effects")]
    public bool rotateObject = true;
    public float rotationSpeed = 90f;
    public bool enablePulseEffect = true;
    public float pulseIntensity = 0.1f;
    public float pulseSpeed = 2f;

    [Header("Audio")]
    public AudioClip collectSound;
    public float soundVolume = 1f;
}

[AddComponentMenu("Game/CollectibleController")]
public class CollectibleController : MonoBehaviour
{
    [Header("Collectible Configuration")]
    [SerializeField] private CollectibleData collectibleData;

    [Header("Visual Components")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private Light itemLight;
    [SerializeField] private float flashMultiplier = 3f;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float flashHoldTime = 0.1f;

    [Header("Physics")]
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private bool autoSetupCollider = true;

    [Header("Events")]
    [SerializeField] private UnityEvent OnCollected;
    [SerializeField] private UnityEvent<CollectibleData> OnCollectedWithData;

    private AudioSource audioSource;
    private Vector3 originalScale;
    private bool isCollecting = false;
    private bool isCollected = false;
    private float pulseTimer = 0f;
    private readonly object lockObject = new object();

    public System.Action<CollectibleController> OnCollectiblePickedUp;

    public CollectibleData Data => collectibleData;
    public bool IsCollected => isCollected;
    public string ItemName => collectibleData?.itemName ?? gameObject.name;
    public int PointValue => collectibleData?.pointValue ?? 1;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        ValidateSetup();
        originalScale = transform.localScale;

        if (LevelManager.Instance != null && !IsCollected && !LevelManager.Instance.ContainsCollectible(this))
        {
            LevelManager.Instance.AddCollectible(this);
        }
    }

    void Update()
    {
        if (!isCollected)
        {
            RotateLocally();
            HandlePulseEffect();
        }
    }

    private void InitializeComponents()
    {
        // Renderer
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        // Partikeleffekt
        if (!collectEffect)
            collectEffect = GetComponentInChildren<ParticleSystem>();

        // Licht
        if (!itemLight)
            itemLight = GetComponentInChildren<Light>();

        // AudioSource automatisch hinzufügen
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 20f;

        // Collider automatisch hinzufügen
        if (autoSetupCollider)
        {
            triggerCollider = GetComponent<Collider>();
            if (!triggerCollider)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.5f;
                triggerCollider = sphere;
            }
            triggerCollider.isTrigger = true;
        }
    }

    private void ValidateSetup()
    {
        if (collectibleData == null)
            collectibleData = new CollectibleData { itemName = gameObject.name };

        if (triggerCollider && !triggerCollider.isTrigger)
            triggerCollider.isTrigger = true;

        if (!gameObject.CompareTag("Collectible"))
            gameObject.tag = "Collectible";
    }

    private void RotateLocally()
    {
        if (collectibleData?.rotateObject != true) return;
        transform.localRotation *= Quaternion.Euler(0f, collectibleData.rotationSpeed * Time.deltaTime, 0f);
    }

    private void HandlePulseEffect()
    {
        if (collectibleData?.enablePulseEffect != true) return;
        pulseTimer += Time.deltaTime * collectibleData.pulseSpeed;
        float pulseValue = 1f + Mathf.Sin(pulseTimer) * collectibleData.pulseIntensity;
        transform.localScale = originalScale * pulseValue;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollecting || isCollected) return;
        if (other.GetComponent<PlayerController>() != null)
            CollectItem();
    }

    public void CollectItem()
    {
        lock (lockObject)
        {
            if (isCollecting || isCollected) return;
            isCollecting = true;
            isCollected = true;
        }

        PlayCollectionSound();
        TriggerCollectionEffect();
        OnCollected?.Invoke();
        OnCollectedWithData?.Invoke(collectibleData);

        try
        {
            OnCollectiblePickedUp?.Invoke(this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CollectibleController] Error firing event for {gameObject.name}: {e.Message}");
        }

        StartCoroutine(CollectionSequence());
    }

    private void PlayCollectionSound()
    {
        if (collectibleData?.collectSound && audioSource)
        {
            audioSource.clip = collectibleData.collectSound;
            audioSource.volume = collectibleData.soundVolume;
            audioSource.Play();
        }
        else if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySound("Collect");
        }
    }

    private void TriggerCollectionEffect()
    {
        if (collectEffect) collectEffect.Play();
        if (itemLight) StartCoroutine(FlashLight());
    }

    private IEnumerator FlashLight()
    {
        if (!itemLight) yield break;

        float originalIntensity = itemLight.intensity;
        itemLight.intensity = originalIntensity * flashMultiplier;

        yield return new WaitForSeconds(flashHoldTime);

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            itemLight.intensity = Mathf.Lerp(originalIntensity * flashMultiplier, 0f, elapsed / flashDuration);
            yield return null;
        }

        itemLight.intensity = 0f;
    }

    private IEnumerator CollectionSequence()
    {
        if (triggerCollider) triggerCollider.enabled = false;

        float scaleTime = 0.2f;
        float elapsed = 0f;
        Vector3 targetScale = originalScale * 1.3f;

        while (elapsed < scaleTime)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / scaleTime);
            yield return null;
        }

        elapsed = 0f;
        float fadeTime = 0.3f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
            foreach (Renderer renderer in renderers)
            {
                if (renderer && renderer.material)
                {
                    Color color = renderer.material.color;
                    color.a = Mathf.Lerp(1f, 0f, t);
                    renderer.material.color = color;
                }
            }
            yield return null;
        }

        if (audioSource && audioSource.isPlaying)
            yield return new WaitWhile(() => audioSource.isPlaying);

        PrefabPooler.Release(gameObject);
    }

    public void ResetCollectible()
    {
        lock (lockObject)
        {
            isCollected = false;
            isCollecting = false;
        }

        if (triggerCollider) triggerCollider.enabled = true;
        transform.localScale = originalScale;

        foreach (Renderer renderer in renderers)
        {
            if (renderer && renderer.material)
            {
                Color color = renderer.material.color;
                color.a = 1f;
                renderer.material.color = color;
            }
        }

        if (LevelManager.Instance != null && !LevelManager.Instance.ContainsCollectible(this))
        {
            LevelManager.Instance.AddCollectible(this);
        }
    }
}
