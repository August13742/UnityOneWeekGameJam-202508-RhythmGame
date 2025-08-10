using UnityEngine;

namespace Rhythm.GamePlay.OSU
{
    public class EnemyAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string deathTrigger = "Death";
        
        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }
        
        private void OnEnable()
        {
            // Play spawn animation when enemy is activated from pool
            PlaySpawnAnimation();
        }
        
        public void PlaySpawnAnimation()
        {
            if (animator != null && !string.IsNullOrEmpty(spawnTrigger))
            {
                animator.SetTrigger(spawnTrigger);
            }
        }
        
        public void PlayDeathAnimation()
        {
            if (animator != null && !string.IsNullOrEmpty(deathTrigger))
            {
                animator.SetTrigger(deathTrigger);
            }
        }
    }
}
