using UnityEngine;
using Rhythm.GamePlay.OSU.Aimless;

namespace Rhythm.GamePlay.OSU
{
    public class EnemyAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string deathTrigger = "Death";
        private Vector3 lookAtTarget;
        
        private void Start()
        {
            GameObject playerObj = RhythmManagerOSUAimless.Instance.PlayerObject;
            if (playerObj != null)
            {
                lookAtTarget = transform.position * 2 - playerObj.transform.position;
            }
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            PlaySpawnAnimation();
        }

        void Update()
        {
            if (lookAtTarget != Vector3.zero)
            {
                transform.LookAt(lookAtTarget);
            }
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
                //Debug.Log("DEATHアニメーション再生");
            }
        }
    }
}
