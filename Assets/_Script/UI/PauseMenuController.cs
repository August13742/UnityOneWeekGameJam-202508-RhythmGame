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
        
        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button calibrationButton;
        [SerializeField] private Button songListButton;
        [SerializeField] private Button resumeButton;
        
        [Header("Statistics Display")]
        [SerializeField] private TextMeshProUGUI notesText;
        [SerializeField] private TextMeshProUGUI perfectText;
        [SerializeField] private TextMeshProUGUI goodText;
        [SerializeField] private TextMeshProUGUI missText;
        
        [Header("Scene References")]
        [SerializeField] private string calibrationSceneName = "CalibrationScene";
        [SerializeField] private string songListSceneName = "SongSelectionScene";
        
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
            if (songListButton)
                songListButton.onClick.AddListener(OnSongListClicked);
            if (resumeButton)
                resumeButton.onClick.AddListener(OnResumeClicked);


            if (pauseMenuPanel)
                pauseMenuPanel.SetActive(false);
                
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
                if (rhythmManager && rhythmManager.IsGameActive)
                {
                    TogglePauseMenu();
                }
                else if (rhythmManager && rhythmManager.CurrentState == GameState.Paused)
                {
                    TogglePauseMenu();
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
                
            rhythmManager.PauseGame();
            
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
            
            if (rhythmManager && rhythmManager.CanResumeGame)
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
            HidePauseMenu();
            AudioManager.Instance.FadeMusic();
            if (rhythmManager)
            {
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

        private void OnSongListClicked()
        {
            HidePauseMenu();
            
            // Load song selection scene
            if (!string.IsNullOrEmpty(songListSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(songListSceneName);
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

        public void SetSongListSceneName(string sceneName)
        {
            songListSceneName = sceneName;
        }

        #endregion
    }
}
