using UnityEngine;

public class AssassinAnimator : MonoBehaviour
{
    private Animator animator;

    // Animation property hashes
    private readonly int dieHash = Animator.StringToHash("Die");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int isRunningHash = Animator.StringToHash("IsRunning");
    private readonly int slashHash = Animator.StringToHash("Slash");
    private readonly int hitHash = Animator.StringToHash("Hit");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("AssassinAnimator: No Animator component found on " + gameObject.name);
        }
    }

    /// Play the death animation.
    public void PlayDie()
    {
        if (animator != null) animator.SetTrigger(dieHash);
    }


    /// Play the jump animation.
    public void PlayJump()
    {
        if (animator != null) animator.SetTrigger(jumpHash);
    }

    /// Set the running state.

    /// tên = isRunning True if running, false for idle.
    public void SetRunning(bool isRunning)
    {
        if (animator != null) animator.SetBool(isRunningHash, isRunning);
    }


    /// Force back to Idle state quickly by turning off isRunning.
    public void PlayIdle()
    {
        SetRunning(false);
    }

    /// Play the slash (chém) attack animation.
    public void PlaySlash()
    {
        if (animator != null) animator.SetTrigger(slashHash);
    }


    /// Play the hit (trúng đòn) animation.
    public void PlayHit()
    {
        if (animator != null) animator.SetTrigger(hitHash);
    }
}
