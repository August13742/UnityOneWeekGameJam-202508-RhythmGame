using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongRowView : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text songName;
    public Button rowSelectBtn;
    public Button easyBtn, normalBtn, hardBtn, insaneBtn;

    [Header("Row Sections (parents start INACTIVE)")]
    public GameObject difficultyGroupRoot;
    public GameObject toggleGroupUIRoot;
    public GameObject previousResultsRoot;

    public Button startButton;

    [Header("Gameplay Toggles")]
    public Toggle autoPlayToggle;
    public Toggle indicatorToggle;
    public Toggle approachRingToggle;

    [Header("Stats Texts in PreviousResultsGroup")]
    public TMP_Text bestScore;
    public TMP_Text note;
    public TMP_Text accuracy;
    public TMP_Text perfect;
    public TMP_Text good;
    public TMP_Text miss;
    public TMP_Text maxCombo;

    public void Collapse()
    {
        if (difficultyGroupRoot)
            difficultyGroupRoot.SetActive(false);
        if (toggleGroupUIRoot)
            toggleGroupUIRoot.SetActive(false);
        if (previousResultsRoot)
            previousResultsRoot.SetActive(false);
    }

    public void Expand()
    {
        if (difficultyGroupRoot)
            difficultyGroupRoot.SetActive(true);
        if (toggleGroupUIRoot)
            toggleGroupUIRoot.SetActive(true);
        if (previousResultsRoot)
            previousResultsRoot.SetActive(true);
    }

    public void SetExpanded(bool expanded)
    {
        if (expanded)
            Expand();
        else
            Collapse();
    }
}
