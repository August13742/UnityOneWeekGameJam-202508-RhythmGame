using UnityEngine;
using System;
using DG.Tweening; // DOTweenの参照（必要に応じて使用）
using UnityEngine.UI; // UIコンポーネントの参照

public class PlayerRay : MonoBehaviour
{
    public event Action ClickDoorListener;    // クリックイベントを通知するデリゲート
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100.0f))
        {
            //Debug.Log(hit.collider.gameObject.name); // ヒットしたオブジェクトの名前をログに出力
            if (Input.GetMouseButtonDown(0)) // 左クリックでアクションを実行
            {
                // ここにクリック時のアクションを追加
                switch (hit.collider.gameObject.tag)
                {
                    case "Door":
                        ClickDoorListener?.Invoke(); // クリックイベントを通知 
                    break;
                }
            
            }
        }
        

    }
}
