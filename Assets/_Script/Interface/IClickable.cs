using UnityEngine;

/// <summary>
/// クリック可能なオブジェクトが実装すべきインターフェース
/// </summary>
public interface IClickable
{
    /// <summary>
    /// オブジェクトがクリックされた時に呼ばれるメソッド
    /// </summary>
    void OnClick();
}