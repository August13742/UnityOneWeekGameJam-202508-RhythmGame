using System;
using UnityEngine;

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
        // Example: Return to pool after 2 seconds
        Invoke(nameof(ReturnToPool), 2f);
    }

    private void ReturnToPool()
    {
        returnToPoolCallback?.Invoke(this.gameObject);
        activated = false;
    }
}
