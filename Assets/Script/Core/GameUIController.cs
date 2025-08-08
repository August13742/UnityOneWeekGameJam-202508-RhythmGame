using UnityEngine;
using Rhythm.GamePlay.OSU.Aimless;
using TMPro;
using UnityEngine.UI;
namespace Rhythm.Core
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void Start()
        {
            RhythmManagerOSUAimless.OnGameStateChanged += OnGameStateChanged;
            RhythmManagerOSUAimless.OnSongProgressChanged += OnProgressChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
        }
        private void OnProgressChanged(float progress)
        {

        }
        public void OnStartButtonClicked()
        {
            if (RhythmManagerOSUAimless.Instance.CanStartGame)
                RhythmManagerOSUAimless.Instance.StartGame();
        }

        public void OnPauseButtonClicked()
        {
            if (RhythmManagerOSUAimless.Instance.CanPauseGame)
                RhythmManagerOSUAimless.Instance.PauseGame();
        }
    }
}


