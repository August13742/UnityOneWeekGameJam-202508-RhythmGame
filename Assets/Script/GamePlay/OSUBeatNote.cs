using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rhythm.GamePlay
{
    public class OSUBeatNote : MonoBehaviour, IPointerDownHandler
    {
        [Header("Visual References")]
        [SerializeField] private Image hitCircle;
        [SerializeField] private RawImage approachRing;

        // Assigned at spawn:
        private double hitTime;
        public double Hittime => hitTime;
        private float approachTime;
        private bool hasProcessed = false;

        // Callbacks injected by the spawner:
        private Action<double> onHit;  // delta -> JudgmentSystem.RegisterHit
        private Action onMiss;   // -> JudgmentSystem.RegisterMiss
        /// <summary>
        /// Initialise timing and callback hooks.
        /// </summary>
        public void Initialise(
            double hitTime,
            float approachTime,
            Action<double> onHit,
            Action onMiss)
        {
            this.hitTime = hitTime;
            this.approachTime = approachTime;
            this.onHit = onHit;
            this.onMiss = onMiss;

            hasProcessed = false;
            hitCircle.color = Color.white;      // reset any tint
            approachRing.rectTransform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (hasProcessed)
                return;

            double now = AudioSettings.dspTime;
            AnimateApproachRing(now);

            // grace period 0.2
            if (now > hitTime + approachTime * .2f)
            {
                hasProcessed = true;
                onMiss?.Invoke();
                StartCoroutine(HitFeedbackAndCleanup(Color.red));
            }
        }

        /// <summary>
        /// Shrinks ring from 1->0 over approachTime.
        /// </summary>
        private void AnimateApproachRing(double now)
        {
            double elapsed = now - (hitTime - approachTime);
            float t = Mathf.Clamp01((float)(elapsed / approachTime));

            approachRing.rectTransform.localScale = Vector3.Lerp(
                Vector3.one * 1.5f,   // start scale
                Vector3.one*0.5f,         // end, 0.5 because ring is 2 times as big
                t);
        }

        /// <summary>
        /// Called by the EventSystem on pointer down.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (hasProcessed)
                return;

            eventData.Use(); //consume input to prevent passing down

            hasProcessed = true;
            double now = AudioSettings.dspTime;
            string result = null; // Move declaration here, before usage
            double delta = now - hitTime;   // +ve = late, –ve = early

            void JudgmentHandler(string ret, int _)
            {
                result = ret;
            }

            JudgementSystem.Instance.OnJudgment += JudgmentHandler;
            onHit?.Invoke(delta);
            JudgementSystem.Instance.OnJudgment -= JudgmentHandler;

            Color feedbackColor = Color.blue;
            if (result == "Perfect")
                feedbackColor = Color.green;
            else if (result == "Good")
                feedbackColor = Color.yellow;
            else if (result == "Miss")
                feedbackColor = Color.red;

            StartCoroutine(HitFeedbackAndCleanup(feedbackColor));
        }

  
        public System.Collections.IEnumerator HitFeedbackAndCleanup(Color feedbackColor, float delay = 0.1f)
        {
            hitCircle.color = feedbackColor;
            yield return new WaitForSeconds(delay);
            RhythmManager rhythmManager =
                FindFirstObjectByType<Rhythm.GamePlay.RhythmManager>();
            // Instead of Destroy(gameObject):

            if (rhythmManager != null)
                rhythmManager.ReturnNoteToPool(this);
            else
                Destroy(gameObject);
        }
    }
}
