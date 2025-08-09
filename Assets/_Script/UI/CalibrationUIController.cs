using DG.Tweening;
using Rhythm.GamePlay.OSU.Aimless;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rhythm.UI
{
    public class CalibrationUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider offsetSlider;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text offsetValueText;
        [SerializeField] private TMP_Text instructionsText;
        [SerializeField] private Image fadePanel;

        [Header("Settings")]
        [SerializeField] private BeatmapData calibrationBeatmap;
        [SerializeField] private string nextSceneName;
        private RhythmManagerOSUAimless rhythmManager;

        private void Start()
        {
            rhythmManager = RhythmManagerOSUAimless.Instance;
            SetupUI();
            //StartCoroutine(DelayedCalibration());
            fadePanel.color = Color.black;
            fadePanel.DOFade(0f, 1f).
                SetEase(Ease.InQuint).
                OnComplete(() =>
                {
                    StartCalibration();
                    Destroy(fadePanel.gameObject);
                });
            

        }

        private void SetupUI()
        {
            
            // Setup slider
            if (offsetSlider != null)
            {
                offsetSlider.minValue = -0.5f;
                offsetSlider.maxValue = 0.5f;
                offsetSlider.value = rhythmManager.AudioOffset;
                offsetSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            // Setup buttons
            if (increaseButton != null)
                increaseButton.onClick.AddListener(() => rhythmManager.IncreaseAudioOffset());
                
            if (decreaseButton != null)
                decreaseButton.onClick.AddListener(() => rhythmManager.DecreaseAudioOffset());
                
            if (exitButton != null)
                exitButton.onClick.AddListener(ExitCalibration);

            // Setup instructions
            if (instructionsText != null)
            {
                //instructionsText.text = "Adjust the audio offset until the beats feel synchronized.\n" +
                //                      "Use SPACE to hit notes or the +/- buttons to adjust timing.";
            }

            Debug.Log("setup");
            if (offsetValueText != null)
            {
                Debug.Log("offset" + rhythmManager.AudioOffset);
                offsetValueText.text = $"{rhythmManager.AudioOffset * 1000} MS";
            }
            RhythmManagerOSUAimless.OnAudioOffsetChanged += UpdateOffsetDisplay;
        }

        private void StartCalibration()
        {
            if (calibrationBeatmap != null)
            {
                rhythmManager.StartCalibrationMode(calibrationBeatmap);
            }
            else
            {
                Debug.LogError("No calibration beatmap assigned!");
            }
        }

        private void OnSliderValueChanged(float value)
        {
            rhythmManager.SetAudioOffset(value);
        }

        private void UpdateOffsetDisplay(float offset)
        {
            if (offsetSlider != null && !offsetSlider.IsInteractable())
                offsetSlider.value = offset;

            if (offsetValueText != null)
                offsetValueText.text = $"{offset * 1000:F0}ms";
        }

        private void ExitCalibration()
        {
            rhythmManager.ExitCalibrationMode();


            PlayerPrefs.SetFloat("AudioOffset", rhythmManager.AudioOffset);
            PlayerPrefs.Save();

            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }

        private void OnDestroy()
        {
            RhythmManagerOSUAimless.OnAudioOffsetChanged -= UpdateOffsetDisplay;
        }


    }
}
