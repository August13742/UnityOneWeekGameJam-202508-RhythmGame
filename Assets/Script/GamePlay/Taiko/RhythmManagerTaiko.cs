using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rhythm.GamePlay
{

    /// <summary>
    /// Singleton
    /// </summary>
    public class RhythmManagerTaiko : MonoBehaviour
    {

        public static RhythmManagerTaiko Instance
        {
            get; private set;
        }


        [Header("Assets and Prefabs")]
        [SerializeField] private BeatmapData beatmap;
        [SerializeField] private TaikoNote prefabA;
        [SerializeField] private TaikoNote prefabB;
        [SerializeField] private Canvas canvas;

        [Header("Gameplay Settings")]
        [SerializeField] private float pixelsPerSecond = 400f;
        [SerializeField] private float approachTime = 1.0f;
        //[SerializeField] private float perfectWindow = 0.1f;
        [SerializeField] private float goodWindow = 0.2f;
        [SerializeField] private float missWindow = 0.25f;

        private double dspSongStartTime;
        private int spawnIndex = 0;

        private readonly Queue<TaikoNote> pool = new();
        private readonly List<TaikoNote> activeNotes = new();

        private AudioSource audioSource;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = beatmap.musicTrack;          // <— 1. guarantee clip
            dspSongStartTime = AudioSettings.dspTime + 1.5; // 1.5 s lead-in
            audioSource.PlayScheduled(dspSongStartTime);
        }

        private void Update()
        {
            double now = AudioSettings.dspTime;
            double songTime = now - dspSongStartTime;

            // Spawn notes
            while (spawnIndex < beatmap.notes.Count &&
                   beatmap.notes[spawnIndex].hitTime - approachTime <= songTime)
            {
                SpawnNote(beatmap.notes[spawnIndex++]);
            }

            // Move & check notes
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var note = activeNotes[i];
                note.UpdatePosition(now);
                note.MissCheck(now, missWindow);
            }

            // Input
            if (Input.GetKeyDown(KeyCode.F))
                HandleInput(NoteType.A, now);
            if (Input.GetKeyDown(KeyCode.J))
                HandleInput(NoteType.B, now);
        }

        private void SpawnNote(BeatNoteData data)
        {
            var prefab = data.type == NoteType.A ? prefabA : prefabB;
            var note = GetFromPool(prefab);

            note.transform.SetParent(canvas.transform, false);

            
            double absHit = dspSongStartTime + data.hitTime;

            note.Initialise(
                absHit,
                data.type,
                pixelsPerSecond,
                delta => JudgementSystem.Instance.RegisterHit(delta),
                () => JudgementSystem.Instance.RegisterMiss()
            );

            activeNotes.Add(note);
        }

        private void HandleInput(NoteType type, double now)
        {
            float bestDelta = float.MaxValue;
            TaikoNote bestNote = null;

            foreach (var note in activeNotes)
            {
                if (note.Type != type)
                    continue;

                double delta = now - note.HitTime;
                float absDelta = Mathf.Abs((float)delta);

                if (absDelta < bestDelta && absDelta <= goodWindow)
                {
                    bestDelta = absDelta;
                    bestNote = note;
                }
            }

            if (bestNote != null)
            {
                bestNote.ProcessHit(now - bestNote.HitTime);
                activeNotes.Remove(bestNote);
            }
            else
            {
                JudgementSystem.Instance.RegisterMiss();
            }
        }

        private TaikoNote GetFromPool(TaikoNote prefab)
        {
            if (pool.Count > 0)
            {
                var note = pool.Dequeue();
                note.gameObject.SetActive(true);
                return note;
            }
            return Instantiate(prefab, transform); // fallback
        }

        public void RecycleNote(TaikoNote note)
        {
            note.ResetNote();
            pool.Enqueue(note);
            activeNotes.Remove(note);
        }
    }
}
