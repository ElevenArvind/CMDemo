using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        public bool loop = false;
    }

    public enum SoundType
    {
        CardFlip,
        MatchFound,
        MistakeFound,
        Victory
    }

    [Header("Sound Effects")]
    public SoundEffect[] soundEffects;
    
    [Header("Audio Sources Pool")]
    [SerializeField] private int audioSourcePoolSize = 10;
    
    [Header("Sound Throttling")]
    [SerializeField] private float soundThrottleTime = 0.1f; // Minimum time between same sound plays
    
    private Dictionary<SoundType, SoundEffect> soundDictionary;
    private Queue<AudioSource> audioSourcePool;
    private List<AudioSource> activeAudioSources;
    private Dictionary<SoundType, float> lastSoundPlayTime;
    
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
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

    private void InitializeAudioManager()
    {
        // Create sound dictionary for fast lookup
        soundDictionary = new Dictionary<SoundType, SoundEffect>();
        
        foreach (var sound in soundEffects)
        {
            if (System.Enum.TryParse(sound.name, out SoundType soundType))
            {
                soundDictionary[soundType] = sound;
            }
        }

        // Initialize sound throttling dictionary
        lastSoundPlayTime = new Dictionary<SoundType, float>();

        // Initialize audio source pool
        audioSourcePool = new Queue<AudioSource>();
        activeAudioSources = new List<AudioSource>();

        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioSourceObj = new GameObject($"AudioSource_{i}");
            audioSourceObj.transform.SetParent(transform);
            AudioSource audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSourcePool.Enqueue(audioSource);
        }
    }

    public void PlaySound(SoundType soundType)
    {
        if (!soundDictionary.ContainsKey(soundType))
        {
            Debug.LogWarning($"Sound {soundType} not found in AudioManager!");
            return;
        }

        // Check if sound should be throttled
        if (ShouldThrottleSound(soundType))
        {
            return;
        }

        SoundEffect sound = soundDictionary[soundType];
        
        if (sound.clip == null)
        {
            Debug.LogWarning($"AudioClip for {soundType} is null!");
            return;
        }

        // Update last play time
        lastSoundPlayTime[soundType] = Time.time;

        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource != null)
        {
            PlaySoundOnSource(audioSource, sound);
        }
    }

    public void PlaySoundWithDelay(SoundType soundType, float delay)
    {
        StartCoroutine(PlaySoundDelayed(soundType, delay));
    }

    private IEnumerator PlaySoundDelayed(SoundType soundType, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(soundType);
    }

    private bool ShouldThrottleSound(SoundType soundType)
    {
        // Allow CardFlip sounds to play without throttling for responsive feedback
        if (soundType == SoundType.CardFlip)
        {
            return false;
        }

        // Check if we have a record of this sound being played
        if (!lastSoundPlayTime.ContainsKey(soundType))
        {
            return false; // First time playing this sound
        }

        // Check if enough time has passed since last play
        float timeSinceLastPlay = Time.time - lastSoundPlayTime[soundType];
        return timeSinceLastPlay < soundThrottleTime;
    }

    private AudioSource GetAvailableAudioSource()
    {
        // Try to get from pool first
        if (audioSourcePool.Count > 0)
        {
            AudioSource audioSource = audioSourcePool.Dequeue();
            activeAudioSources.Add(audioSource);
            return audioSource;
        }

        // If pool is empty, find an inactive one from active sources
        for (int i = 0; i < activeAudioSources.Count; i++)
        {
            if (!activeAudioSources[i].isPlaying)
            {
                return activeAudioSources[i];
            }
        }

        // If all are busy, create a new temporary one (for edge cases)
        Debug.LogWarning("All audio sources are busy, creating temporary AudioSource");
        GameObject tempObj = new GameObject("TempAudioSource");
        tempObj.transform.SetParent(transform);
        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.playOnAwake = false;
        
        // Destroy after playing
        StartCoroutine(DestroyTempAudioSource(tempSource, 5f));
        
        return tempSource;
    }

    private void PlaySoundOnSource(AudioSource audioSource, SoundEffect sound)
    {
        audioSource.clip = sound.clip;
        audioSource.volume = sound.volume;
        audioSource.pitch = sound.pitch;
        audioSource.loop = sound.loop;
        audioSource.Play();

        if (!sound.loop)
        {
            StartCoroutine(ReturnAudioSourceToPool(audioSource, sound.clip.length / sound.pitch));
        }
    }

    private IEnumerator ReturnAudioSourceToPool(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (activeAudioSources.Contains(audioSource))
        {
            activeAudioSources.Remove(audioSource);
            audioSourcePool.Enqueue(audioSource);
        }
    }

    private IEnumerator DestroyTempAudioSource(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null)
        {
            Destroy(audioSource.gameObject);
        }
    }

    public void StopAllSounds()
    {
        foreach (var audioSource in activeAudioSources)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    public void StopSound(SoundType soundType)
    {
        if (!soundDictionary.ContainsKey(soundType))
            return;

        AudioClip clipToStop = soundDictionary[soundType].clip;
        
        foreach (var audioSource in activeAudioSources)
        {
            if (audioSource.isPlaying && audioSource.clip == clipToStop)
            {
                audioSource.Stop();
                break; // Stop only the first occurrence
            }
        }
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    public bool IsSoundPlaying(SoundType soundType)
    {
        if (!soundDictionary.ContainsKey(soundType))
            return false;

        AudioClip targetClip = soundDictionary[soundType].clip;
        
        foreach (var audioSource in activeAudioSources)
        {
            if (audioSource.isPlaying && audioSource.clip == targetClip)
            {
                return true;
            }
        }
        
        return false;
    }

    // Method to play sound without throttling (for special cases)
    public void PlaySoundForced(SoundType soundType)
    {
        if (!soundDictionary.ContainsKey(soundType))
        {
            Debug.LogWarning($"Sound {soundType} not found in AudioManager!");
            return;
        }

        SoundEffect sound = soundDictionary[soundType];
        
        if (sound.clip == null)
        {
            Debug.LogWarning($"AudioClip for {soundType} is null!");
            return;
        }

        // Update last play time
        lastSoundPlayTime[soundType] = Time.time;

        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource != null)
        {
            PlaySoundOnSource(audioSource, sound);
        }
    }

    // Quick play methods for common game sounds
    public void PlayCardFlip() => PlaySound(SoundType.CardFlip);
    public void PlayMatchFound() => PlaySound(SoundType.MatchFound);
    public void PlayMistakeFound() => PlaySound(SoundType.MistakeFound);
    public void PlayVictory() => PlaySoundForced(SoundType.Victory); // Victory should always play

    private void Start()
    {
        // Test all sounds are properly assigned
        foreach (SoundType soundType in System.Enum.GetValues(typeof(SoundType)))
        {
            if (!soundDictionary.ContainsKey(soundType) || soundDictionary[soundType].clip == null)
            {
                Debug.LogWarning($"Sound {soundType} is not properly configured in AudioManager!");
            }
        }
    }
}
