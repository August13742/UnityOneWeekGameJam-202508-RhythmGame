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
        private GameObject playerObj;
        private void Start()
        {
              
              playerObj = RhythmManagerOSUAimless.Instance.PlayerObject;
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
            transform.LookAt(transform.position * 2 - playerObj.transform.position);
                Debug.Log("Playerを向く!");
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
