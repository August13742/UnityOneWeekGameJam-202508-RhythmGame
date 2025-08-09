using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UIコンポーネントの参照
using DG.Tweening; // DOTweenの参照（必要に応じて使用）

public class MusicSelect : MonoBehaviour
{
    [SerializeField] private float upMaxPosY;   // 上に移動できる最大Y座標
    [SerializeField] private float downMaxPosY; // 下に移動できる最大Y座標
    [SerializeField] private float moveSpeed; // 移動速度

    RectTransform _rectTransform;

    void Start()
    {
        _rectTransform = this.GetComponent<RectTransform>();  // RectTransformの参照を取得
        
        // 初期位置と制限値をデバッグ出力
        Debug.Log("Initial anchoredPosition: " + _rectTransform.anchoredPosition);
        Debug.Log("upMaxPosY: " + upMaxPosY + ", downMaxPosY: " + downMaxPosY);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.DownArrow)) // GetKeyDownに変更（1回だけ実行）
        {
            Vector2 newPosition = _rectTransform.anchoredPosition + new Vector2(0, +moveSpeed); // anchoredPositionを使用
            newPosition.y = Mathf.Clamp(newPosition.y, upMaxPosY, downMaxPosY); // Y座標を制限
            _rectTransform.anchoredPosition = newPosition; // 制限された位置を適用
            
            Debug.Log("Down pressed - New Position: " + newPosition); // デバッグ用のログ出力
        }
        else if (Input.GetKey(KeyCode.UpArrow)) // GetKeyDownに変更（1回だけ実行）
        {
            Vector2 newPosition = _rectTransform.anchoredPosition + new Vector2(0, -moveSpeed); // anchoredPositionを使用
            newPosition.y = Mathf.Clamp(newPosition.y, upMaxPosY, downMaxPosY); // Y座標を制限
            _rectTransform.anchoredPosition = newPosition; // 制限された位置を適用
            
            Debug.Log("Up pressed - New Position: " + newPosition); // デバッグ用のログ出力
        }
    }

    
    
}
