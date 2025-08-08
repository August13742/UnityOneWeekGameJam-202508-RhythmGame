using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get; private set;
    }

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
    private Coroutine activeFadeCoroutine;
    private float currentMusicVolume = 1f;

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
    private void OnEnable()
    {
        
    }
    private void Start()
    {
        if (GameEvents.Instance != null)
        {

            GameEvents.Instance.OnPlayMusicScheduled += HandlePlayMusicScheduled;
        }
        else
            Debug.Log("no game event");

    }
    private void OnDisable()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.OnPlayMusicScheduled -= HandlePlayMusicScheduled;
        }
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

        // Ensure the clip is set and the player is ready
        musicPlayer.clip = clip;
        musicPlayer.volume = currentMusicVolume;
        
        // Calculate safe start time with proper buffer
        double currentDspTime = AudioSettings.dspTime;
        double safeStartTime;
        
        // If the scheduled time is in the past or too close to now, schedule for immediate playback
        if (dspTime <= currentDspTime + 0.05) // 50ms buffer
        {
            safeStartTime = currentDspTime + 0.1; // Start 100ms from now
            Debug.LogWarning($"[AudioManager] Scheduled time {dspTime} is too close to current time {currentDspTime}. Adjusting to {safeStartTime}");
        }
        else
        {
            safeStartTime = dspTime;
        }
        
        Debug.Log($"[AudioManager] Scheduling music: {clip.name} at DSP time: {safeStartTime} (requested: {dspTime}, current: {currentDspTime})");
        
        // For very immediate playback, use Play() instead of PlayScheduled()
        if (safeStartTime - currentDspTime < 0.1)
        {
            Debug.Log("[AudioManager] Using immediate Play() instead of PlayScheduled()");
            musicPlayer.Play();
        }
        else
        {
            musicPlayer.PlayScheduled(safeStartTime);
        }
    }

    public void PlaySFX(SFXResource resource)
    {
        if (resource.loop)
        {
            PlayLoopingSFX(resource);
        }
        else
        {
            PlayOneShotSFX(resource.clip, resource.volumeLinear, resource.pitchScale);
        }
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
        // Fade out current music
        float halfDuration = duration * 0.5f;
        if (musicPlayer.isPlaying)
        {
            yield return FadeAudioSource(musicPlayer, musicPlayer.volume, 0f, halfDuration);
            musicPlayer.Stop();
        }

        // Switch and fade in new music
        musicPlayer.clip = newClip;
        musicPlayer.Play();
        yield return FadeAudioSource(musicPlayer, 0f, currentMusicVolume, halfDuration);
    }

    IEnumerator FadeAudioSource(AudioSource source, float startVol, float endVol, float duration)
    {
        float elapsed = 0f;
        source.volume = startVol;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, endVol, elapsed / duration);
            yield return null;
        }

        source.volume = endVol;
    }

    public void SetMusicVolume(float volumeLinear)
    {
        currentMusicVolume = volumeLinear;
        musicPlayer.volume = volumeLinear;
    }

    public void FadeMusic(float lowerThreshold = 0.2f, float fadeDuration = 0.5f)
    {
        if (activeFadeCoroutine != null)
            StopCoroutine(activeFadeCoroutine);
        activeFadeCoroutine = StartCoroutine(FadeAudioSource(musicPlayer, musicPlayer.volume, lowerThreshold, fadeDuration));
    }

    public void UnfadeMusic(float fadeDuration = 0.5f)
    {
        if (activeFadeCoroutine != null)
            StopCoroutine(activeFadeCoroutine);
        activeFadeCoroutine = StartCoroutine(FadeAudioSource(musicPlayer, musicPlayer.volume, currentMusicVolume, fadeDuration));
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
