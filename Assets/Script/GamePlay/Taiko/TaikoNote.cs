using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Rhythm.GamePlay.Taiko
{
    public enum NoteType
    {
        A, B
    }

    public class TaikoNote : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform rect;
        [SerializeField] private Image noteImage;

        [Header("Colours")]
        [SerializeField] private Color typeAColor = Color.red;
        [SerializeField] private Color typeBColor = Color.blue;
        [SerializeField] private Color hitEffectColor = Color.yellow;

        [Header("FX")]
        [SerializeField] private float hitEffectDuration = 0.2f;

        public double HitTime
        {
            get; private set;
        }
        public NoteType Type
        {
            get; private set;
        }

        // cached lane params (assigned at Initialise)
        private float xHit;           // anchored X of hit-bar center (same space as rect)
        private float laneY;          // anchored Y for the lane
        private float v;              // px/s
        private Color baseColor;
        private bool fxTriggered;

        void Reset()
        {
            rect = GetComponent<RectTransform>();
            noteImage = GetComponentInChildren<Image>();
        }

        public void Initialise(
            double absHitTime,
            NoteType type,
            float pixelsPerSecond,
            float hitBarAnchoredX,
            float laneAnchoredY)
        {
            HitTime = absHitTime;
            Type = type;
            v = pixelsPerSecond;
            xHit = hitBarAnchoredX;
            laneY = laneAnchoredY;
            fxTriggered = false;

            baseColor = (type == NoteType.A) ? typeAColor : typeBColor;
            if (noteImage)
                noteImage.color = baseColor;

            if (!rect)
                rect = GetComponent<RectTransform>();

            // place instantly to correct x for current DSP time (no popping/jump)
            double t = AudioSettings.dspTime;
            float xNow = xHit + v * (float)(HitTime - t);
            rect.anchoredPosition = new Vector2(xNow, laneY);

            gameObject.SetActive(true);
        }

        public void UpdatePosition(double dspNow)
        {
            // exact kinematic: x(t) = x_hit + v * (HitTime - t)
            float xNow = xHit + v * (float)(HitTime - dspNow);
            rect.anchoredPosition = new Vector2(xNow, laneY);
        }

        public bool HasPassedHitZone(double dspNow, float postGrace = 0.15f)
        {
            return dspNow > HitTime + postGrace; // purely time-based
        }

        public void TriggerHitEffect()
        {
            if (fxTriggered)
                return;
            fxTriggered = true;
            StartCoroutine(HitFX());
        }

        IEnumerator HitFX()
        {
            if (!noteImage)
                yield break;
            var orig = transform.localScale;
            noteImage.color = hitEffectColor;
            transform.localScale = orig * 1.2f;
            yield return new WaitForSeconds(hitEffectDuration * 0.5f);
            noteImage.color = baseColor;
            transform.localScale = orig;
            yield return new WaitForSeconds(hitEffectDuration * 0.5f);
        }

        public void ResetNote()
        {
            StopAllCoroutines();
            fxTriggered = false;
            if (noteImage)
                noteImage.color = baseColor;
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }
}
