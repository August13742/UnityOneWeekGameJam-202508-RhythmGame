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
        public double RelHit
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
            double relHitTime,
            NoteType type,
            float pixelsPerSecond,
            float hitBarAnchoredX,
            float laneAnchoredY)
        {
            RelHit = relHitTime;
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

            // Use pause-aware time for positioning
            double song = OSU.Aimless.RhythmManagerOSUAimless.Instance.SongTimeNow();
            float xNow = xHit + v * (float)(RelHit - song);
            rect.anchoredPosition = new Vector2(xNow, laneY);
            gameObject.SetActive(true);
    
        }

        public void UpdatePosition(double songNow)
        {
            float xNow = xHit + v * (float)(RelHit - songNow);
            rect.anchoredPosition = new Vector2(xNow, laneY);
        }

        public bool HasPassedHitZone(double songNow, float postGrace = 0.15f)
        {
            return songNow > RelHit + postGrace;
        }

        public void TriggerHitEffect()
        {
            if (fxTriggered)
                return;
            fxTriggered = true;
            
            // Check if gameObject is active before starting coroutine
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HitFX());
            }
            else
            {
                // If inactive, apply effect immediately without coroutine
                ApplyHitEffectImmediate();
            }
        }

        private void ApplyHitEffectImmediate()
        {
            if (noteImage)
            {
                noteImage.color = hitEffectColor;
                transform.localScale = Vector3.one * 1.2f;
            }
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

        private void OnDisable()
        {
            // Stop all coroutines when disabled to prevent errors
            StopAllCoroutines();
        }
    }
}
