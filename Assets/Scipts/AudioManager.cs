using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SoundEffect
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool randomizePitch = false;
    [Range(0f, 0.5f)] public float pitchVariation = 0.1f;
}

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixerGroup masterMixer;
    [SerializeField] private AudioMixerGroup sfxMixer;
    [SerializeField] private AudioMixerGroup musicMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource[] sfxSources;
    [SerializeField] private int poolSize = 10;

    [Header("Player Sound Effects")]
    [SerializeField] private SoundEffect[] playerSounds = {
        new SoundEffect { name = "Jump" },
        new SoundEffect { name = "DoubleJump" },
        new SoundEffect { name = "Land" },
        new SoundEffect { name = "Sprint" },
        new SoundEffect { name = "Slide" },
        new SoundEffect { name = "Fly" }
    };

    [Header("Environment Sounds")]
    [SerializeField] private SoundEffect[] environmentSounds;

    [Header("Music")]
    [SerializeField] private AudioClip[] backgroundMusic;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool shuffleMusic = true;
    [SerializeField] private float musicFadeDuration = 2f;

    [Header("3D Audio Settings")]
    [SerializeField] private float dopplerLevel = 1f;
    [SerializeField] private float rolloffDistance = 50f;
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    // Private fields
    private Dictionary<string, SoundEffect> soundDictionary;
    private Queue<AudioSource> availableSources;
    private List<AudioSource> allSources;
    private int currentMusicIndex = 0;
    private Coroutine musicFadeCoroutine;

    // Singleton pattern
    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playMusicOnStart && backgroundMusic.Length > 0)
            PlayRandomMusic();
    }

    private void InitializeAudioManager()
    {
        CreateAudioSourcePool();
        BuildSoundDictionary();
        SetupAudioSettings();
    }

    private void CreateAudioSourcePool()
    {
        availableSources = new Queue<AudioSource>();
        allSources = new List<AudioSource>();

        // Create main music source if not assigned
        if (!musicSource)
        {
            GameObject musicObj = new GameObject("Music Source");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.outputAudioMixerGroup = musicMixer;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        // Create SFX source pool
        if (sfxSources == null || sfxSources.Length == 0)
        {
            sfxSources = new AudioSource[poolSize];
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX Source {i}");
                sfxObj.transform.SetParent(transform);
                
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = sfxMixer;
                source.playOnAwake = false;
                
                sfxSources[i] = source;
                availableSources.Enqueue(source);
                allSources.Add(source);
            }
        }
    }

    private void BuildSoundDictionary()
    {
        soundDictionary = new Dictionary<string, SoundEffect>();

        // Add player sounds
        foreach (var sound in playerSounds)
        {
            if (!string.IsNullOrEmpty(sound.name))
                soundDictionary[sound.name] = sound;
        }

        // Add environment sounds
        foreach (var sound in environmentSounds)
        {
            if (!string.IsNullOrEmpty(sound.name))
                soundDictionary[sound.name] = sound;
        }
    }

    private void SetupAudioSettings()
    {
        // Configure all sources with 3D settings including doppler
        foreach (var source in allSources)
        {
            source.rolloffMode = rolloffMode;
            source.maxDistance = rolloffDistance;
            source.spatialBlend = 1f; // 3D sound
            source.dopplerLevel = dopplerLevel; // Set doppler per source
        }
    }

    // ===== Public Interface =====

    public void PlaySound(string soundName, Vector3? position = null, Transform parent = null)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"AudioManager: Sound '{soundName}' not found!");
            return;
        }

        SoundEffect sound = soundDictionary[soundName];
        if (!sound.clip)
        {
            Debug.LogWarning($"AudioManager: No clip assigned to sound '{soundName}'!");
            return;
        }

        AudioSource source = GetAvailableSource();
        if (!source) return;

        // Configure audio source
        source.clip = sound.clip;
        source.volume = sound.volume;
        source.pitch = sound.randomizePitch ? 
            sound.pitch + Random.Range(-sound.pitchVariation, sound.pitchVariation) : 
            sound.pitch;
        source.dopplerLevel = dopplerLevel; // Ensure doppler is set

        // Set position
        if (position.HasValue)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f; // 3D
        }
        else
        {
            source.spatialBlend = 0f; // 2D
        }

        // Set parent
        if (parent)
            source.transform.SetParent(parent);
        else
            source.transform.SetParent(transform);

        // Play sound
        source.Play();

        // Return to pool when finished
        StartCoroutine(ReturnSourceToPool(source, sound.clip.length / source.pitch));
    }

    public void PlaySoundAtPlayer(string soundName)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player)
            PlaySound(soundName, player.transform.position, player.transform);
        else
            PlaySound(soundName);
    }

    public void StopSound(string soundName)
    {
        foreach (var source in allSources)
        {
            if (source.isPlaying && soundDictionary.ContainsKey(soundName) && 
                source.clip == soundDictionary[soundName].clip)
            {
                source.Stop();
            }
        }
    }

    public void StopAllSounds()
    {
        foreach (var source in allSources)
        {
            if (source.isPlaying)
                source.Stop();
        }
    }

    // ===== Music Control =====

    public void PlayMusic(AudioClip clip, bool fadeIn = true)
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        if (fadeIn && musicSource.isPlaying)
            musicFadeCoroutine = StartCoroutine(FadeToNewMusic(clip));
        else
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void PlayRandomMusic()
    {
        if (backgroundMusic.Length == 0) return;

        if (shuffleMusic)
            currentMusicIndex = Random.Range(0, backgroundMusic.Length);
        
        PlayMusic(backgroundMusic[currentMusicIndex]);
        
        currentMusicIndex = (currentMusicIndex + 1) % backgroundMusic.Length;
    }

    public void StopMusic(bool fadeOut = true)
    {
        if (fadeOut)
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSFXVolume(float volume)
    {
        // This would ideally control the mixer group volume
        foreach (var source in allSources)
        {
            source.volume = Mathf.Clamp01(volume);
        }
    }

    // ===== Volume Control (using Audio Mixer) =====

    public void SetMasterVolume(float volume)
    {
        if (masterMixer)
        {
            float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            masterMixer.audioMixer.SetFloat("MasterVolume", dbValue);
        }
    }

    public void SetSFXMixerVolume(float volume)
    {
        if (sfxMixer)
        {
            float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            sfxMixer.audioMixer.SetFloat("SFXVolume", dbValue);
        }
    }

    public void SetMusicMixerVolume(float volume)
    {
        if (musicMixer)
        {
            float dbValue = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            musicMixer.audioMixer.SetFloat("MusicVolume", dbValue);
        }
    }

    // ===== Private Methods =====

    private AudioSource GetAvailableSource()
    {
        if (availableSources.Count > 0)
            return availableSources.Dequeue();

        // If no sources available, find one that's not playing
        foreach (var source in allSources)
        {
            if (!source.isPlaying)
                return source;
        }

        Debug.LogWarning("AudioManager: No available audio sources!");
        return null;
    }

    private IEnumerator ReturnSourceToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (source)
        {
            source.Stop();
            source.transform.SetParent(transform);
            availableSources.Enqueue(source);
        }
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip)
    {
        float startVolume = musicSource.volume;
        
        // Fade out current music
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / musicFadeDuration;
            yield return null;
        }
        
        // Switch to new clip
        musicSource.clip = newClip;
        musicSource.Play();
        
        // Fade in new music
        while (musicSource.volume < startVolume)
        {
            musicSource.volume += startVolume * Time.deltaTime / musicFadeDuration;
            yield return null;
        }
        
        musicSource.volume = startVolume;
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / musicFadeDuration;
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.volume = startVolume;
        musicFadeCoroutine = null;
    }

    // ===== Event Integration =====

    void OnEnable()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player)
        {
            player.OnGroundedChanged += OnPlayerGroundedChanged;
            player.OnFlyingChanged += OnPlayerFlyingChanged;
        }
    }

    void OnDisable()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player)
        {
            player.OnGroundedChanged -= OnPlayerGroundedChanged;
            player.OnFlyingChanged -= OnPlayerFlyingChanged;
        }
    }

    private void OnPlayerGroundedChanged(bool grounded)
    {
        if (grounded)
            PlaySoundAtPlayer("Land");
    }

    private void OnPlayerFlyingChanged(bool flying)
    {
        if (flying)
            PlaySoundAtPlayer("Fly");
    }

    // ===== Utility Methods =====

    public bool IsSoundPlaying(string soundName)
    {
        if (!soundDictionary.ContainsKey(soundName))
            return false;

        AudioClip targetClip = soundDictionary[soundName].clip;
        foreach (var source in allSources)
        {
            if (source.isPlaying && source.clip == targetClip)
                return true;
        }
        
        return false;
    }

    public void AddSound(SoundEffect newSound)
    {
        if (!string.IsNullOrEmpty(newSound.name))
            soundDictionary[newSound.name] = newSound;
    }

    public void RemoveSound(string soundName)
    {
        if (soundDictionary.ContainsKey(soundName))
            soundDictionary.Remove(soundName);
    }
}