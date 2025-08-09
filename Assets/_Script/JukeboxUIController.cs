using UnityEngine;
using System.Collections.Generic;

public class JukeboxUIController : MonoBehaviour
{
    [SerializeField] private Transform contentRoot; // ScrollRect Content
    [SerializeField] private SongRowView rowPrefab;
    [SerializeField] private SongRecordsDB recordsDB;
    [SerializeField] private RecordsPanelController recordsPanel;

    private readonly List<GameObject> spawned = new();

    private void OnEnable() => Rebuild();

    public void Rebuild()
    {
        foreach (var go in spawned)
            Destroy(go);
        spawned.Clear();

        var db = BeatmapIndex.Build();

        foreach (var kv in db)
        {
            string songKey = kv.Key;
            var row = Instantiate(rowPrefab, contentRoot);
            row.songName.text = songKey;

            SetupButton(row.easyBtn, songKey, kv.Value, Difficulty.Easy);
            SetupButton(row.normalBtn, songKey, kv.Value, Difficulty.Normal);
            SetupButton(row.hardBtn, songKey, kv.Value, Difficulty.Hard);
            SetupButton(row.insaneBtn, songKey, kv.Value, Difficulty.Insane);

            spawned.Add(row.gameObject);
        }
    }

    private void SetupButton(UnityEngine.UI.Button btn, string songKey,
                             Dictionary<Difficulty, BeatmapData> diffs, Difficulty d)
    {
        if (diffs.TryGetValue(d, out var beatmap))
        {
            btn.gameObject.SetActive(true);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"Selected {songKey} [{d}]");
                var rec = recordsDB?.GetRecord(songKey, d);
                recordsPanel.ShowRecord(songKey, d, rec);

                // Later: start playing the song
            });
        }
        else
        {
            btn.gameObject.SetActive(false);
        }
    }
}
