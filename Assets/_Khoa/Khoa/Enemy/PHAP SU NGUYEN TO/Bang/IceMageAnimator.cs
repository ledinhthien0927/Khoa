using UnityEngine;

public class IceMageAnimator : MonoBehaviour
{
    private Animator animator;

    // Animation property hashes
    private readonly int isWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int throwPotionHash = Animator.StringToHash("ThrowPotion");
    private readonly int dieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("IceMageAnimator: No Animator component found on " + gameObject.name);
        }
    }


    /// Set the walking state.
    /// ten = isWalking -> True if walking, false for idle
    public void SetWalking(bool isWalking)
    {
        if (animator != null) animator.SetBool(isWalkingHash, isWalking);
    }


    /// Force back to Idle state quickly by turning off isWalking.
    public void PlayIdle()
    {
        SetWalking(false);
    }


    /// Play the hit (trúng đòn) animation.
    public void PlayHit()
    {
        if (animator != null) animator.SetTrigger(hitHash);
    }


    /// Play the throw potion (ném bình) animation.
    public void PlayThrowPotion()
    {
        if (animator != null) animator.SetTrigger(throwPotionHash);
    }

    /// Play the death animation.
    public void PlayDie()
    {
        if (animator != null) animator.SetTrigger(dieHash);
    }
}
