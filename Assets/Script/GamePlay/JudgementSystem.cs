using System;
using System.Collections;
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
        [SerializeField] private GameObject InjuredScreenEffect;
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

        // --- Note Statistics ---
        public int TotalNotes { get; private set; } = 0;
        public int PerfectCount { get; private set; } = 0;
        public int GoodCount { get; private set; } = 0;
        public int MissCount { get; private set; } = 0;

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
        [Tooltip("± window around hit time for a Perfect Judgement")]
        public float PerfectWindow = 0.1f;
        [Tooltip("± window around hit time for a Good Judgement")]
        public float GoodWindow = 0.2f;

        private void Start()
        {
            InjuredScreenEffect.SetActive(false);
        }

        // Events
        public event Action<string, int> OnJudgement;   // (JudgementName, currentCombo)
        public event Action<int, float, int> OnScoreChanged; // (score, currentAccuracy)
        public event Action<int> OnComboChanged;

        /// <summary>
        /// Call this when a note reports a pointer‐click (delta = actualTime − scheduledTime).
        /// </summary>
        public void RegisterHit(double delta)
        {
            float absDelta = Mathf.Abs((float)delta);
            string result;
            int points;

            // Every hit (even if not judged as miss) is a note attempt
            TotalNotes++;

            if (absDelta <= PerfectWindow)
            {
                result = "Perfect";
                points = pointsPerPerfect;
                AudioManager.Instance.PlaySFX(shootHitSFXResource);
                CurrentCombo++;
                PerfectCount++;
            }
            else if (absDelta <= GoodWindow)
            {
                result = "Good";
                points = pointsPerGood;
                AudioManager.Instance.PlaySFX(shootHitSFXResource);
                CurrentCombo++;
                GoodCount++;
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
            OnJudgement?.Invoke(result, CurrentCombo);
            OnScoreChanged?.Invoke(Score, CurrentAccuracy, CurrentCombo);
            OnComboChanged?.Invoke(CurrentCombo);

            //Debug.Log($"[{result}] Δ={delta:F3}s → +{points}pts, CurrentCombo={CurrentCombo}");
        }

        /// <summary>
        /// Call this when a note times out without being clicked.
        /// </summary>
        public void RegisterMiss()
        {
            TotalNotes++;
            MissCount++;

            CurrentCombo = 0;
            CurrentMaxPossibleScore += pointsPerPerfect;
            CurrentAccuracy = (float)Score / CurrentMaxPossibleScore;

            OnScoreChanged?.Invoke(Score, CurrentAccuracy, CurrentCombo);
            OnJudgement?.Invoke("Miss", CurrentCombo);
            OnComboChanged?.Invoke(CurrentCombo);

            StartCoroutine(ToggleInjuredEffect());
            //Debug.Log("[Miss] → CurrentCombo reset");
        }

        public void ResetStatistics()
        {
            TotalNotes = 0;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
        }

        IEnumerator ToggleInjuredEffect()
        {
            InjuredScreenEffect.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            InjuredScreenEffect.SetActive(false);
        }
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
