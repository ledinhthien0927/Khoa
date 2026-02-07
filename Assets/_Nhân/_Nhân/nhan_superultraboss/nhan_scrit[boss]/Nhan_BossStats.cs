using UnityEngine;

public class Nhan_BossStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float Nhan_maxHealth = 1000f;
    public float Nhan_currentHealth;
    
    [Header("Phase Settings")]
    public float Nhan_phase2Threshold = 0.1f; // 10%
    public bool Nhan_isPhase2 = false;

    private Nhan_ValeriusBT _ai; // Tham chiếu đến script BT
    private Animator _animator;

    void Start()
    {
        Nhan_currentHealth = Nhan_maxHealth;
        _ai = GetComponent<Nhan_ValeriusBT>();
        _animator = GetComponent<Animator>();
    }

    public HitResult TakeDamage(DamageInfo info)
    {
        // 1. Logic Phản Đòn (Noble's Parry)
        if (_ai != null && _ai.Nhan_IsParrying)
        {
            Debug.Log("BOSS PARRIED PLAYER!");
            // Nếu muốn Boss phản công ngay lập tức thì code thêm hàm TriggerCounterAttack bên BT
            return HitResult.Parried;
        }

        // 2. Nhận sát thương
        Nhan_currentHealth -= info.amount;
        Debug.Log($"Boss HP: {Nhan_currentHealth}/{Nhan_maxHealth}");
        
        if (_animator) _animator.SetTrigger("GetHit");

        // 3. Kiểm tra chuyển Phase 2 (Dưới 10% máu)
        if (Nhan_currentHealth <= Nhan_maxHealth * Nhan_phase2Threshold && !Nhan_isPhase2)
        {
            Nhan_isPhase2 = true;
            Debug.Log("=== ENTERING PHASE 2: SOUL SHATTER ===");
            // Tại đây bạn sẽ gọi hàm tách 5 bản thể (sẽ làm ở bước sau)
        }

        if (Nhan_currentHealth <= 0) Die();

        return HitResult.Hit;
    }

    void Die()
    {
        if (_animator) _animator.SetTrigger("Die");
        // Logic rơi đồ, thắng game
        Destroy(gameObject, 5f);
    }
}