using System;
using System.Collections.Generic;

[Serializable]
public class BeatmapDataJsonRoot
{
    public float approachTime;
    public List<BeatmapDataJson> notes;
}

[Serializable]
public class BeatmapDataJson
{
    public double hitTime;
    public NoteType type;
    public int spawnPointIndex;
}

public enum NoteType
{
    Tap = 0, Hold = 1, Swipe = 2
}
