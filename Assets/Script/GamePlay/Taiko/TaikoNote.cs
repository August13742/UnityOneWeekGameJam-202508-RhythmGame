using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.CanvasScaler;

namespace Rhythm.GamePlay
{
    public enum NoteType
    {
        A, B
    }

    public class TaikoNote : MonoBehaviour
    {
        private float checkPosX = -256f;
        private RectTransform rect;
        private double hitTime;         // Absolute DSP time when this note should reach the hit bar
        private float speed;            // Pixels per second
        private bool hasProcessed = false;
        private NoteType type;

        private RhythmManagerTaiko manager;
        private Image noteImage;

        private Action<double> onHit;   // callback to JudgementSystem.RegisterHit
        private Action onMiss;   // callback to JudgementSystem.RegisterMiss

        public NoteType Type => type;
        public double HitTime => hitTime;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            noteImage = transform.GetChild(0).GetComponent<Image>();
        }

        public void Initialise(
            double hitTime,
            NoteType type,
            float speed,
            RhythmManagerTaiko manager,
            Action<double> onHit,
            Action onMiss)
        {
            this.hitTime = hitTime;
            this.type = type;
            this.speed = speed;
            this.manager = manager;
            this.onHit = onHit;   // store callbacks
            this.onMiss = onMiss;

            hasProcessed = false;
            noteImage.color = Color.white;
            gameObject.SetActive(true);
        }

        public void UpdatePosition(double dspNow)
        {
            if (hasProcessed)
                return;

            double timeUntilHit = hitTime - dspNow;
            float newX = checkPosX + (float)(timeUntilHit * speed);
            rect.anchoredPosition = new Vector2(newX, 0f);
        }

        public void MissCheck(double dspNow, float gracePeriod)
        {
            if (hasProcessed)
                return;

            if (dspNow > hitTime + gracePeriod)
            {
                hasProcessed = true;
                onMiss?.Invoke();                         // ← use injected callback
                StartCoroutine(FeedbackAndRecycle(Color.red));
            }
        }

        public void ProcessHit(double delta)
        {
            if (hasProcessed)
                return;
            hasProcessed = true;

            string result = null;
            void JudgmentHandler(string j, int _) => result = j;

            JudgementSystem.Instance.OnJudgment += JudgmentHandler;
            onHit?.Invoke(delta);                     // ← call injected RegisterHit
            JudgementSystem.Instance.OnJudgment -= JudgmentHandler;

            Color feedback = result switch
            {
                "Perfect" => Color.green,
                "Good" => Color.yellow,
                _ => Color.red
            };

            StartCoroutine(FeedbackAndRecycle(feedback));
        }

        private System.Collections.IEnumerator FeedbackAndRecycle(Color colour, float wait = 0.1f)
        {
            if (noteImage != null)
                noteImage.color = colour;   // show feedback
            yield return new WaitForSeconds(wait);             // let player see it
            manager.RecycleNote(this);                         // returns to pool, ResetNote() restores white
        }


        public void ResetNote()
        {
            if (noteImage != null)
                noteImage.color = Color.white;
            gameObject.SetActive(false);
        }
    }
}
