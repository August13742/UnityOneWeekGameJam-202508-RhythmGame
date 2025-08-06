using System;
using UnityEngine;

namespace Rhythm.GamePlay.OSU
{

    public class EnemyRhythmUnit : MonoBehaviour
    {
        private double hitTime;
        private bool activated;
        private Action<GameObject> returnToPoolCallback;

        public void SetHitTime(double absoluteTime)
        {
            hitTime = absoluteTime;
        }

        public void SetReturnToPoolCallback(Action<GameObject> callback)
        {
            returnToPoolCallback = callback;
        }

        private void Update()
        {
            if (!activated && AudioSettings.dspTime >= hitTime)
            {
                Activate();
                activated = true;
            }
        }

        private void Activate()
        {
            // anim, etc
            // Example: Return to pool after seconds
            Invoke(nameof(ReturnToPool), .5f);
        }

        private void ReturnToPool()
        {
            returnToPoolCallback?.Invoke(this.gameObject);
            activated = false;
        }
    }
}
