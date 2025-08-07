using UnityEngine;
using Rhythm.GamePlay.OSU.Aimless;

namespace Rhythm.GamePlay.Player
{
    public class PlayerController : MonoBehaviour
    {

        [SerializeField] private GameObject lookAtTargetObject;

        // Add a smoothing factor to control the tween speed
        [SerializeField] private float lookAtSmoothing = 15f;
        [SerializeField] private Animator animator;
        private RhythmManagerOSUAimless rhythmManager;

        private void Start()
        {
            rhythmManager = FindFirstObjectByType<RhythmManagerOSUAimless>();
            animator = GetComponentInChildren<Animator>();
            if (lookAtTargetObject == null)
            {
                lookAtTargetObject = new GameObject("LookAtTarget_Generated");
                // Parent it to the scene root to avoid position compounding issues.
                //lookAtTargetObject.transform.SetParent(null);
            }
            rhythmManager.ShotFired += OnShotFired;
        }

        void OnShotFired()
        {
            animator.SetTrigger("Shoot");
        }
        private void Update()
        {
            if (rhythmManager != null)
            {
                // Get the definitive 3D target position
                Vector3 targetPosition = rhythmManager.GetCurrentTargetPosition();

                lookAtTargetObject.transform.position = Vector3.Lerp(
                    lookAtTargetObject.transform.position,
                    targetPosition,
                    Time.deltaTime * lookAtSmoothing
                );
                Vector3 yAxisRotation = new (lookAtTargetObject.transform.position.x,this.transform.position.y , lookAtTargetObject.transform.position.z);
                this.transform.LookAt(yAxisRotation);

            }
        }
    }
}
