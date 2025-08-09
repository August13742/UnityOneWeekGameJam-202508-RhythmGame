using UnityEngine;
using UnityEngine.UI;

namespace Rhythm.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button calibrateButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private string calibrationSceneName = "CalibrationScene";

        private void Start()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (playButton != null)
                playButton.onClick.AddListener(StartGame);
                
            if (calibrateButton != null)
                calibrateButton.onClick.AddListener(StartCalibration);
                
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
        }

        private void StartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }

        private void StartCalibration()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(calibrationSceneName);
        }

        private void OpenSettings()
        {
            // Implement settings menu
            Debug.Log("Opening settings...");
        }

        private void QuitGame()
        {
            Application.Quit();
        }
    }
}
