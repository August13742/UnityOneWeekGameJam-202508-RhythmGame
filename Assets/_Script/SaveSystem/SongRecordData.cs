using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Rhythm/SongRecordsDB")]
public class SongRecordsDB : ScriptableObject
{
    // store records in memory
    public Dictionary<string, SongRecord> records = new Dictionary<string, SongRecord>();

    public SongRecord GetRecord(string songId, Difficulty difficulty)
    {
        string key = GetKey(songId, difficulty);
        if (records.TryGetValue(key, out var record))
            return record;
        // Try to load from PlayerPrefs if not in memory
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            var loaded = JsonUtility.FromJson<SongRecord>(json);
            records[key] = loaded;
            return loaded;
        }
        return null;
    }

    public void SaveRecord(string songId, Difficulty difficulty, int score, float accuracy, int combo)
    {
        string key = GetKey(songId, difficulty);
        if (!records.ContainsKey(key))
            records[key] = new SongRecord();

        var rec = records[key];
        rec.highScore = Mathf.Max(rec.highScore, score);
        rec.bestAccuracy = Mathf.Max(rec.bestAccuracy, accuracy);
        rec.maxCombo = Mathf.Max(rec.maxCombo, combo);

        // Save to PlayerPrefs
        string json = JsonUtility.ToJson(rec);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    private string GetKey(string songId, Difficulty d) => $"{songId}_{d}";
}
