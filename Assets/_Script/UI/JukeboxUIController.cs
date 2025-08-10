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

    private bool autoPlayState = false;
    private bool showIndicatorState = true;
    private bool showApproachRingState = true;
    private bool showPerfectSFXState = true;

    private void OnEnable() => Rebuild();
    private void OnDisable() => ClearRows();

    private void Start()
    {
        CrossfadeManager.Instance.FadeFromBlack();
    }
    public void Rebuild()
    {
        ClearRows();

        var db = BeatmapIndex.Build();
        foreach (var kv in db)
        {
            SongRowController controller = Instantiate(rowPrefab, contentRoot);
            controller.Init(kv.Key, kv.Value, recordsDB, this);

            controller.OnExpandRequested += HandleExpandRequest;
            controller.OnStartRequested += InitiateGameStart;

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
    public void InitiateGameStart(BeatmapData beatmap, string songKey, Difficulty difficulty)
    {
        StartCoroutine(StartRequestCoroutine(beatmap, songKey, difficulty));
    }

    private System.Collections.IEnumerator StartRequestCoroutine(BeatmapData beatmap, string songKey, Difficulty difficulty)
    {
        Debug.Log($"Starting song '{songKey}' with AutoPlay: {autoPlayState}, Indicator: {showIndicatorState}, SFX: {showPerfectSFXState}");

        Rhythm.Core.GameStartParameters.SetParameters(
            beatmap,
            songKey,
            difficulty,
            this.autoPlayState,
            this.showIndicatorState,
            this.showApproachRingState,
            this.showPerfectSFXState
        );

        AudioManager.Instance.StopMusic();
        CrossfadeManager.Instance.FadeToBlack();
        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("RhythmGunman");
    }


    private void ClearRows()
    {
        foreach (var r in rows)
        {
            if (r)
            {
                r.OnExpandRequested -= HandleExpandRequest;
                r.OnStartRequested -= InitiateGameStart;
                Destroy(r.gameObject);
            }
        }
        rows.Clear();
        expandedRow = null;
    }

    public void SetAutoPlay(bool isOn)
    {
        this.autoPlayState = isOn;
    }

    public void SetShowIndicator(bool isOn)
    {
        this.showIndicatorState = isOn;
    }

    public void SetShowApproachRing(bool isOn)
    {
        this.showApproachRingState = isOn;
    }
    public void SetPerfectSFXState(bool isOn)
    {
        this.showPerfectSFXState = isOn;
    }
    public bool AutoPlayState => autoPlayState;
    public bool ShowIndicatorState => showIndicatorState;
    public bool ShowApproachRingState => showApproachRingState;
    public bool ShowPerfectSFXState => showPerfectSFXState;

}
