using UnityEngine;

namespace Rhythm.Core
{
    public static class GameStartParameters
    {

        private static BeatmapData currentBeatmap;
        private static string currentSongKey;
        private static Difficulty currentDifficulty;

        private static bool autoPlay;
        private static bool showIndicator;
        private static bool showApproachRing;
        private static bool showPerfectSFX;
        private static bool hasParameters = false;


        public static void SetParameters(BeatmapData beatmap, string songKey, Difficulty difficulty, bool autoPlay, bool showIndicator, bool showApproachRing, bool showPerfectSFX)
        {
            currentBeatmap = beatmap;
            currentSongKey = songKey;
            currentDifficulty = difficulty;

            GameStartParameters.autoPlay = autoPlay;
            GameStartParameters.showIndicator = showIndicator;
            GameStartParameters.showApproachRing = showApproachRing;
            GameStartParameters.showPerfectSFX = showPerfectSFX;

            hasParameters = true;
            Debug.Log($"[GameStartParameters] Set parameters: Song='{songKey}', Difficulty='{difficulty}', AutoPlay='{autoPlay}'");
        }

        public static bool TryGetParameters(out BeatmapData beatmap, out string songKey, out Difficulty difficulty, out bool autoPlay, out bool showIndicator, out bool showApproachRing, out bool showPerfectSFX)
        {
            beatmap = currentBeatmap;
            songKey = currentSongKey;
            difficulty = currentDifficulty;

            autoPlay = GameStartParameters.autoPlay;
            showIndicator = GameStartParameters.showIndicator;
            showApproachRing = GameStartParameters.showApproachRing;
            showPerfectSFX = GameStartParameters.showPerfectSFX;

            bool result = hasParameters && currentBeatmap != null && !string.IsNullOrEmpty(currentSongKey);

            if (result)
            {
                Debug.Log($"[GameStartParameters] Retrieved parameters: Song='{songKey}', AutoPlay='{autoPlay}'");
            }
            else
            {
                Debug.LogWarning("[GameStartParameters] No valid parameters found or parameters are incomplete");
            }

            return result;
        }

        public static void ClearParameters()
        {
            currentBeatmap = null;
            currentSongKey = null;
            currentDifficulty = Difficulty.Normal;


            autoPlay = false;
            showIndicator = true;
            showApproachRing = true;
            showPerfectSFX = true;

            hasParameters = false;
            Debug.Log("[GameStartParameters] Parameters cleared");
        }

        public static bool HasValidParameters => hasParameters && currentBeatmap != null && !string.IsNullOrEmpty(currentSongKey);
    }
}
