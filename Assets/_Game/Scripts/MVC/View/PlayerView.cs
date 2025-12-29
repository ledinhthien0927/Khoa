using UnityEngine;
using System; // Bắt buộc để dùng Action

public class PlayerView : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // Sự kiện gửi tín hiệu kèm Mật khẩu (Int) sang Controller
    public Action<int> OnAttackImpact; 

    // Cập nhật Blend Tree Di chuyển
    public void UpdateMovementAnimation(float horizontal, float vertical)
    {
        if (animator == null) return;
        animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", vertical, 0.1f, Time.deltaTime);
    }

    // Trigger Tấn công
    public void TriggerAttack()
    {
        if (animator) 
        {
            animator.ResetTrigger("Attack"); // Reset để tránh kẹt
            animator.SetTrigger("Attack");
        }
    }

    // Bool Đỡ đòn
    public void SetBlocking(bool isBlocking)
    {
        if (animator) animator.SetBool("IsBlocking", isBlocking);
    }

    // Trigger Nhảy/Lướt
    public void TriggerDash()
    {
        if (animator) animator.SetTrigger("Dash");
    }

    // --- HÀM ANIMATION EVENT (QUAN TRỌNG) ---
    // Gắn hàm này vào Animation Đòn 3, điền Int = 1
    public void AE_TriggerImpact(int type)
    {
        OnAttackImpact?.Invoke(type);
    }
}