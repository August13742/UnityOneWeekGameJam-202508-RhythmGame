using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Rhythm.Core;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public static event System.Action<double> OnMusicStartConfirmed;

    [Header("Mixer Groups")]
    public AudioMixer mixer;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Settings")]
    [SerializeField] private int sfxPoolSize = 10;
    [SerializeField] private float musicCrossfadeDuration = 2.0f;

    // SFX Players
    private List<AudioSource> sfxPlayerPool = new List<AudioSource>();
    private Dictionary<string, AudioSource> loopingSfxPlayers = new Dictionary<string, AudioSource>();
    private List<AudioSource> pausedSfxPlayers = new List<AudioSource>();

    // Music Players (A/B system for crossfading)
    private AudioSource musicPlayerA;
    private AudioSource musicPlayerB;
    private AudioSource activeMusicPlayer;
    private Coroutine musicFadeCoroutine;

    // Music State
    private float currentMusicVolume = 1f;
    private float duckFactor = 1f;
    private Coroutine duckCoroutine;
    
    // Pause/Resume State
    private bool isMusicPaused = false;
    private double musicPauseTime;
    private double scheduledStartTime;
    private bool wasScheduledToPlay = false;
    
    public float MusicVolume => currentMusicVolume * duckFactor;
    public bool IsMusicPaused => isMusicPaused;
    public bool IsMusicPlaying => activeMusicPlayer != null && activeMusicPlayer.isPlaying && !isMusicPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAudioPool();
        CreateMusicPlayers();
    }

    private void Start()
    {
        StartCoroutine(TryBindGameEvents());
    }

    private void OnDisable()
    {
        if (GameEvents.Instance != null)
            GameEvents.Instance.OnPlayMusicScheduled -= HandlePlayMusicScheduled;
    }

    private IEnumerator TryBindGameEvents()
    {
        while (GameEvents.Instance == null)
            yield return null;
        GameEvents.Instance.OnPlayMusicScheduled += HandlePlayMusicScheduled;
    }

    private void InitializeAudioPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxPlayerPool.Add(CreateAudioSource("SFX Player"));
        }
    }

    private void CreateMusicPlayers()
    {
        musicPlayerA = CreateAudioSource("Music Player A");
        musicPlayerA.loop = true;
        musicPlayerA.outputAudioMixerGroup = musicGroup;

        musicPlayerB = CreateAudioSource("Music Player B");
        musicPlayerB.loop = true;
        musicPlayerB.outputAudioMixerGroup = musicGroup;

        activeMusicPlayer = musicPlayerA;
    }

    private AudioSource CreateAudioSource(string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    /// <summary>
    /// Applies the current base volume and ducking factor to the active music player.
    /// </summary>
    private void ApplyMusicVolume()
    {
        if (activeMusicPlayer != null)
            activeMusicPlayer.volume = currentMusicVolume * duckFactor;
    }
    
    // --- Public Music Control ---

    public void PlayMusic(MusicResource resource, float fadeDuration = -1f)
    {
        if (resource == null || resource.clip == null) return;
        if (activeMusicPlayer.clip == resource.clip && activeMusicPlayer.isPlaying) return;

        float duration = (fadeDuration >= 0) ? fadeDuration : musicCrossfadeDuration;
        
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(DoImmediateCrossfade(resource.clip, duration));
    }

    public void StopMusic()
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        musicPlayerA.Stop();
        musicPlayerB.Stop();
        
        isMusicPaused = false;
        wasScheduledToPlay = false;

        // Reset ducking
        if (duckCoroutine != null) StopCoroutine(duckCoroutine);
        duckFactor = 1f;
        ApplyMusicVolume();
    }
    
    public void PauseMusic()
    {
        if (isMusicPaused) return;

        // If a fade is in progress, cancel it.
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        if (activeMusicPlayer.isPlaying)
        {
            musicPauseTime = AudioSettings.dspTime;
            activeMusicPlayer.Pause();
            isMusicPaused = true;
            Debug.Log($"[AudioManager] Music paused at DSP time: {musicPauseTime}");
        }
        else if (wasScheduledToPlay)
        {
            // Music was scheduled but not yet playing
            isMusicPaused = true;
            musicPauseTime = AudioSettings.dspTime;
            // The scheduled play on the source is implicitly paused. We just need to track state.
            Debug.Log("[AudioManager] Music scheduled playback paused.");
        }
    }

    public void ResumeMusic()
    {
        if (!isMusicPaused) return;

        double resumeTime = AudioSettings.dspTime;
        double pauseDuration = resumeTime - musicPauseTime;

        if (wasScheduledToPlay)
        {
            // Reschedule the track to maintain timing relative to the pause.
            double newScheduledTime = scheduledStartTime + pauseDuration;
            activeMusicPlayer.Stop(); // Stop to override the previous schedule.
            activeMusicPlayer.PlayScheduled(newScheduledTime);
            scheduledStartTime = newScheduledTime; // Update the schedule time
            Debug.Log($"[AudioManager] Rescheduling music to DSP time: {newScheduledTime}");
        }
        else
        {
            // Music was playing when paused, so just unpause it.
            activeMusicPlayer.UnPause();
            Debug.Log($"[AudioManager] Music resumed at DSP time: {resumeTime}");
        }

        isMusicPaused = false;
    }

    // --- Event Handlers ---
    
    private void HandlePlayMusicScheduled(AudioClip clip, double dspTime)
    {
        if (clip == null)
        {
            Debug.LogError("[AudioManager] Trying to schedule a null audio clip!");
            return;
        }

        if (musicPlayerA == null || musicPlayerB == null)
        {
            Debug.LogError("[AudioManager] Music Players are not initialized!");
            return;
        }

        // Avoid fading to the same clip if it's already the active one.
        if (activeMusicPlayer.clip == clip)
        {
            Debug.LogWarning($"[AudioManager] Told to schedule the same clip '{clip.name}'. Re-scheduling without crossfade.");
            // A simple reschedule might be better here depending on game logic,
            // but for now we just ensure it plays at the right time.
            activeMusicPlayer.Stop();
            activeMusicPlayer.PlayScheduled(dspTime);
            OnMusicStartConfirmed?.Invoke(dspTime);
            return;
        }
        
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(DoScheduledCrossfade(clip, dspTime, musicCrossfadeDuration));
    }
    
    // --- Coroutines for Fading ---

    private IEnumerator DoScheduledCrossfade(AudioClip newClip, double dspTime, float duration)
    {
        AudioSource fadeInPlayer = (activeMusicPlayer == musicPlayerA) ? musicPlayerB : musicPlayerA;
        AudioSource fadeOutPlayer = activeMusicPlayer;

        // 1. Schedule the new track
        double currentDspTime = AudioSettings.dspTime;
        double minLead = 0.20; // 200ms guard for reliable scheduling
        double safeStartTime = (dspTime <= currentDspTime + minLead) ? currentDspTime + minLead : dspTime;

        fadeInPlayer.clip = newClip;
        fadeInPlayer.volume = 0f;
        fadeInPlayer.PlayScheduled(safeStartTime);

        // 2. Update state immediately
        activeMusicPlayer = fadeInPlayer;
        isMusicPaused = false;
        wasScheduledToPlay = true;
        scheduledStartTime = safeStartTime;

        Debug.Log($"[AudioManager] Scheduling crossfade. New clip '{newClip.name}' @ {safeStartTime:F6}. Fading out '{fadeOutPlayer.clip?.name ?? "nothing"}'.");
        OnMusicStartConfirmed?.Invoke(safeStartTime);
        
        // 3. Start the fade-out on the old player in parallel
        if (fadeOutPlayer.isPlaying)
        {
            StartCoroutine(FadeOutAndStop(fadeOutPlayer, duration));
        }

        // 4. Wait until the new track is supposed to start playing
        yield return new WaitUntil(() => AudioSettings.dspTime >= safeStartTime);
        
        // 5. Fade in the new track
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float targetVolume = currentMusicVolume * duckFactor; // Live ducking
            fadeInPlayer.volume = Mathf.Lerp(0f, targetVolume, progress);
            yield return null;
        }

        ApplyMusicVolume(); // Ensure final volume is correct
        musicFadeCoroutine = null;
    }

    private IEnumerator DoImmediateCrossfade(AudioClip newClip, float duration)
    {
        AudioSource fadeInPlayer = (activeMusicPlayer == musicPlayerA) ? musicPlayerB : musicPlayerA;
        AudioSource fadeOutPlayer = activeMusicPlayer;

        // Start fading out the old track
        if (fadeOutPlayer.isPlaying)
        {
            StartCoroutine(FadeOutAndStop(fadeOutPlayer, duration));
        }
        
        // Set up and play the new track immediately
        fadeInPlayer.clip = newClip;
        fadeInPlayer.volume = 0f;
        fadeInPlayer.Play();
        
        // The new track is now the active one
        activeMusicPlayer = fadeInPlayer;
        wasScheduledToPlay = false;

        // Fade in the new track
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float targetVolume = currentMusicVolume * duckFactor;
            fadeInPlayer.volume = Mathf.Lerp(0f, targetVolume, progress);
            yield return null;
        }

        ApplyMusicVolume();
        musicFadeCoroutine = null;
    }
    
    private IEnumerator FadeOutAndStop(AudioSource source, float duration)
    {
        if (!source.isPlaying) yield break;
        
        float startVolume = source.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        source.Stop();
        source.volume = 0;
        source.clip = null; // Free up memory
    }
    
    // --- Volume Controls & Ducking ---
    
    public void SetMusicVolume(float volumeLinear)
    {
        currentMusicVolume = Mathf.Clamp01(volumeLinear);
        ApplyMusicVolume();
    }

    public void FadeMusic(float lowerThreshold = 0.2f, float fadeDuration = 0.5f) => StartDuck(Mathf.Clamp01(lowerThreshold), fadeDuration);
    public void UnfadeMusic(float fadeDuration = 0.5f) => StartDuck(1f, fadeDuration);
    
    private void StartDuck(float target, float duration)
    {
        if (duckCoroutine != null) StopCoroutine(duckCoroutine);
        duckCoroutine = StartCoroutine(DuckRoutine(target, duration));
    }
    
    private IEnumerator DuckRoutine(float target, float duration)
    {
        float start = duckFactor;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            duckFactor = Mathf.Lerp(start, target, t / duration);
            ApplyMusicVolume();
            yield return null;
        }
        duckFactor = target;
        ApplyMusicVolume();
        duckCoroutine = null;
    }
    
    public void SetBusVolume(string busName, float volumeLinear)
    {
        if (mixer == null) return;
        float dB = volumeLinear > 0.001f ? 20f * Mathf.Log10(volumeLinear) : -80f;
        mixer.SetFloat(busName, dB);
    }

    // --- SFX Section ---
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

    private void PlayOneShotSFX(AudioClip clip, float volume, float pitch)
    {
        AudioSource player = GetAvailableSFXPlayer();

        player.clip = clip;
        player.volume = volume;
        player.pitch = pitch;
        player.loop = false;
        player.outputAudioMixerGroup = sfxGroup;
        player.Play();
    }

    private AudioSource GetAvailableSFXPlayer()
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

    private void PlayLoopingSFX(SFXResource resource)
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

    private IEnumerator FadeOutAndDestroy(AudioSource player, float duration)
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
}
