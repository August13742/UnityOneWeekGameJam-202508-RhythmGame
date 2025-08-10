using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Rhythm.Core;
using Rhythm.GamePlay.OSU.Aimless;

namespace Rhythm.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject volumeConfigPanel;
        
        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button calibrationButton;
        [SerializeField] private Button jukeboxButton;
        [SerializeField] private Button resumeButton;
        
        [Header("Statistics Display")]
        [SerializeField] private TextMeshProUGUI notesText;
        [SerializeField] private TextMeshProUGUI perfectText;
        [SerializeField] private TextMeshProUGUI goodText;
        [SerializeField] private TextMeshProUGUI missText;
        
        [Header("Scene References")]
        [SerializeField] private string calibrationSceneName = "CalibrationScene";
        [SerializeField] private string jukeboxSceneName = "JukeboxScene";
        
        private RhythmManagerOSUAimless rhythmManager;
        private JudgementSystem judgementSystem;
        private bool isPauseMenuActive;

        private void Start()
        {
            rhythmManager = RhythmManagerOSUAimless.Instance;
            judgementSystem = JudgementSystem.Instance;
            
            if (restartButton)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (calibrationButton)
                calibrationButton.onClick.AddListener(OnCalibrationClicked);
            if (jukeboxButton)
                jukeboxButton.onClick.AddListener(OnJukeboxClicked);
            if (resumeButton)
                resumeButton.onClick.AddListener(OnResumeClicked);


            if (pauseMenuPanel)
                pauseMenuPanel.SetActive(false);
            if(volumeConfigPanel) volumeConfigPanel.SetActive(false);
                
            isPauseMenuActive = false;
        }

        private void OnEnable()
        {
            if (RhythmManagerOSUAimless.Instance)
            {
                RhythmManagerOSUAimless.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (RhythmManagerOSUAimless.Instance)
            {
                RhythmManagerOSUAimless.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!rhythmManager) return;
                
                if (isPauseMenuActive)
                {
                    HidePauseMenu();
                    return;
                }
                
                // If game is playing, show pause menu
                if (rhythmManager.CurrentState == GameState.Playing && rhythmManager.IsGameActive)
                {
                    ShowPauseMenu();
                }
            }
            
            if (isPauseMenuActive)
            {
                UpdateStatisticsDisplay();
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Finished || newState == GameState.NotStarted)
            {
                HidePauseMenu();
            }
        }

        public void TogglePauseMenu()
        {
            if (isPauseMenuActive)
            {
                HidePauseMenu();
            }
            else
            {
                ShowPauseMenu();
            }
        }

        public void ShowPauseMenu()
        {
            if (!rhythmManager || !rhythmManager.CanPauseGame)
                return;
                
            if (rhythmManager.CurrentState == GameState.Playing)
            {
                rhythmManager.PauseGame();
            }
            
            if (pauseMenuPanel)
                pauseMenuPanel.SetActive(true);
                
            isPauseMenuActive = true;
            UpdateStatisticsDisplay();
        }

        public void HidePauseMenu()
        {
            if (pauseMenuPanel)
                pauseMenuPanel.SetActive(false);
                
            isPauseMenuActive = false;
            
            // Only resume if the game is currently paused
            if (rhythmManager && rhythmManager.CurrentState == GameState.Paused && rhythmManager.CanResumeGame)
            {
                rhythmManager.ResumeGame();
            }
        }

        private void UpdateStatisticsDisplay()
        {
            if (!judgementSystem)
                return;
                
            // current/total
            if (notesText)
            {
                int currentNotes = judgementSystem.PerfectCount + judgementSystem.GoodCount + judgementSystem.MissCount;
                notesText.text = $"Notes: {currentNotes}/{judgementSystem.TotalNotesInSong}";
            }
            
            if (perfectText)
            {
                int currentNotes = judgementSystem.PerfectCount + judgementSystem.GoodCount + judgementSystem.MissCount;
                perfectText.text = $"Perfect: {judgementSystem.PerfectCount}/{currentNotes}";
            }
            
            if (goodText)
            {
                goodText.text = $"Good: {judgementSystem.GoodCount}";
            }
            
            if (missText)
            {
                missText.text = $"Miss: {judgementSystem.MissCount}";
            }
        }

        #region Button Event Handlers

        private void OnRestartClicked()
        {
            StartCoroutine(ScheduleRestart());
        }
        private System.Collections.IEnumerator ScheduleRestart()
        {
            HidePauseMenu();
            AudioManager.Instance.FadeMusic();
            CrossfadeManager.Instance.FadeToBlack();
            yield return new WaitForSeconds(1);
            if (rhythmManager)
            {
                CrossfadeManager.Instance.FadeFromBlack();
                yield return new WaitForSeconds(1);
                rhythmManager.RestartGame();
            }
        }
        private void OnCalibrationClicked()
        {
            HidePauseMenu();
            AudioManager.Instance.FadeMusic();
            // Load calibration scene
            if (!string.IsNullOrEmpty(calibrationSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(calibrationSceneName);
            }
            else
            {
                Debug.LogWarning("PauseMenuController: Calibration scene name not set!");
            }
        }

        System.Collections.IEnumerator ScheduleChangeJukebox()
        {
            AudioManager.Instance.StopMusic();
            CrossfadeManager.Instance.FadeToBlack();
            yield return new WaitForSeconds(1);
            UnityEngine.SceneManagement.SceneManager.LoadScene(jukeboxSceneName);
        }
        private void OnJukeboxClicked()
        {
            HidePauseMenu();
            
            // Load song selection scene
            if (!string.IsNullOrEmpty(jukeboxSceneName))
            {
                StartCoroutine(ScheduleChangeJukebox());
            }
            else
            {
                Debug.LogWarning("PauseMenuController: Song list scene name not set!");
            }
        }

        private void OnResumeClicked()
        {
            HidePauseMenu();
        }

        #endregion

        #region Public API

        public bool IsPauseMenuActive => isPauseMenuActive;

        public void SetCalibrationSceneName(string sceneName)
        {
            calibrationSceneName = sceneName;
        }

        public void SetJukeboxSceneName(string sceneName)
        {
            jukeboxSceneName = sceneName;
        }

        #endregion
    }
}
