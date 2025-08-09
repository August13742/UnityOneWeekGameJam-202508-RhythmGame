using UnityEngine;
using System;

namespace Rhythm.Core
{

    /// <summary>
    /// A singleton hub for game-wide events.
    /// Ensures systems can communicate without direct dependencies.
    /// </summary>
    public class GameEvents : MonoBehaviour
    {
        public static GameEvents Instance
        {
            get; private set;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        // Event signature: The AudioClip to play, and the exact DSP time for scheduling.
        public event Action<AudioClip, double> OnPlayMusicScheduled;

        public void PlayMusicScheduled(AudioClip clip, double dspTime)
        {
            OnPlayMusicScheduled?.Invoke(clip, dspTime);
        }


    }
}

