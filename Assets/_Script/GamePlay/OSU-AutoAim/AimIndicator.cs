using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Rhythm.GamePlay.OSU.Aimless
{

    public class AimIndicator : MonoBehaviour
    {
        private RectTransform rectTransform;
        [SerializeField] private RawImage indicatorImage;
        [SerializeField] private float scaleFactor = 1.2f; 
        [SerializeField] private float cycleDuration = 0.5f; 
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        private void Start()
        {
            indicatorImage.transform
                .DOScale(Vector3.one * scaleFactor, cycleDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
            indicatorImage.transform.DORotate(new Vector3(0, 0, 10), cycleDuration, RotateMode.Fast)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.Linear);
        }
        public void Initialise(RectTransform target, float duration)
        {
            // Kill any previously running tweens on this object.
            rectTransform.DOKill();

            // Start the new tween from its current position to the target's position.
            rectTransform.DOAnchorPos(target.anchoredPosition, duration)
                         .SetEase(Ease.InOutQuint);
        }
    }
}
