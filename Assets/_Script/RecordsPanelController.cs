using UnityEngine;
using TMPro;

public class RecordsPanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text comboText;

    public void ShowRecord(string songId, Difficulty diff, SongRecord rec)
    {
        songNameText.text = songId;
        difficultyText.text = diff.ToString();

        if (rec != null)
        {
            scoreText.text = $"High Score: {rec.highScore}";
            accuracyText.text = $"Best Accuracy: {rec.bestAccuracy:P1}";
            comboText.text = $"Max Combo: {rec.maxCombo}";
        }
        else
        {
            scoreText.text = "High Score: -";
            accuracyText.text = "Best Accuracy: -";
            comboText.text = "Max Combo: -";
        }

        gameObject.SetActive(true);
    }
}
