//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using Rhythm.GamePlay.OSU.Aimless;
//using Rhythm.Core;

//namespace Rhythm.UI
//{
//    public class SongFinishedMenu : MonoBehaviour
//    {
//        [Header("UI References")]
//        [SerializeField] private GameObject menuPanel;
//        [SerializeField] private Button restartButton;
//        [SerializeField] private Button mainMenuButton;
//        [SerializeField] private Button exitButton;

//        [Header("Statistics Display")]
//        [SerializeField] private TMP_Text finalScoreText;
//        [SerializeField] private TMP_Text finalAccuracyText;
//        [SerializeField] private TMP_Text maxComboText;
//        [SerializeField] private TMP_Text totalNotesText;
//        [SerializeField] private TMP_Text perfectCountText;
//        [SerializeField] private TMP_Text goodCountText;
//        [SerializeField] private TMP_Text missCountText;
//        [SerializeField] private TMP_Text songNameText;



//        private int maxComboAchieved = 0;

//        private void Awake()
//        {
//            if (menuPanel != null)
//                menuPanel.SetActive(false);

//            // Setup button listeners
//            if (restartButton != null)
//                restartButton.onClick.AddListener(RestartSong);
            
//            if (mainMenuButton != null)
//                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
//            if (exitButton != null)
//                exitButton.onClick.AddListener(ExitGame);
//        }

//        private void OnEnable()
//        {
//            // Subscribe to game events
//            RhythmManagerOSUAimless.OnGameFinished += ShowSongFinishedMenu;
//            RhythmManagerOSUAimless.OnGameStarted += HideSongFinishedMenu;
            
//            // Subscribe to combo changes to track max combo
//            if (JudgementSystem.Instance != null)
//                JudgementSystem.Instance.OnComboChanged += TrackMaxCombo;
//        }

//        private void OnDisable()
//        {
//            // Unsubscribe from events
//            RhythmManagerOSUAimless.OnGameFinished -= ShowSongFinishedMenu;
//            RhythmManagerOSUAimless.OnGameStarted -= HideSongFinishedMenu;
            
//            if (JudgementSystem.Instance != null)
//                JudgementSystem.Instance.OnComboChanged -= TrackMaxCombo;
//        }

//        private void ShowSongFinishedMenu()
//        {
//            if (menuPanel == null) return;

//            UpdateStatisticsDisplay();
//            menuPanel.SetActive(true);
            
//            // Pause the game (if not already paused)
//            Time.timeScale = 0f;
            
//            // Show cursor for menu interaction
//            Cursor.lockState = CursorLockMode.None;
//            Cursor.visible = true;
//        }

//        private void HideSongFinishedMenu()
//        {
//            if (menuPanel != null)
//                menuPanel.SetActive(false);
            
//            // Reset max combo tracking
//            maxComboAchieved = 0;
            
//            // Resume normal time scale
//            Time.timeScale = 1f;
//        }

//        private void UpdateStatisticsDisplay()
//        {
//            var judgementSystem = JudgementSystem.Instance;
//            var rhythmManager = RhythmManagerOSUAimless.Instance;

//            if (judgementSystem == null || rhythmManager == null) return;

//            // Display basic statistics
//            if (finalScoreText != null)
//                finalScoreText.text = $"Score: {judgementSystem.Score:N0}";

//            if (finalAccuracyText != null)
//                finalAccuracyText.text = $"Accuracy: {judgementSystem.CurrentAccuracy:P2}";

//            if (maxComboText != null)
//                maxComboText.text = $"Max Combo: {maxComboAchieved}";

//            if (totalNotesText != null)
//                totalNotesText.text = $"Total Notes: {judgementSystem.TotalNotes}";

//            if (perfectCountText != null)
//                perfectCountText.text = $"Perfect: {judgementSystem.PerfectCount}";

//            if (goodCountText != null)
//                goodCountText.text = $"Good: {judgementSystem.GoodCount}";

//            if (missCountText != null)
//                missCountText.text = $"Miss: {judgementSystem.MissCount}";

//            if (songNameText != null)
//                songNameText.text = rhythmManager.GetCurrentBeatmapName();

//            // Calculate and display grade
//            UpdateGradeDisplay(judgementSystem.CurrentAccuracy);
//        }

//        private void UpdateGradeDisplay(float accuracy)
//        {
//            string grade;
//            Color gradeColor;

//            if (accuracy >= 0.95f)
//            {
//                grade = "S";
//                gradeColor = sRankColor;
//            }
//            else if (accuracy >= 0.90f)
//            {
//                grade = "A";
//                gradeColor = aRankColor;
//            }
//            else if (accuracy >= 0.80f)
//            {
//                grade = "B";
//                gradeColor = bRankColor;
//            }
//            else if (accuracy >= 0.70f)
//            {
//                grade = "C";
//                gradeColor = cRankColor;
//            }
//            else
//            {
//                grade = "D";
//                gradeColor = dRankColor;
//            }

//            if (gradeText != null)
//            {
//                gradeText.text = grade;
//                gradeText.color = gradeColor;
//            }

//            if (gradeBackground != null)
//            {
//                gradeBackground.color = new Color(gradeColor.r, gradeColor.g, gradeColor.b, 0.3f);
//            }
//        }

//        private void TrackMaxCombo(int currentCombo)
//        {
//            if (currentCombo > maxComboAchieved)
//                maxComboAchieved = currentCombo;
//        }

//        private void RestartSong()
//        {
//            var rhythmManager = RhythmManagerOSUAimless.Instance;
//            if (rhythmManager != null)
//            {
//                HideSongFinishedMenu();
//                rhythmManager.RestartGame();
//            }
//        }

//        private void ReturnToMainMenu()
//        {
//            HideSongFinishedMenu();
            
//            // Stop the current game
//            var rhythmManager = RhythmManagerOSUAimless.Instance;
//            if (rhythmManager != null)
//                rhythmManager.StopGame();

//            // Load main menu scene (adjust scene name as needed)
//            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
//        }

//        private void ExitGame()
//        {
//            HideSongFinishedMenu();
            
//#if UNITY_EDITOR
//            UnityEditor.EditorApplication.isPlaying = false;
//#else
//            Application.Quit();
//#endif
//        }

//        private void OnDestroy()
//        {
//            // Clean up button listeners
//            if (restartButton != null)
//                restartButton.onClick.RemoveListener(RestartSong);
            
//            if (mainMenuButton != null)
//                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            
//            if (exitButton != null)
//                exitButton.onClick.RemoveListener(ExitGame);
//        }
//    }
//}
