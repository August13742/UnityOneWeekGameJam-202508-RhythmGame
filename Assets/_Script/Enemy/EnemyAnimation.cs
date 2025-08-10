using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private Animator animator; // Animatorコンポーネントの参照
    void Start()
    {
        animator = this.GetComponent<Animator>(); // Animatorコンポーネントを取得
        

    }


    public void PlayDieAnimation()
    {
        // 死亡アニメーションを再生する
        animator.SetTrigger("DieTrigger");

    }

    public void PlaySpawnAnimation()
    {
        // スポーンアニメーションを再生する
        animator.SetTrigger("SpawnTrigger");
    }
}
