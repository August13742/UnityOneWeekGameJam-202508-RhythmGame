using System.Collections.Generic;
using UnityEngine;

public static class BeatmapIndex
{
    public static Dictionary<string, Dictionary<Difficulty, BeatmapData>> Build()
    {
        var result = new Dictionary<string, Dictionary<Difficulty, BeatmapData>>();
        
        Debug.Log("BeatmapIndex: Starting to build beatmap database...");
        
        // Load all BeatmapData assets from Resources/Beatmaps
        BeatmapData[] beatmaps = Resources.LoadAll<BeatmapData>("Beatmaps");
        
        Debug.Log($"BeatmapIndex: Found {beatmaps.Length} beatmaps in Resources/Beatmaps");
        
        if (beatmaps.Length == 0)
        {
            Debug.LogWarning("BeatmapIndex: No beatmaps found in Resources/Beatmaps folder. Make sure your BeatmapData assets are in the correct location.");
            
            // Check if the folder exists
            var testLoad = Resources.Load("Beatmaps");
            if (testLoad == null)
            {
                Debug.LogError("BeatmapIndex: Resources/Beatmaps folder doesn't exist or is empty!");
            }
            
            return result;
        }
        
        foreach (var beatmap in beatmaps)
        {
            if (beatmap == null)
            {
                Debug.LogWarning("BeatmapIndex: Found null beatmap asset");
                continue;
            }
            
            Debug.Log($"BeatmapIndex: Processing beatmap: {beatmap.name}");
            
            if (beatmap.musicTrack == null)
            {
                Debug.LogWarning($"BeatmapIndex: Beatmap '{beatmap.name}' has no music track assigned");
                // Don't skip - still add it to the list
            }
            
            // Extract song name and difficulty from asset name
            string assetName = beatmap.name;
            string songKey = ExtractSongKey(assetName);
            Difficulty difficulty = ExtractDifficulty(assetName);
            
            Debug.Log($"BeatmapIndex: '{assetName}' -> Song: '{songKey}', Difficulty: {difficulty}");
            
            // Group by song key
            if (!result.ContainsKey(songKey))
            {
                result[songKey] = new Dictionary<Difficulty, BeatmapData>();
                Debug.Log($"BeatmapIndex: Created new song entry for: {songKey}");
            }
            
            result[songKey][difficulty] = beatmap;
        }
        
        Debug.Log($"BeatmapIndex: Build complete. Found {result.Count} unique songs:");
        foreach (var song in result.Keys)
        {
            Debug.Log($"  - {song} ({result[song].Count} difficulties)");
        }
        
        return result;
    }
    
    private static string ExtractSongKey(string assetName)
    {
        // Assuming format: "SongName_DIFFICULTY"
        int lastUnderscore = assetName.LastIndexOf('_');
        if (lastUnderscore > 0)
        {
            string songKey = assetName.Substring(0, lastUnderscore);
            Debug.Log($"BeatmapIndex: Extracted song key '{songKey}' from '{assetName}'");
            return songKey;
        }
        
        Debug.Log($"BeatmapIndex: No underscore found in '{assetName}', using full name as song key");
        return assetName; // Fallback to full name
    }
    
    private static Difficulty ExtractDifficulty(string assetName)
    {
        // Assuming format: "SongName_DIFFICULTY"
        int lastUnderscore = assetName.LastIndexOf('_');
        if (lastUnderscore < 0 || lastUnderscore >= assetName.Length - 1)
        {
            Debug.LogWarning($"BeatmapIndex: Cannot extract difficulty from '{assetName}', defaulting to Normal");
            return Difficulty.Normal;
        }
        
        string difficultyStr = assetName.Substring(lastUnderscore + 1).ToUpper();
        
        Difficulty result = difficultyStr switch
        {
            "EASY" => Difficulty.Easy,
            "NORMAL" => Difficulty.Normal,
            "HARD" => Difficulty.Hard,
            "INSANE" => Difficulty.Insane,
            _ => Difficulty.Normal
        };
        
        Debug.Log($"BeatmapIndex: Extracted difficulty '{result}' from '{difficultyStr}' in '{assetName}'");
        return result;
    }
}
