using UnityEngine;

public class BackButton : MonoBehaviour
{
    [SerializeField] private GameObject _musicGroup; // 音楽のグループ
    [SerializeField] private difficultyGroup _difficultyGroup; // 難易度グループ
    void Start()
    {

    }

    public void OnClick()
    {
        _difficultyGroup.difficultyGroupBack(); // 難易度グループを元の位置に戻す
        _musicGroup.SetActive(true); // 音楽のグループを表示する
        this.gameObject.SetActive(false); // このボタンを非表示にする

    }
}
