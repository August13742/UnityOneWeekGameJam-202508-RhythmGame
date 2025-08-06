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
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Stop any animation that is already running on this object.
            rectTransform.DOKill();

            // Start the new animation from its current position to the new target.
            rectTransform.DOAnchorPos(target.anchoredPosition, duration)
                         .SetEase(Ease.OutQuint);
        }
    }
}
