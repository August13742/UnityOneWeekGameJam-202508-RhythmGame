using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Rhythm.GamePlay;

namespace Rhythm.Control
{
    public class NoteInputManager : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            OSUBeatNote noteToHit = null;
            double earliestHitTime = double.MaxValue;

            foreach (var result in results)
            {
                var note = result.gameObject.GetComponentInParent<OSUBeatNote>();
                if (note != null && !note.HasProcessed)
                {
                    // Find the note that is closest to its hit time
                    if (note.HitTime < earliestHitTime)
                    {
                        earliestHitTime = note.HitTime;
                        noteToHit = note;
                    }
                }
            }
            if (noteToHit != null)
            {
                noteToHit.ProcessHit();
            }
        }
    }
}
