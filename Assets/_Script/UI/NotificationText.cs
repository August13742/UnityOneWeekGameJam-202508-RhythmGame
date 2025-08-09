using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

namespace Rhythm.UI
{
    public class NotificationText : MonoBehaviour
    {
        private TMP_Text label;
        private float lifetime = 1f;
        [SerializeField] private float elevateHeight;
        [SerializeField] private float tweenSize = 1.5f;
        private RectTransform rectTransform;
        private Action<NotificationText> onReturnToPool;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Initialise(string text, float lifetime = 1f, Action<NotificationText> onReturnToPool = null)
        {
            this.lifetime = lifetime;
            this.onReturnToPool = onReturnToPool;
            label.text = text;
            rectTransform.localScale = Vector3.one;
            rectTransform.DOAnchorPos(rectTransform.anchoredPosition + new Vector2(0, elevateHeight), lifetime)
                .SetEase(Ease.OutElastic)
                .OnComplete(() =>
                {
                    onReturnToPool?.Invoke(this);
                });
            rectTransform.DOScale(tweenSize, lifetime * 0.5f).SetLoops(2, LoopType.Yoyo);
        }

        public void ResetText()
        {
            rectTransform.DOKill();
            rectTransform.localScale = Vector3.one;
            if (label != null)
                label.text = "";
            gameObject.SetActive(false);
        }
    }
}
