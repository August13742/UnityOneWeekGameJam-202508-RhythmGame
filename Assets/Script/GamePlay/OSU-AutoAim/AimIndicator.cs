using UnityEngine;
using DG.Tweening;

namespace Rhythm.GamePlay.OSU.Aimless
{

    public class AimIndicator : MonoBehaviour
    {
        private RectTransform rectTransform;
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void Initialise(RectTransform target, float duration)
        {
            // Kill any previously running tweens on this object.
            rectTransform.DOKill();

            // Start the new tween from its current position to the target's position.
            rectTransform.DOAnchorPos(target.anchoredPosition, duration)
                         .SetEase(Ease.InOutSine);
        }
    }
}
