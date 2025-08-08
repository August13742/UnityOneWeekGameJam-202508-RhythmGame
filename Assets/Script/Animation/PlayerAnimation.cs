using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using UnityEngine.ParticleSystemJobs; // パーティクルシステムの参照（必要に応じて使用）

public class PlayerAnimation : MonoBehaviour
{
    private Animator _playerAnimator;
    private int animationCounter = 0; // アニメーションのカウンター
    private RigBuilder _rigBuilder;   // RigBuilderの参照（必要に応じて使用）

    [SerializeField] private ParticleSystem _particleSystem; // パーティクルシステムの参照（必要に応じて使用）

    void Start()
    {
        _playerAnimator = this.GetComponent<Animator>();
        _rigBuilder = this.GetComponent<RigBuilder>();
        _rigBuilder.enabled = true; // RigBuilderを無効化
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
        int currentAnimation = animationCounter % 2;

        Debug.Log("Playing animation: " + currentAnimation);

        switch (currentAnimation)
        {
            case 0:
                FirstShotAnimation();
                break;
            case 1:
               SecondShotAnimation();
                break;
        }

        animationCounter++; // 次回用にカウンターを増加
    }

    public void FirstShotAnimation()
    {
        _rigBuilder.enabled = true;
        _playerAnimator.SetTrigger("shot1");
        _particleSystem.transform.position = this.transform.position; // パーティクルシステムの位置をプレイヤーの位置に設定
        _particleSystem.Play(); // パーティクルシステムを再生（必要に応じて使用）

    }

    public void SecondShotAnimation()
    {
        _rigBuilder.enabled = true;
        _playerAnimator.SetTrigger("shot2");
         _particleSystem.Play();
        
    }
}
