using UnityEngine;
using TMPro;
using Rhythm.GamePlay;

namespace Rhythm.UI
{



    public class ScoreBoard : MonoBehaviour
    {
        public TMP_Text ScoreLabel;
        public TMP_Text AccuracyLabel;

        private void Start()
        {
            ScoreLabel.text = "Score: 0";
            AccuracyLabel.text = "100%";

            JudgementSystem.Instance.OnScoreChanged += UpdateScore;
        }
        void UpdateScore(int score, float accuracy)
        {
            ScoreLabel.text = $"Score: {score.ToString()}";
            AccuracyLabel.text = $"{accuracy * 100:F2}%";

        }
        private void OnDisable()
        {
            JudgementSystem.Instance.OnScoreChanged -= UpdateScore;
        }
    }
}   
