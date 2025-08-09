using UnityEngine;
using TMPro;
using Rhythm.Core;

namespace Rhythm.UI
{



    public class ScoreBoard : MonoBehaviour
    {
        public TMP_Text ScoreLabel;
        public TMP_Text AccuracyLabel;
        public TMP_Text ComboLabel;

        private void Start()
        {
            ScoreLabel.text = "Score: 0";
            ComboLabel.text = "Combo: 0";
            AccuracyLabel.text = "100%";

            JudgementSystem.Instance.OnScoreChanged += UpdateScore;
        }
        void UpdateScore(int score, float accuracy, int combo)
        {
            ScoreLabel.text = $"Score: {score}";
            AccuracyLabel.text = $"{accuracy * 100:F2}%";
            ComboLabel.text = $"Combo: {combo}";

        }
        private void OnDisable()
        {
            if (JudgementSystem.Instance != null)
            JudgementSystem.Instance.OnScoreChanged -= UpdateScore;
        }
    }
}   
