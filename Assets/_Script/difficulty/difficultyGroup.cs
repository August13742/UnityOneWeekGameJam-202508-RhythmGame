using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UIコンポーネントの参照
using DG.Tweening; // DOTweenの参照（必要に応じて使用）
using System;

public class difficultyGroup : MonoBehaviour
{
    private Vector3 pos;

    void Start()
    {
        pos = this.transform.position;  //初期位置を保持

    }

    void Update()
    {
        // ここに必要な更新処理を追加
        if (Input.GetKeyDown(KeyCode.Space))
        {
            difficultyGroupMove();
        }
    }


    public void difficultyGroupMove()
    {
        this.gameObject.SetActive(true); // 難易度グループを表示する
        // ここに難易度グループの移動処理を追加
        this.transform.DOMove(pos + new Vector3(-250, 0, 0), 0.4f);
        
    }

    public void difficultyGroupBack()
    {
        this.gameObject.SetActive(false); // 難易度グループを非表示する
         // ここに難易度グループの移動処理を追加
        this.transform.DOMove(pos, 0.4f);

    }
    
}
