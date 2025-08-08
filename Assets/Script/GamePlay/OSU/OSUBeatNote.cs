using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Rhythm.UI;

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
        public double HitTime
        {
            get; private set;
        }
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

        public void Initialise(
            double hitTime,
            double relativeHitTime,
            float approachTime,
            System.Action<double> onHit,
            System.Action onMiss,
            System.Action<OSUBeatNote> onReturnToPool,
            INoteVisualSettings visualSettings,
            Vector3 worldPosition,
            GameObject notificationText)
        
        {
            this.HitTime = hitTime;
            this.RelativeHitTime = relativeHitTime;
            this.approachTime = approachTime;
            this.onHit = onHit;
            this.onMiss = onMiss;
            this.onReturnToPool = onReturnToPool;
            this.visualSettings = visualSettings;
            this.WorldPosition = worldPosition;
            this.notificationText = notificationText;
            this.notificationTextComponent = notificationText != null ? notificationText.GetComponent<NotificationText>() : null;

            HasProcessed = false;
            IndicatorSoundPlayed = false; // Add this line
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
            if (HasProcessed)
                return;

            double now = AudioSettings.dspTime;
            AnimateApproachRing(now);


            if (now > HitTime + 0.2)
            {
                ProcessMiss();
            }
        }

        public void ProcessHit()
        {
            if (HasProcessed)
                return;
            HasProcessed = true;

            double now = AudioSettings.dspTime;
            double delta = now - HitTime;

            string result = null;
            void JudgmentHandler(string ret, int _) => result = ret;
            JudgementSystem.Instance.OnJudgment += JudgmentHandler;
            onHit?.Invoke(delta);
            JudgementSystem.Instance.OnJudgment -= JudgmentHandler;
            Color feedbackColor = result switch
            {
                "Perfect" => new Color(0, 1, 0, .35f), // green
                "Good" => new Color(1, 0.92f, 0.016f, 0.35f), // yellow
                "Miss" => new Color(1, 0, 0, 0.35f), // red
                _ => Color.blue
            };

            ShowNotification(result);

            StartCoroutine(HitFeedbackAndCleanup(feedbackColor));
        }

        public void ProcessMiss()
        {
            if (HasProcessed)
                return;
            HasProcessed = true;
            onMiss?.Invoke();
            ShowNotification("Miss");
            StartCoroutine(HitFeedbackAndCleanup(new Color(1, 0, 0, 0.35f)));
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

        private void AnimateApproachRing(double now)
        {
            double elapsed = now - (HitTime - approachTime);
            float t = Mathf.Clamp01((float)(elapsed / approachTime));
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
