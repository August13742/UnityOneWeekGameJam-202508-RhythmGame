using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Beatmap")]
public class BeatmapData : ScriptableObject
{
    public AudioClip musicTrack;
    public float approachTime = 1.0f;
    public List<BeatNoteData> notes = new List<BeatNoteData>();
}
