using System;
using UnityEngine;
using UnityEngine.UIElements;

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
        public SFXResource shootMissSFXResource;

        public SFXResource shootHitSFXResource;

        public int Score
        {
            get; private set;
        } = 0;
        public int CurrentMaxPossibleScore
        {
            get; private set;
        } = 0;
        public int CurrentCombo
        {
            get; private set;
        } = 0;
        public float CurrentAccuracy
        {
            get; private set;
        } = 1f;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        [SerializeField] private int pointsPerPerfect = 20;
        [SerializeField] private int pointsPerGood = 10;


        [Header("Timing Windows (seconds)")]
        [Tooltip("± window around hit time for a Perfect judgment")]
        [SerializeField] private float perfectWindow = 0.1f;
        [Tooltip("± window around hit time for a Good judgment")]
        [SerializeField] private float goodWindow = 0.2f;




        // Events
        public event Action<string, int> OnJudgment;   // (judgmentName, currentCombo)
        public event Action<int, float> OnScoreChanged; // (score, currentAccuracy)
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
                points = pointsPerPerfect;
                AudioManager.Instance.PlaySFX(shootHitSFXResource);
                CurrentCombo++;
            }
            else if (absDelta <= goodWindow)
            {
                result = "Good";
                points = pointsPerGood;
                AudioManager.Instance.PlaySFX(shootHitSFXResource);
                CurrentCombo++;
            }
            else
            {
                // Too far off -> treat as miss
                AudioManager.Instance.PlaySFX(shootMissSFXResource);
                RegisterMiss();
                return;
            }

            CurrentMaxPossibleScore += pointsPerPerfect;
            Score += points;
            CurrentAccuracy = (float)Score / CurrentMaxPossibleScore;
            OnJudgment?.Invoke(result, CurrentCombo);
            OnScoreChanged?.Invoke(Score, CurrentAccuracy);
            OnComboChanged?.Invoke(CurrentCombo);

            //Debug.Log($"[{result}] Δ={delta:F3}s → +{points}pts, CurrentCombo={CurrentCombo}");
        }

        /// <summary>
        /// Call this when a note times out without being clicked.
        /// </summary>
        public void RegisterMiss()
        {
            CurrentCombo = 0;
            CurrentMaxPossibleScore += pointsPerPerfect;
            CurrentAccuracy = (float)Score / CurrentMaxPossibleScore;

            OnScoreChanged?.Invoke(Score, CurrentAccuracy);
            OnJudgment?.Invoke("Miss", CurrentCombo);
            OnComboChanged?.Invoke(CurrentCombo);
            //Debug.Log("[Miss] → CurrentCombo reset");
        }
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }


}
