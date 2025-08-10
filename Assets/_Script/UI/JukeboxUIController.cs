using System.Collections.Generic;
using Rhythm.UI;
using UnityEngine;

public class JukeboxUIController : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SongRowController rowPrefab;
    [SerializeField] private SongRecordsDB recordsDB;

    private readonly List<SongRowController> rows = new();
    private SongRowController expandedRow;

    private void OnEnable() => Rebuild();
    private void OnDisable() => ClearRows();

    public void Rebuild()
    {
        ClearRows();

        var db = BeatmapIndex.Build(); // songKey -> {diff->BeatmapData}
        foreach (var kv in db)
        {
            SongRowController controller = Instantiate(rowPrefab, contentRoot);

            controller.Init(kv.Key, kv.Value, recordsDB);

            controller.OnExpandRequested += HandleExpandRequest;
            controller.OnStartRequested += HandleStartRequest;

            rows.Add(controller);
        }
    }

    // This method is called when ANY row asks to be expanded
    private void HandleExpandRequest(SongRowController requestedRow)
    {
        // If a different row is already expanded, collapse it.
        if (expandedRow != null && expandedRow != requestedRow)
        {
            expandedRow.Collapse();
        }
        // Track the newly expanded row.
        expandedRow = requestedRow;
    }

    // This method is called when a row's start button is pressed
    private void HandleStartRequest(BeatmapData beatmap, string songKey, Difficulty difficulty)
    {
        Debug.Log($"Starting song '{songKey}' on difficulty '{difficulty}'.");
        // TODO: Add your scene loading or game start logic here.
        UnityEngine.SceneManagement.SceneManager.LoadScene("RhythmGunman");
        // GameManager.Instance.StartSong(beatmap);
    }

    private void ClearRows()
    {
        foreach (var r in rows)
        {
            if (r)
            {
                r.OnExpandRequested -= HandleExpandRequest;
                r.OnStartRequested -= HandleStartRequest;
                Destroy(r.gameObject);
            }
        }
        rows.Clear();
        expandedRow = null;
    }
}
