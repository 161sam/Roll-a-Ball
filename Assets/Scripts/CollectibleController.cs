using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controller für sammelbare Objekte im Roll-a-Ball Spiel
/// Handles collection logic, audio feedback, and visual effects
/// </summary>
[System.Serializable]
public class CollectibleData
{
    [Header("Collectible Properties")]
    public string itemName = "Collectible";
    public string itemType = "standard";
    public int pointValue = 1;
    public bool isCollected = false;

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

    [Header("Physics")]
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private bool autoSetupCollider = true;

    [Header("Events")]
    [SerializeField] private UnityEvent OnCollected;
    [SerializeField] private UnityEvent<CollectibleData> OnCollectedWithData;

    // Private fields
    private AudioSource audioSource;
    private Vector3 originalScale;
    private bool isCollecting = false;
    private float pulseTimer = 0f;

    // Public Events für GameManager Integration
    public System.Action<CollectibleController> OnCollectiblePickedUp;

    // Properties
    public CollectibleData Data => collectibleData;
    public bool IsCollected => collectibleData.isCollected;
    public string ItemName => collectibleData.itemName;
    public int PointValue => collectibleData.pointValue;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        ValidateSetup();
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (!collectibleData.isCollected)
        {
            HandleRotation();
            HandlePulseEffect();
        }
    }

    private void InitializeComponents()
    {
        // Auto-find components if not assigned
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        if (!collectEffect)
            collectEffect = GetComponentInChildren<ParticleSystem>();

        if (!itemLight)
            itemLight = GetComponentInChildren<Light>();

        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 20f;
        }

        // Setup Collider
        if (autoSetupCollider && !triggerCollider)
        {
            triggerCollider = GetComponent<Collider>();
            if (!triggerCollider)
            {
                // Add SphereCollider as default
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.5f;
                triggerCollider = sphere;
            }
            else
            {
                triggerCollider.isTrigger = true;
            }
        }
    }

    private void ValidateSetup()
    {
        if (collectibleData == null)
        {
            collectibleData = new CollectibleData();
            collectibleData.itemName = gameObject.name;
        }

        if (triggerCollider && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"CollectibleController on {gameObject.name}: Collider should be a trigger!");
            triggerCollider.isTrigger = true;
        }

        // Ensure object has Collectible tag
        if (!gameObject.CompareTag("Collectible"))
        {
            #if UNITY_EDITOR
            if (System.Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, t => t == "Collectible"))
            {
                gameObject.tag = "Collectible";
            }
            else
            {
                Debug.LogWarning($"CollectibleController on {gameObject.name}: 'Collectible' tag not found. Please add it in Project Settings > Tags and Layers");
            }
            #else
            gameObject.tag = "Collectible";
            #endif
        }
    }

    private void HandleRotation()
    {
        if (collectibleData.rotateObject)
        {
            // Rotate around the global Y-axis so parent rotations do not affect the spin
            transform.Rotate(Vector3.up, collectibleData.rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandlePulseEffect()
    {
        if (!collectibleData.enablePulseEffect) return;

        pulseTimer += Time.deltaTime * collectibleData.pulseSpeed;
        float pulseValue = 1f + Mathf.Sin(pulseTimer) * collectibleData.pulseIntensity;
        
        transform.localScale = originalScale * pulseValue;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollecting || collectibleData.isCollected) return;

        // Check if it's the player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            CollectItem();
        }
    }

    public void CollectItem()
    {
        if (isCollecting || collectibleData.isCollected) return;

        isCollecting = true;
        collectibleData.isCollected = true;

        // Play sound effect
        PlayCollectionSound();

        // Trigger particle effect
        TriggerCollectionEffect();

        // Invoke events
        OnCollected?.Invoke();
        OnCollectedWithData?.Invoke(collectibleData);
        OnCollectiblePickedUp?.Invoke(this);

        // Notify GameManager
        if (LevelManager.Instance)
        {
            LevelManager.Instance.OnCollectibleCollected(this);
        }

        // Visual feedback and destruction
        StartCoroutine(CollectionSequence());
    }

    private void PlayCollectionSound()
    {
        if (collectibleData.collectSound && audioSource)
        {
            audioSource.clip = collectibleData.collectSound;
            audioSource.volume = collectibleData.soundVolume;
            audioSource.Play();
        }
        else if (AudioManager.Instance)
        {
            // Fallback to AudioManager
            AudioManager.Instance.PlaySound("Collect");
        }
    }

    private void TriggerCollectionEffect()
    {
        if (collectEffect)
        {
            collectEffect.Play();
        }

        // Light flash effect
        if (itemLight)
        {
            StartCoroutine(FlashLight());
        }
    }

    private System.Collections.IEnumerator FlashLight()
    {
        if (!itemLight) yield break;

        float originalIntensity = itemLight.intensity;
        itemLight.intensity = originalIntensity * 3f;
        // TODO: Expose flash intensity and duration as serialized fields

        yield return new WaitForSeconds(0.1f);

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            itemLight.intensity = Mathf.Lerp(originalIntensity * 3f, 0f, elapsed / duration);
            yield return null;
        }

        itemLight.intensity = 0f;
    }

    private System.Collections.IEnumerator CollectionSequence()
    {
        // Disable collider immediately
        if (triggerCollider)
            triggerCollider.enabled = false;

        // Scale up briefly
        float scaleTime = 0.2f;
        float elapsed = 0f;
        Vector3 targetScale = originalScale * 1.3f;

        while (elapsed < scaleTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale down and fade out
        elapsed = 0f;
        float fadeTime = 0.3f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            
            // Scale down
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
            
            // Fade out materials
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

        // Wait for sound to finish
        if (audioSource && audioSource.isPlaying)
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        // Destroy object
        Destroy(gameObject);
    }

    // Public utility methods
    public void SetCollectibleData(CollectibleData data)
    {
        collectibleData = data;
        if (collectibleData != null && !string.IsNullOrEmpty(collectibleData.itemName))
        {
            gameObject.name = collectibleData.itemName;
        }
    }

    public void ForceCollect()
    {
        CollectItem();
    }

    public void ResetCollectible()
    {
        collectibleData.isCollected = false;
        isCollecting = false;
        
        if (triggerCollider)
            triggerCollider.enabled = true;
        
        transform.localScale = originalScale;
        
        // Reset material alpha
        foreach (Renderer renderer in renderers)
        {
            if (renderer && renderer.material)
            {
                Color color = renderer.material.color;
                color.a = 1f;
                renderer.material.color = color;
            }
        }

        // TODO: Return collectible to object pool instead of keeping in scene
    }

    // Debug
    void OnDrawGizmosSelected()
    {
        if (triggerCollider)
        {
            Gizmos.color = collectibleData.isCollected ? Color.gray : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (triggerCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (triggerCollider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
