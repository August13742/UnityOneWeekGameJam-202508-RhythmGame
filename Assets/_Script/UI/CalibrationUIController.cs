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

        [Header("Settings")]
        [SerializeField] private BeatmapData calibrationBeatmap;
        [SerializeField] private string nextSceneName;
        private RhythmManagerOSUAimless rhythmManager;

        private void Awake()
        {

            StartCoroutine(DelayedStart());

        }
        System.Collections.IEnumerator DelayedStart()
        {
            rhythmManager = RhythmManagerOSUAimless.Instance;
            SetupUI();
            StartCalibration();
            CrossfadeManager.Instance.FadeFromBlack(1f);
            yield return new WaitForSeconds(1f);

        }
        private void SetupUI()
        {
            
            if (offsetSlider != null)
            {
                offsetSlider.minValue = -0.5f;
                offsetSlider.maxValue = 0.5f;
                offsetSlider.value = rhythmManager.AudioOffset;
                offsetSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (increaseButton != null)
                increaseButton.onClick.AddListener(() => rhythmManager.IncreaseAudioOffset());
                
            if (decreaseButton != null)
                decreaseButton.onClick.AddListener(() => rhythmManager.DecreaseAudioOffset());
                
            if (exitButton != null)
                exitButton.onClick.AddListener(ExitCalibration);

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
            StartCoroutine(ScheduleExitCalibration());
        }
        System.Collections.IEnumerator ScheduleExitCalibration()
        {
            rhythmManager.ExitCalibrationMode();


            PlayerPrefs.SetFloat("AudioOffset", rhythmManager.AudioOffset);
            PlayerPrefs.Save();

            CrossfadeManager.Instance.FadeToBlack(1f);
            yield return new WaitForSeconds(1);
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
        private void OnDestroy()
        {
            RhythmManagerOSUAimless.OnAudioOffsetChanged -= UpdateOffsetDisplay;
        }


    }
}
