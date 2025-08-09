using UnityEngine;
using Rhythm.GamePlay.OSU.Aimless;
using UnityEngine.UI;
namespace Rhythm.Core
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button skipButton;

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

        /// <summary>
        /// Skips the current song to the end to trigger the game end screen (for debugging).
        /// </summary>
        public void OnSkipToEndButtonClicked()
        {
            var rhythmManager = RhythmManagerOSUAimless.Instance;
            if (rhythmManager != null && rhythmManager.IsGameActive)
            {
                // Fast-forward the song time to just after the song's length
                var beatmap = rhythmManager.CurrentBeatmap;
                if (beatmap != null && beatmap.musicTrack != null)
                {
                    // Set the DSP start time so that SongTimeNow() > song length
                    double songLength = beatmap.musicTrack.length;
                    // Add a small buffer to ensure the end is triggered
                    double newDspSongStartTime = AudioSettings.dspTime - songLength - 1.0 - rhythmManager.TotalPausedDuration;
                    typeof(RhythmManagerOSUAimless)
                        .GetField("dspSongStartTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .SetValue(rhythmManager, newDspSongStartTime);
                }
            }
        }
    }
}


