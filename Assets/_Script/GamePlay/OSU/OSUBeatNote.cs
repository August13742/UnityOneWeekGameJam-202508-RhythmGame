using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Rhythm.UI;
using Rhythm.Core;

namespace Rhythm.GamePlay.OSU
{
    public class OSUBeatNote : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Image hitCircle;
        [SerializeField] private RawImage approachRing;
        private GameObject notificationText;
        public bool IndicatorSoundPlayed
        {
            get; set;
        }
        // Public property to be read by the input manager
        public Vector3 WorldPosition
        {
            get; private set;
        }
        public double RelativeHitTime
        {
            get; private set;
        }
        public bool HasProcessed
        {
            get; private set;
        }

        private float approachTime;
        private Color defaultColour = new Vector4(1, 1, 1, 0.35f);

        private System.Action<double> onHit;
        private System.Action onMiss;
        private System.Action<OSUBeatNote> onReturnToPool;

        private INoteVisualSettings visualSettings;
        private NotificationText notificationTextComponent;

        private Aimless.RhythmManagerOSUAimless rhythmManager;

        public void Initialise(
            double relativeHitTime,

            float approachTime,
            System.Action<double> onHit,
            System.Action onMiss,
            System.Action<OSUBeatNote> onReturnToPool,
            INoteVisualSettings visualSettings,
            Vector3 worldPosition,
            GameObject notificationText)
        
        {
            this.RelativeHitTime = relativeHitTime;

            this.approachTime = approachTime;
            this.onHit = onHit;
            this.onMiss = onMiss;
            this.onReturnToPool = onReturnToPool;
            this.visualSettings = visualSettings;
            this.WorldPosition = worldPosition;
            this.notificationText = notificationText;
            this.notificationTextComponent = notificationText != null ? notificationText.GetComponent<NotificationText>() : null;
            this.rhythmManager = Aimless.RhythmManagerOSUAimless.Instance;

            HasProcessed = false;
            IndicatorSoundPlayed = false;
            hitCircle.color = defaultColour;

            if (approachRing && visualSettings.ShowApproachRing)
            {
                approachRing.gameObject.SetActive(true);
                approachRing.rectTransform.localScale = Vector3.one;
            }

            gameObject.SetActive(true);
            if (notificationTextComponent != null)
                notificationTextComponent.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (rhythmManager == null || rhythmManager.CurrentState == Aimless.GameState.Paused)
                return;
            if (HasProcessed)
                return;

            double nowSong = rhythmManager.SongTimeNow();
            AnimateApproachRing(nowSong);

            if (nowSong > RelativeHitTime + 0.2)
                ProcessMiss();
        }


        public void ProcessHit()
        {
            if (HasProcessed)
                return;
            HasProcessed = true;

            // Use pause-aware time for hit calculation
            double nowSong = rhythmManager != null ? rhythmManager.SongTimeNow() : 0.0;
            double delta = nowSong - RelativeHitTime;

            string result = null;
            void JudgementHandler(string ret, int _) => result = ret;
            JudgementSystem.Instance.OnJudgement += JudgementHandler;
            onHit?.Invoke(delta);
            JudgementSystem.Instance.OnJudgement -= JudgementHandler;
            
            Color feedbackColor = result switch
            {
                "Perfect" => new Color(0, 1, 0, .35f), // green
                "Good" => new Color(1, 0.92f, 0.016f, 0.35f), // yellow
                "Miss" => new Color(1, 0, 0, 0.35f), // red
                _ => Color.blue
            };

            ShowNotification(result);

            // Check if gameObject is still active before starting coroutine
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HitFeedbackAndCleanup(feedbackColor));
            }
            else
            {
                // If inactive, return to pool immediately
                onReturnToPool?.Invoke(this);
            }
        }

        public void ProcessMiss()
        {
            if (HasProcessed)
                return;
            HasProcessed = true;
            onMiss?.Invoke();
            ShowNotification("Miss");
            
            // Check if gameObject is still active before starting coroutine
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HitFeedbackAndCleanup(new Color(1, 0, 0, 0.35f)));
            }
            else
            {
                // If inactive, return to pool immediately
                onReturnToPool?.Invoke(this);
            }
        }

        private void ShowNotification(string result)
        {
            if (notificationTextComponent != null)
            {
                notificationTextComponent.gameObject.SetActive(true);
                notificationTextComponent.Initialise(
                    result,
                    1f,
                    notif =>
                    {
                        // Return to pool via RhythmManagerOSUAimless
                        Rhythm.GamePlay.OSU.Aimless.RhythmManagerOSUAimless.Instance.ReturnNotificationTextToPool(notif);
                    }
                );
            }
        }

        private void AnimateApproachRing(double nowSong)
        {
            if (approachRing == null || !approachRing.gameObject.activeSelf)
                return;

            double start = RelativeHitTime - approachTime;
            float t = Mathf.Clamp01((float)((nowSong - start) / approachTime));
            approachRing.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one * 0.5f, t);
        }

        public IEnumerator HitFeedbackAndCleanup(Color feedbackColor, float delay = 0.1f)
        {
            hitCircle.color = feedbackColor;

            if (approachRing)
                approachRing.gameObject.SetActive(false);

            yield return new WaitForSeconds(delay);

            onReturnToPool?.Invoke(this);
        }
    }
}
