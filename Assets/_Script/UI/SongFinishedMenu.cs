using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Rhythm.GamePlay.OSU.Aimless;
using Rhythm.Core;

namespace Rhythm.UI
{
    public class SongFinishedMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button exitButton;

        [Header("Statistics Display")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text finalAccuracyText;
        [SerializeField] private TMP_Text maxComboText;
        [SerializeField] private TMP_Text totalNotesText;
        [SerializeField] private TMP_Text perfectCountText;
        [SerializeField] private TMP_Text goodCountText;
        [SerializeField] private TMP_Text missCountText;
        [SerializeField] private TMP_Text songNameText;



        private void Awake()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);

            // Setup button listeners
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartSong);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (exitButton != null)
                exitButton.onClick.AddListener(ExitGame);
        }

        private void OnEnable()
        {
            RhythmManagerOSUAimless.OnGameFinished += ShowSongFinishedMenu;
            RhythmManagerOSUAimless.OnGameStarted += HideSongFinishedMenu;

        }

        private void OnDisable()
        {
            RhythmManagerOSUAimless.OnGameFinished -= ShowSongFinishedMenu;
            RhythmManagerOSUAimless.OnGameStarted -= HideSongFinishedMenu;

        }

        private void ShowSongFinishedMenu()
        {
            if (menuPanel == null)
                return;

            UpdateStatisticsDisplay();
            menuPanel.SetActive(true);



            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HideSongFinishedMenu()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);


        }

        private void UpdateStatisticsDisplay()
        {
            var judgementSystem = JudgementSystem.Instance;
            var rhythmManager = RhythmManagerOSUAimless.Instance;

            if (judgementSystem == null || rhythmManager == null)
                return;

            // Display basic statistics
            if (finalScoreText != null)
                finalScoreText.text = $"{judgementSystem.Score:N0}";

            if (finalAccuracyText != null)
                finalAccuracyText.text = $"{judgementSystem.CurrentAccuracy:P2}";

            if (maxComboText != null)
                maxComboText.text = $"{judgementSystem.MaxComboAchieved}";

            if (totalNotesText != null)
                totalNotesText.text = $"{judgementSystem.TotalNotes}";

            if (perfectCountText != null)
                perfectCountText.text = $"{judgementSystem.PerfectCount}";

            if (goodCountText != null)
                goodCountText.text = $"{judgementSystem.GoodCount}";

            if (missCountText != null)
                missCountText.text = $"{judgementSystem.MissCount}";

            if (songNameText != null)
                songNameText.text = rhythmManager.GetCurrentBeatmapName();

        }


        private void RestartSong()
        {
            var rhythmManager = RhythmManagerOSUAimless.Instance;
            if (rhythmManager != null)
            {
                HideSongFinishedMenu();
                rhythmManager.RestartGame();
            }
        }

        private void ReturnToMainMenu()
        {
            HideSongFinishedMenu();

            // Stop the current game
            var rhythmManager = RhythmManagerOSUAimless.Instance;
            if (rhythmManager != null)
                rhythmManager.StopGame();

            // Load main menu scene (adjust scene name as needed)
            UnityEngine.SceneManagement.SceneManager.LoadScene("JukeboxScene");
        }

        private void ExitGame()
        {
            HideSongFinishedMenu();
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartSong);

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(ExitGame);
        }
    }
}
