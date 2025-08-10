using UnityEngine;
using UnityEngine.UI;

public class VolumeConfigUIController : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sFXvolumeSlider;

    [Header("Settings")]
    [SerializeField] private float defaultMasterVolume = 0.8f;
    [SerializeField] private float defaultMusicVolume = 0.7f;
    [SerializeField] private float defaultSFXVolume = 0.8f;

    // Mixer parameter names (should match your AudioMixer)
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string MUSIC_VOLUME_PARAM = "MusicVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";

    // PlayerPrefs keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    private void Start()
    {
        SetupUI();
        LoadVolumeSettings();
    }

    private void SetupUI()
    {
        // Configure sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sFXvolumeSlider != null)
        {
            sFXvolumeSlider.minValue = 0f;
            sFXvolumeSlider.maxValue = 1f;
            sFXvolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void LoadVolumeSettings()
    {
        // Load saved settings or use defaults
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultMasterVolume);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;

        if (sFXvolumeSlider != null)
            sFXvolumeSlider.value = sfxVolume;

        ApplyMasterVolume(masterVolume);
        ApplyMusicVolume(musicVolume);
        ApplySFXVolume(sfxVolume);
    }

    private void OnMasterVolumeChanged(float value)
    {
        ApplyMasterVolume(value);
        SaveVolumeSetting(MASTER_VOLUME_KEY, value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        ApplyMusicVolume(value);
        SaveVolumeSetting(MUSIC_VOLUME_KEY, value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        ApplySFXVolume(value);
        SaveVolumeSetting(SFX_VOLUME_KEY, value);
    }

    private void ApplyMasterVolume(float volumeLinear)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBusVolume(MASTER_VOLUME_PARAM, volumeLinear);
        }
    }

    private void ApplyMusicVolume(float volumeLinear)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBusVolume(MUSIC_VOLUME_PARAM, volumeLinear);
            AudioManager.Instance.SetMusicVolume(volumeLinear);
        }
    }

    private void ApplySFXVolume(float volumeLinear)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBusVolume(SFX_VOLUME_PARAM, volumeLinear);
        }
    }

    private void SaveVolumeSetting(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = defaultMasterVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = defaultMusicVolume;

        if (sFXvolumeSlider != null)
            sFXvolumeSlider.value = defaultSFXVolume;
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sFXvolumeSlider != null)
            sFXvolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }
}
