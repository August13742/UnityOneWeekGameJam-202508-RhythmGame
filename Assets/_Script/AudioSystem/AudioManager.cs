using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Rhythm.Core;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get; private set;
    }
    public static event System.Action<double> OnMusicStartConfirmed;

    [Header("Mixer Groups")]
    public AudioMixer mixer;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Settings")]
    [SerializeField] private int sfxPoolSize = 10;

    private List<AudioSource> sfxPlayerPool = new List<AudioSource>();
    private Dictionary<string, AudioSource> loopingSfxPlayers = new Dictionary<string, AudioSource>();
    private List<AudioSource> pausedSfxPlayers = new List<AudioSource>();

    private AudioSource musicPlayer;
    private float currentMusicVolume = 1f;
    
    // Music pause state tracking
    private bool isMusicPaused = false;
    private double musicPauseTime;
    private double musicResumeTime;
    private double scheduledStartTime;
    private bool wasScheduledToPlay = false;
    private float duckFactor = 1f;        
    private Coroutine duckCoroutine;
    public float MusicVolume
    {
        get
        {
            return currentMusicVolume * duckFactor;
        }
    }

    // Always compute: applied = currentMusicVolume * duckFactor
    private void ApplyMusicVolume()
    {
        if (musicPlayer != null)
            musicPlayer.volume = currentMusicVolume * duckFactor;
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAudioPool();
        CreateMusicPlayer();
    }
    private IEnumerator TryBindGameEvents()
    {
        while (GameEvents.Instance == null)
            yield return null;
        GameEvents.Instance.OnPlayMusicScheduled += HandlePlayMusicScheduled;
    }
    private void Start()
    {
        if (GameEvents.Instance != null)
            GameEvents.Instance.OnPlayMusicScheduled += HandlePlayMusicScheduled;
        else
            StartCoroutine(TryBindGameEvents());
    }
    private void OnDisable()
    {
        if (GameEvents.Instance != null)
            GameEvents.Instance.OnPlayMusicScheduled -= HandlePlayMusicScheduled;
    }


    void InitializeAudioPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxPlayerPool.Add(CreateAudioSource("SFX Player"));
        }
    }

    void CreateMusicPlayer()
    {
        musicPlayer = CreateAudioSource("Music Player");
        musicPlayer.loop = true;
        musicPlayer.outputAudioMixerGroup = musicGroup;
    }

    AudioSource CreateAudioSource(string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    private void HandlePlayMusicScheduled(AudioClip clip, double dspTime)
    {
        if (musicPlayer == null)
        {
            Debug.LogError("[AudioManager] Music Player is not initialized!");
            return;
        }

        if (clip == null)
        {
            Debug.LogError("[AudioManager] Trying to play null audio clip!");
            return;
        }

        // Stop any currently playing music
        if (musicPlayer.isPlaying)
        {
            musicPlayer.Stop();
        }

        // Reset pause state
        isMusicPaused = false;
        wasScheduledToPlay = true;
        scheduledStartTime = dspTime;

        musicPlayer.clip = clip;
        ApplyMusicVolume();

        double currentDspTime = AudioSettings.dspTime;

        // Never play "now". If requested time is too soon, push both sides forward.
        double minLead = 0.20; // 200 ms guard so scheduling is reliable on all machines
        double safeStartTime = (dspTime <= currentDspTime + minLead)
            ? currentDspTime + minLead
            : dspTime;

        wasScheduledToPlay = true;
        scheduledStartTime = safeStartTime;

        musicPlayer.clip = clip;
        ApplyMusicVolume();
        musicPlayer.PlayScheduled(safeStartTime);

        Debug.Log($"[AudioManager] Scheduled '{clip.name}' @ {safeStartTime:F6} (req {dspTime:F6}, now {currentDspTime:F6})");

        // Tell gameplay the *actual* start time to lock onto.
        OnMusicStartConfirmed?.Invoke(safeStartTime);
    }

    public void PauseMusic()
    {
        if (musicPlayer == null || isMusicPaused)
            return;

        if (musicPlayer.isPlaying)
        {
            musicPauseTime = AudioSettings.dspTime;
            musicPlayer.Pause();
            isMusicPaused = true;
            Debug.Log($"[AudioManager] Music paused at DSP time: {musicPauseTime}");
        }
        else
        {
            // Music was scheduled but not yet playing
            isMusicPaused = true;
            musicPauseTime = AudioSettings.dspTime;
            Debug.Log("[AudioManager] Music scheduled playback paused");
        }
    }

    public void ResumeMusic()
    {
        if (musicPlayer == null || !isMusicPaused)
            return;

        musicResumeTime = AudioSettings.dspTime;
        double pauseDuration = musicResumeTime - musicPauseTime;

        if (musicPlayer.clip != null)
        {
            if (wasScheduledToPlay)
            {
                // If music was originally scheduled, reschedule it with the pause duration added
                double newScheduledTime = scheduledStartTime + pauseDuration;
                double currentTime = AudioSettings.dspTime;
                
                if (newScheduledTime <= currentTime + 0.05) // If scheduled time is too close or in the past
                {
                    Debug.Log("[AudioManager] Resuming music immediately");
                    musicPlayer.UnPause();
                }
                else
                {
                    Debug.Log($"[AudioManager] Rescheduling music to DSP time: {newScheduledTime}");
                    musicPlayer.Stop(); // Stop any current state
                    musicPlayer.PlayScheduled(newScheduledTime);
                }
            }
            else
            {
                // Music was already playing when paused
                musicPlayer.UnPause();
                Debug.Log($"[AudioManager] Music resumed at DSP time: {musicResumeTime} (was paused for {pauseDuration:F3}s)");
            }
        }

        isMusicPaused = false;
    }

    public void StopMusic()
    {
        if (musicPlayer != null)
        {
            musicPlayer.Stop();
            isMusicPaused = false;
            wasScheduledToPlay = false;
        }
        // reset duck
        if (duckCoroutine != null)
            StopCoroutine(duckCoroutine);
        duckFactor = 1f;
        ApplyMusicVolume();
    }

    public bool IsMusicPaused => isMusicPaused;
    public bool IsMusicPlaying => musicPlayer != null && musicPlayer.isPlaying && !isMusicPaused;

    public void PlaySFX(SFXResource resource)
    {
        if (resource == null || resource.clip == null)
        {
            Debug.LogWarning("[AudioManager] PlaySFX called with null resource/clip.");
            return;
        }
        if (resource.loop)
            PlayLoopingSFX(resource);
        else
            PlayOneShotSFX(resource.clip, resource.volumeLinear, resource.pitchScale);
    }


    void PlayOneShotSFX(AudioClip clip, float volume, float pitch)
    {
        AudioSource player = GetAvailableSFXPlayer();

        player.clip = clip;
        player.volume = volume;
        player.pitch = pitch;
        player.loop = false;
        player.outputAudioMixerGroup = sfxGroup;
        player.Play();
    }

    AudioSource GetAvailableSFXPlayer()
    {
        foreach (AudioSource player in sfxPlayerPool)
        {
            if (!player.isPlaying)
                return player;
        }

        // Expand pool if needed
        Debug.LogWarning("[AudioManager] SFX pool exhausted. Expanding pool size.");
        AudioSource newPlayer = CreateAudioSource("SFX Player (Dynamic)");
        sfxPlayerPool.Add(newPlayer);
        return newPlayer;
    }

    void PlayLoopingSFX(SFXResource resource)
    {
        string eventName = resource.eventName;

        // Stop existing instance
        if (loopingSfxPlayers.TryGetValue(eventName, out AudioSource existing))
        {
            if (existing != null)
            {
                existing.Stop();
                Destroy(existing.gameObject);
            }
            loopingSfxPlayers.Remove(eventName);
        }

        // Create new player
        AudioSource player = CreateAudioSource($"Loop: {eventName}");
        player.clip = resource.clip;
        player.volume = resource.volumeLinear;
        player.pitch = resource.pitchScale;
        player.loop = true;
        player.outputAudioMixerGroup = sfxGroup;
        player.Play();

        loopingSfxPlayers[eventName] = player;
    }

    public void StopLoopedSFX(string eventName, float fadeOutDuration = 0f)
    {
        if (!loopingSfxPlayers.TryGetValue(eventName, out AudioSource player))
        {
            Debug.LogWarning($"[AudioManager] Looped SFX not found: {eventName}");
            return;
        }

        loopingSfxPlayers.Remove(eventName);

        if (fadeOutDuration > 0.01f)
        {
            StartCoroutine(FadeOutAndDestroy(player, fadeOutDuration));
        }
        else
        {
            Destroy(player.gameObject);
        }
    }

    IEnumerator FadeOutAndDestroy(AudioSource player, float duration)
    {
        float startVolume = player.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            player.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        Destroy(player.gameObject);
    }

    public void PlayMusic(MusicResource resource, float fadeDuration = 2f)
    {
        if (musicPlayer.clip == resource.clip && musicPlayer.isPlaying)
            return;

        StartCoroutine(CrossFadeMusic(resource.clip, fadeDuration));
    }

    IEnumerator CrossFadeMusic(AudioClip newClip, float duration)
    {
        float half = duration * 0.5f;

        if (musicPlayer.isPlaying)
        {
            yield return FadeAudioSource(musicPlayer, musicPlayer.volume, 0f, half);
            musicPlayer.Stop();
        }

        musicPlayer.clip = newClip;
        musicPlayer.Play();

        // live duck-aware fade-in
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float target = currentMusicVolume * duckFactor;
            musicPlayer.volume = Mathf.Lerp(0f, target, t / half);
            yield return null;
        }
        ApplyMusicVolume();
    }


    IEnumerator FadeAudioSource(AudioSource source, float startVol, float endVol, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVol, endVol, t / duration);
            yield return null;
        }
        source.volume = endVol;
    }


    public void SetMusicVolume(float volumeLinear)
    {
        currentMusicVolume = volumeLinear;
        ApplyMusicVolume();
    }


    public void FadeMusic(float lowerThreshold = 0.2f, float fadeDuration = 0.5f)
    {
        StartDuck(Mathf.Clamp01(lowerThreshold), fadeDuration);
    }

    public void UnfadeMusic(float fadeDuration = 0.5f)
    {
        StartDuck(1f, fadeDuration);
    }
    private void StartDuck(float target, float duration)
    {
        if (duckCoroutine != null)
            StopCoroutine(duckCoroutine);
        duckCoroutine = StartCoroutine(DuckRoutine(target, duration));
    }
    private IEnumerator DuckRoutine(float target, float duration)
    {
        float start = duckFactor;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;          // unaffected by Time.timeScale
            duckFactor = Mathf.Lerp(start, target, t / duration);
            ApplyMusicVolume();
            yield return null;
        }
        duckFactor = target;
        ApplyMusicVolume();
        duckCoroutine = null;
    }
    public void PauseSFX()
    {
        pausedSfxPlayers.Clear();

        foreach (AudioSource player in sfxPlayerPool)
        {
            if (player.isPlaying)
            {
                player.Pause();
                pausedSfxPlayers.Add(player);
            }
        }

        foreach (AudioSource player in loopingSfxPlayers.Values)
        {
            if (player.isPlaying)
            {
                player.Pause();
                pausedSfxPlayers.Add(player);
            }
        }
    }

    public void ResumeSFX()
    {
        foreach (AudioSource player in pausedSfxPlayers)
        {
            player.UnPause();
        }
        pausedSfxPlayers.Clear();
    }

    // Volume control
    public void SetBusVolume(string busName, float volumeLinear)
    {
        if (mixer == null)
            return;

        // Convert to dB (-80dB is silence)
        float dB = volumeLinear > 0.001f ?
            20f * Mathf.Log10(volumeLinear) :
            -80f;

        mixer.SetFloat(busName, dB);
    }
}
