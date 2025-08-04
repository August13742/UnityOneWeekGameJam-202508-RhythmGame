using System.Collections.Generic;
using UnityEngine;
namespace Rhythm.GamePlay
{
    public class RhythmManager : MonoBehaviour
    {
        public float delay = 3f;
        [SerializeField] private BeatmapData beatmap;
        [SerializeField] private OSUBeatNote notePrefab;
        [SerializeField] private RectTransform noteParentCanvas;
        [SerializeField] private float audioOffset = 0.0f; // Add calibration parameter

        private double dspSongStartTime;
        private int spawnIndex = 0;

        // --- Pooling fields ---
        [Header("Pooling")]
        [SerializeField] private int poolSize = 8;
        private readonly Queue<OSUBeatNote> notePool = new();

        private void Awake()
        {
            // Pre-instantiate pool objects and hide them
            for (int i = 0; i < poolSize; i++)
            {
                var note = Instantiate(notePrefab, noteParentCanvas);
                note.gameObject.SetActive(false);
                notePool.Enqueue(note);
            }
        }

        private void Start()
        {
            // Start audio after a 3-second delay
            AudioSource audioSource = GetComponent<AudioSource>();
            double startTime = AudioSettings.dspTime + delay;
            audioSource.clip = beatmap.musicTrack;
            audioSource.PlayScheduled(startTime);
            dspSongStartTime = startTime;

            // Synchronize countdown
            var countdownText = FindFirstObjectByType<Rhythm.UI.CountDownText>();
            if (countdownText != null)
                countdownText.SetScheduledStartTime(dspSongStartTime);
        }

        private void Update()
        {
            double dspNow = AudioSettings.dspTime;
            double songTime = dspNow - dspSongStartTime;

            // Spawn all notes whose hitTime is within the lead-in window
            while (spawnIndex < beatmap.notes.Count &&
                   beatmap.notes[spawnIndex].hitTime - beatmap.approachTime <= songTime)
            {
                SpawnNote(beatmap.notes[spawnIndex]);
                spawnIndex++;
            }
        }

        private void SpawnNote(BeatNoteData data)
        {
            OSUBeatNote note = GetNoteFromPool();
            var rect = note.GetComponent<RectTransform>();
            rect.SetParent(noteParentCanvas, false);
            rect.anchoredPosition = new Vector2(Random.Range(-720, 0), Random.Range(-300, 0));

            note.Initialise(
                dspSongStartTime + data.hitTime + audioOffset, // Add offset to compensate for audio latency
                beatmap.approachTime,
                delta => JudgementSystem.Instance.RegisterHit(delta),
                () => JudgementSystem.Instance.RegisterMiss()
            );
        }

        private OSUBeatNote GetNoteFromPool()
        {
            if (notePool.Count > 0)
            {
                var note = notePool.Dequeue();
                note.gameObject.SetActive(true);
                return note;
            }
            // If pool is empty, instantiate as fallback (should be rare)
            var newNote = Instantiate(notePrefab, noteParentCanvas);
            return newNote;
        }

        public void ReturnNoteToPool(OSUBeatNote note)
        {
            note.gameObject.SetActive(false);
            notePool.Enqueue(note);
        }
    }
}
