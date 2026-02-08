using UnityEngine;

public class Nhan_BossStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public string bossName = "Bá Tước Valerius";
    public float Nhan_maxHealth = 1000f;
    public float Nhan_currentHealth;
    
    [Header("Phase Settings")]
    public float Nhan_phase2Threshold = 0.1f; 
    public bool Nhan_isPhase2 = false;

    private Nhan_ValeriusBT _ai; 
    private Animator _animator;

    void Start()
    {
        Nhan_currentHealth = Nhan_maxHealth;
        _ai = GetComponent<Nhan_ValeriusBT>();
        _animator = GetComponent<Animator>();

        // Kích hoạt thanh máu Boss trên màn hình
        if (Nhan_BossUI.Instance != null)
        {
            Nhan_BossUI.Instance.ShowBoss(bossName, Nhan_maxHealth);
        }
    }

    public HitResult TakeDamage(DamageInfo info)
    {
        // Nếu Boss đang đỡ đòn
        if (_ai != null && _ai.Nhan_IsParrying) return HitResult.Parried;

        // Trừ máu
        Nhan_currentHealth -= info.amount;
        
        // Cập nhật thanh máu trên màn hình
        if (Nhan_BossUI.Instance != null)
        {
            Nhan_BossUI.Instance.UpdateHP(Nhan_currentHealth);
        }

        if (_animator) _animator.SetTrigger("GetHit");

        // Logic chuyển Phase
        if (Nhan_currentHealth <= Nhan_maxHealth * Nhan_phase2Threshold && !Nhan_isPhase2)
        {
            Nhan_isPhase2 = true;
            // Code tách bản thể sẽ viết ở đây
        }

        if (Nhan_currentHealth <= 0) Die();

        return HitResult.Hit;
    }

    void Die()
    {
        if (_animator) _animator.SetTrigger("Die");
        if (Nhan_BossUI.Instance != null) Nhan_BossUI.Instance.HideBoss(); // Ẩn thanh máu
        Destroy(gameObject, 5f);
    }
}