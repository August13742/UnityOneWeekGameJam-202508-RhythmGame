using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DifficultyBar : MonoBehaviour
{
    [SerializeField] private Button easyBtn, normalBtn, hardBtn, insaneBtn;

    private string currentSongKey;
    private IReadOnlyDictionary<string, Dictionary<Difficulty, BeatmapData>> db;

    public System.Action<BeatmapData> OnBeatmapChosen; // hook to loader

    void Awake()
    {
        // Don't build the database in Awake - it might not be ready yet
        DisableAll();
    }

    void Start()
    {
        // Build the database in Start to ensure it's ready
        RefreshDatabase();
    }

    private void RefreshDatabase()
    {
        Debug.Log("DifficultyBar: Refreshing beatmap database...");
        db = BeatmapIndex.Build();
        
        if (db == null)
        {
            Debug.LogError("DifficultyBar: Failed to build beatmap database!");
            return;
        }
        
        Debug.Log($"DifficultyBar: Database refreshed with {db.Count} songs");
    }

    public void SelectSong(string songKey)
    {
        Debug.Log($"DifficultyBar: SelectSong called with songKey: '{songKey}'");
        
        // Ensure database is available
        if (db == null)
        {
            Debug.LogWarning("DifficultyBar: Database is null, refreshing...");
            RefreshDatabase();
        }
        
        if (db == null)
        {
            Debug.LogError("DifficultyBar: Database is still null after refresh!");
            return;
        }

        currentSongKey = songKey;
        DisableAll();

        if (string.IsNullOrEmpty(songKey))
        {
            Debug.LogWarning("DifficultyBar: songKey is null or empty");
            return;
        }

        if (!db.TryGetValue(songKey, out var byDiff))
        {
            Debug.LogWarning($"DifficultyBar: Song '{songKey}' not found in database. Available songs:");
            foreach (var key in db.Keys)
            {
                Debug.Log($"  - {key}");
            }
            return;
        }

        Debug.Log($"DifficultyBar: Found song '{songKey}' with {byDiff.Count} difficulties");

        Setup(easyBtn, byDiff, Difficulty.Easy);
        Setup(normalBtn, byDiff, Difficulty.Normal);
        Setup(hardBtn, byDiff, Difficulty.Hard);
        Setup(insaneBtn, byDiff, Difficulty.Insane);
    }

    private void Setup(Button b,
        IReadOnlyDictionary<Difficulty, BeatmapData> byDiff,
        Difficulty d)
    {
        if (b == null)
        {
            Debug.LogWarning($"DifficultyBar: Button for difficulty {d} is null!");
            return;
        }

        if (!byDiff.TryGetValue(d, out var bm))
        {
            Debug.Log($"DifficultyBar: Difficulty {d} not available for song '{currentSongKey}'");
            b.interactable = false;
            return;
        }
        
        Debug.Log($"DifficultyBar: Setting up difficulty {d} for song '{currentSongKey}'");
        b.interactable = true;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() => {
            Debug.Log($"DifficultyBar: Difficulty {d} selected for song '{currentSongKey}'");
            OnBeatmapChosen?.Invoke(bm);
        });
    }

    private void DisableAll()
    {
        var buttons = new[] { easyBtn, normalBtn, hardBtn, insaneBtn };
        foreach (var b in buttons)
        {
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.interactable = false;
            }
            else
            {
                Debug.LogWarning("DifficultyBar: One of the difficulty buttons is null!");
            }
        }
    }

    // Public method to manually refresh the database (useful for debugging)
    [ContextMenu("Refresh Database")]
    public void ManualRefreshDatabase()
    {
        RefreshDatabase();
    }

    // Method to validate button references
    [ContextMenu("Validate Button References")]
    public void ValidateButtons()
    {
        Debug.Log("DifficultyBar: Validating button references:");
        Debug.Log($"  Easy Button: {(easyBtn != null ? "OK" : "NULL")}");
        Debug.Log($"  Normal Button: {(normalBtn != null ? "OK" : "NULL")}");
        Debug.Log($"  Hard Button: {(hardBtn != null ? "OK" : "NULL")}");
        Debug.Log($"  Insane Button: {(insaneBtn != null ? "OK" : "NULL")}");
    }
}
