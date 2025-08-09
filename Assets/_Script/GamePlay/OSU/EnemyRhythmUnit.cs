using System;
using UnityEngine;

namespace Rhythm.GamePlay.OSU
{
    public class EnemyRhythmUnit : MonoBehaviour
    {
        private double relHitTime; // song-time moment this should trigger
        private bool activated;
        private Action<GameObject> returnToPoolCallback;
        private Aimless.RhythmManagerOSUAimless rhythmManager;

        private void Awake()
        {
            rhythmManager = Aimless.RhythmManagerOSUAimless.Instance;
        }

        public void SetRelativeHitTime(double t)
        {
            relHitTime = t;
            activated = false;
        }

        public void SetReturnToPoolCallback(Action<GameObject> callback)
        {
            returnToPoolCallback = callback;
        }

        private void Update()
        {
            if (rhythmManager == null || rhythmManager.CurrentState == Aimless.GameState.Paused)
                return;

            if (!activated)
            {
                double songNow = rhythmManager.SongTimeNow();
                if (songNow >= relHitTime)
                {
                    Activate();
                    activated = true;
                }
            }
        }

        private void Activate()
        {
            if (gameObject.activeInHierarchy)
            {
                // Play animation, SFX, etc.
                Invoke(nameof(ReturnToPool), 0.5f); // example lifetime
            }
            else
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            returnToPoolCallback?.Invoke(gameObject);
            activated = false;
        }

        private void OnDisable()
        {
            CancelInvoke();
        }
    }
}
