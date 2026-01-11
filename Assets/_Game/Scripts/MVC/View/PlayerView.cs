using UnityEngine;
using System; 

public class PlayerView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public Action<int> OnAttackImpact; 

    public void UpdateMovementAnimation(float horizontal, float vertical)
    {
        if (animator == null) return;
        animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", vertical, 0.1f, Time.deltaTime);
    }

    public void TriggerAttack()
    {
        if (animator) 
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("CounterAttack"); 
            animator.ResetTrigger("SkillE"); // Reset Skill E
            animator.SetTrigger("Attack");
        }
    }

    public void TriggerCounterAttack()
    {
        if (animator)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("CounterAttack"); 
        }
    }

    // [MỚI] Trigger cho Skill E
    public void TriggerSkillE()
    {
        if (animator)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("SkillE"); // Bạn cần tạo Trigger này trong Animator
        }
    }

    public void SetBlocking(bool isBlocking)
    {
        if (animator) animator.SetBool("IsBlocking", isBlocking);
    }

    public void TriggerDash()
    {
        if (animator) animator.SetTrigger("Dash");
    }

    public void AE_TriggerImpact(int type)
    {
        OnAttackImpact?.Invoke(type);
    }
}