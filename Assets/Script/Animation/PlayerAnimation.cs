using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAnimation : MonoBehaviour
{
    private Animator playerAnimator;
    private int animationCounter = 0; // アニメーションのカウンター
    void Start()
    {
        playerAnimator = this.GetComponent<Animator>();


    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CycleAnimation();
        }
    }

    public void CycleAnimation()
    {
        // 現在のアニメーション番号を取得（カウンターを変更せずに計算）
        int currentAnimation = animationCounter % 3;
        
        Debug.Log("Playing animation: " + currentAnimation);
        
        switch (currentAnimation)
        {
            case 0:
                playerAnimator.SetTrigger("Shot1");
                break;
            case 1:
                playerAnimator.SetTrigger("Shot2");
                break;
            case 2:
                playerAnimator.SetTrigger("Shot3");
                break;
        }
        
        animationCounter++; // 次回用にカウンターを増加
    }

    public void TestAnimation()
    {
        playerAnimator.SetTrigger("Shot");
    }

    public void ShotAnimationChange()
    {
        
    }
}
