using UnityEngine;
using System;

public class MusicButton : MonoBehaviour
{
    [SerializeField] private difficultyGroup _difficultyGroup; // 音楽の名前
    [SerializeField] private GameObject _musicGroup; // 音楽の名前
    [SerializeField] private GameObject _backButton; // 戻るボタン

    public void OnClick()
    {
        _difficultyGroup.difficultyGroupMove(); // 難易度グループの移動処理を呼び出す
        OnFadeout(); // フェードアウト処理を呼び出す
    }

    public void OnFadeout()
    {
        _musicGroup.SetActive(false); // ボタンを非表示にする
        _backButton.SetActive(true); // 戻るボタンを表示する

    }
    public void OnFadein()
    {
        _musicGroup.SetActive(true); // ボタンを表示にする   
    }
}


