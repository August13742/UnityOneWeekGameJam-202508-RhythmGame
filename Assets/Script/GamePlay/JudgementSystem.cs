using System;
using UnityEngine;

namespace Rhythm.GamePlay
{
    /// <summary>
    /// Singleton
    /// </summary>
    public class JudgementSystem : MonoBehaviour
    {
        public static JudgementSystem Instance
        {
            get; private set;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [Header("Timing Windows (seconds)")]
        [Tooltip("± window around hit time for a Perfect judgment")]
        [SerializeField] private float perfectWindow = 0.1f;
        [Tooltip("± window around hit time for a Good judgment")]
        [SerializeField] private float goodWindow = 0.2f;

        // State
        private int combo = 0;
        private int score = 0;

        // Events
        public event Action<string, int> OnJudgment;   // (judgmentName, currentCombo)
        public event Action<int> OnScoreChanged;
        public event Action<int> OnComboChanged;


        /// <summary>
        /// Call this when a note reports a pointer‐click (delta = actualTime − scheduledTime).
        /// </summary>
        public void RegisterHit(double delta)
        {
            float absDelta = Mathf.Abs((float)delta);
            string result;
            int points;

            if (absDelta <= perfectWindow)
            {
                result = "Perfect";
                points = 300;
                combo++;
            }
            else if (absDelta <= goodWindow)
            {
                result = "Good";
                points = 100;
                combo++;
            }
            else
            {
                // Too far off -> treat as miss
                result = "Miss";
                RegisterMiss();
                return;
            }

            score += points;
            OnJudgment?.Invoke(result, combo);
            OnScoreChanged?.Invoke(score);
            OnComboChanged?.Invoke(combo);
            Debug.Log($"[{result}] Δ={delta:F3}s → +{points}pts, combo={combo}");
        }

        /// <summary>
        /// Call this when a note times out without being clicked.
        /// </summary>
        public void RegisterMiss()
        {
            combo = 0;
            OnJudgment?.Invoke("Miss", combo);
            OnComboChanged?.Invoke(combo);
            Debug.Log("[Miss] → combo reset");
        }
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }


}
