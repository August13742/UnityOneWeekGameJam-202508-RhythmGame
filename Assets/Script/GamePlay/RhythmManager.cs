using System.Collections.Generic;
using UnityEngine;

namespace Rhythm.GamePlay
{

    public class RhythmManager : MonoBehaviour
    {
        [SerializeField] private OSUBeatNote beatNotePrefab;
        [SerializeField] private Canvas canvas;

        // A simple queue to hold active notes. A real game would need a more robust system.
        private readonly Queue<OSUBeatNote> activeNotes = new Queue<OSUBeatNote>();

        void Update()
        {
            // Press 'S' to spawn a new note for demonstration.
            if (Input.GetKeyDown(KeyCode.S))
            {
                SpawnNote();
            }

            // Press 'Space' to attempt a hit on the oldest active note.
            // This input handling is laughably simplistic. It assumes the player
            // must always hit the oldest note first, which is standard for this game type.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (activeNotes.Count > 0)
                {
                    // Dequeue the note, whether the hit is successful or not.
                    OSUBeatNote noteToHit = activeNotes.Dequeue();
                    noteToHit.AttemptHit();
                }
            }

            // Clean up any notes that were missed and are now inactive.
            // This is important because a missed note is not dequeued by player input.
            if (activeNotes.Count > 0 && !activeNotes.Peek().gameObject.activeSelf)
            {
                activeNotes.Dequeue();
            }
        }

        private void SpawnNote()
        {
            OSUBeatNote newNote = Instantiate(beatNotePrefab, canvas.transform);

            RectTransform rt = newNote.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(Random.Range(-720, 0), Random.Range(-300, 0));//

            // --- FIX IS HERE ---
            // The duration of the approach animation should match how far in the future the note is scheduled.
            float leadInTime = 1.0f; // How far ahead to schedule the hit.
            float approachTime = leadInTime; // The animation should last for this entire duration.

            float hitTime = (float)AudioSettings.dspTime + leadInTime;

            newNote.Initialise(hitTime, approachTime);
            activeNotes.Enqueue(newNote);
        }
    }
}
