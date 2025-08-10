using System.Collections.Generic;
using DG.Tweening;
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
        Debug.Log($"Starting song '{songKey}' with AutoPlay: {AutoPlayState}, Indicator: {ShowIndicatorState}, SFX: {ShowPerfectSFXState}");
        
        // Set game parameters
        Rhythm.Core.GameStartParameters.SetParameters(
            beatmap, 
            songKey, 
            difficulty, 
            AutoPlayState, 
            ShowIndicatorState, 
            ShowApproachRingState, 
            ShowPerfectSFXState
        );

        // Add diagnostic logging
        Debug.Log("About to start crossfade to black...");
        
        if (CrossfadeManager.Instance == null)
        {
            Debug.LogError("CrossfadeManager.Instance is null!");
            yield break;
        }

        var fadeToBlackTween = CrossfadeManager.Instance.FadeToBlack(1f);
        
        if (fadeToBlackTween == null)
        {
            Debug.LogError("FadeToBlack returned null tween!");
            yield break;
        }

        Debug.Log("Waiting for crossfade to complete...");
        yield return fadeToBlackTween.WaitForCompletion();
        
        Debug.Log("Crossfade complete, about to load scene...");
        
        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("RhythmRevolver");
            Debug.Log("Scene load initiated successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load GameScene: {e.Message}");
        }
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
