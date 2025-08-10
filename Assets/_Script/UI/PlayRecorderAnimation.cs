using Rhythm.UI;
using UnityEngine;

public class PlayRecorderAnimation : MonoBehaviour
{
    private Animator animator;
    private void Start()
    {
        animator = GetComponent<Animator>();
        
    }
    private void Update()
    {
        if (AudioManager.Instance.IsMusicPlaying)
        {
            animator.SetBool("Play", true);
            Destroy(this);
        }
    }
}
