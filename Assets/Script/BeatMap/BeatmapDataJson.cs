using System.Collections.Generic;

[System.Serializable]
public class BeatNoteDataJson
{
    public double hitTime;
}

[System.Serializable]
public class BeatmapDataJson
{
    public string musicTrack;  // Just name; will be linked to actual AudioClip later
    public float approachTime;
    public List<BeatNoteDataJson> notes;
}
