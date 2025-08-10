using UnityEngine;
using DG.Tweening; // DOTweenの参照（必要に応じて使用）
using UnityEngine.UI; // UIコンポーネントの参照
using System;
using System.Collections;
using System.Collections.Generic;

public class TitleDoor : MonoBehaviour, IClickable
{
    [SerializeField] private PlayerRay playerRay; // PlayerRayの参照（必要に応じて使用）
    [SerializeField] private Camera mainCamera; // メインカメラの参照（必要に応じて使用）
    [SerializeField] private GameObject LeftDoor; // 左ドア
    [SerializeField] private GameObject RightDoor; // 右ドア
    [SerializeField] private Transform leftOrigin;    // 左ドアの回転の基準点
    [SerializeField] private Transform rightOrigin;   // 右ドアの回転の基準点

    [SerializeField] private Canvas uiCanvas;
    private bool RunDoorCorutine = false;
    
    // ドアごとの前回角度を管理する辞書
    private Dictionary<GameObject, float> doorPrevValues = new Dictionary<GameObject, float>();
    void Start()
    {
        playerRay.ClickDoorListener += () => OnClick(); // クリックイベントを登録
    }

    // IClickableインターフェースの実装
    public void OnClick()
    {
        if (RunDoorCorutine) return; // 既に実行中なら何もしない
        RunDoorCorutine = true; // 実行中フラグを立てる
        playerRay.ClickDoorListener -= () => OnClick();  // クリックイベントの登録を解除
        Debug.Log("TitleDoor clicked!"); // クリック時のアクションをここに追加
        uiCanvas.gameObject.SetActive(false);

        StartCoroutine(OnClickCoroutine());
    }

    private IEnumerator OnClickCoroutine()
    {
        playerRay.ClickDoorListener -= () => OnClick();  // クリックイベントの登録を解除
        Debug.Log("TitleDoor clicked!"); // クリック時のアクションをここに追加

        // 左ドアと右ドアを同時に回転アニメーション開始
        Tween leftDoorTween = DoRotateAroundDoor(60f, 1f, LeftDoor, leftOrigin, 1f);
        Tween rightDoorTween = DoRotateAroundDoor(60f, 1f, RightDoor, rightOrigin, -1f);

        // 待機（
        yield return new WaitForSeconds(0.5f);

        // カメラの位置を変更
        mainCamera.transform.DOLocalMove(new Vector3(0, 0, 30), 2.0f);

        // 待機
        yield return new WaitForSeconds(0.5f);

        CrossfadeManager.Instance.FadeToBlack();
        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("CalibrationScene");
        
    }

    /// <summary>
    /// ドアを指定した角度だけ回転させる統一メソッド
    /// </summary>
    /// <param name="endValue">回転角度</param>
    /// <param name="duration">アニメーション時間</param>
    /// <param name="door">回転させるドアのGameObject</param>
    /// <param name="origin">回転軸のTransform</param>
    /// <param name="direction">回転方向（1または-1）</param>
    public Tween DoRotateAroundDoor(float endValue, float duration, GameObject door, Transform origin, float direction)
    {
        doorPrevValues[door] = 0.0f; // 初期化
        return DOTween.To(x => RotateAroundDoor(x, door, origin), 0.0f, direction * endValue, duration);
    }

    /// <summary>
    /// ドアの回転処理（統一版）
    /// </summary>
    /// <param name="val">現在の角度値</param>
    /// <param name="door">回転させるドア</param>
    /// <param name="origin">回転軸</param>
    private void RotateAroundDoor(float val, GameObject door, Transform origin)
    {
        float prevVal = doorPrevValues.ContainsKey(door) ? doorPrevValues[door] : 0.0f;
        float delta = val - prevVal;
        door.transform.RotateAround(origin.position, Vector3.up, delta);
        doorPrevValues[door] = val;
    }


    
    
}
