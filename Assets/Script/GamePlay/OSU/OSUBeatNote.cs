using System;
using System.Collections.Generic;
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
        private Color defaultColour = new Vector4(1, 1, 1, 0.35f);

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
            hitCircle.color = defaultColour;      // reset any tint
            approachRing.rectTransform.localScale = Vector3.one;

            hitCircle.raycastTarget = true;
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

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = eventData.position
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (!IsNodeClosestToExpire(results))
                return;

            // Process hit
            eventData.Use();
            hasProcessed = true;

            double now = AudioSettings.dspTime;
            double delta = now - hitTime;

            string result = null;
            void JudgmentHandler(string ret, int _) => result = ret;

            JudgementSystem.Instance.OnJudgment += JudgmentHandler;
            onHit?.Invoke(delta);
            JudgementSystem.Instance.OnJudgment -= JudgmentHandler;

            Color feedbackColor = result switch
            {
                "Perfect" => Color.green,
                "Good" => Color.yellow,
                "Miss" => Color.red,
                _ => Color.blue
            };

            StartCoroutine(HitFeedbackAndCleanup(feedbackColor));
        }
        bool IsNodeClosestToExpire(List<RaycastResult> results)
        {
            OSUBeatNote closestToExpireNote = null;
            double earliestHitTime = double.MaxValue;
            
            foreach (var result in results)
            {
                var note = result.gameObject.GetComponentInParent<OSUBeatNote>();
                if (note != null && !note.hasProcessed)
                {
                    // The note with the earliest hit time is the one closest to expiring
                    if (note.Hittime < earliestHitTime)
                    {
                        closestToExpireNote = note;
                        earliestHitTime = note.Hittime;
                    }
                }
            }
            
            // Return true if this note is the one closest to expiring (bottom-most)
            return closestToExpireNote == this;
        }



        public System.Collections.IEnumerator HitFeedbackAndCleanup(Color feedbackColor, float delay = 0.05f)
        {
            hitCircle.color = feedbackColor;
            hitCircle.raycastTarget = false;
            yield return new WaitForSeconds(delay);

            RhythmManagerOSU.Instance.ReturnNoteToPool(this);

        }
    }
}
