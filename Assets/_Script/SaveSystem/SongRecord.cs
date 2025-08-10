[System.Serializable]
public class SongRecord
{
    public int highScore;
    public float bestAccuracy; // 0..1
    public int maxCombo;

    public int notesHit;    // perfect+good
    public int totalNotes;
    public int bestPerfect;
    public int bestGood;
    public int bestMiss;
}
