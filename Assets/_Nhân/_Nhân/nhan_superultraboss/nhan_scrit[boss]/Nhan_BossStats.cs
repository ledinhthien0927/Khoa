using UnityEngine;

public class Nhan_BossStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public string bossName = "Huyết Ảnh Valerius";
    public float Nhan_maxHealth = 2000f;
    public float Nhan_currentHealth;
    
    [Header("Phase 2 Settings")]
    public float Nhan_phase2Threshold = 0.4f; 
    public bool Nhan_isPhase2 = false;
    public GameObject Nhan_phase2AuraVFX; 

    private Nhan_ValeriusBT _ai; 
    private Animator _animator;

    void Start()
    {
        Nhan_currentHealth = Nhan_maxHealth;
        _ai = GetComponent<Nhan_ValeriusBT>();
        _animator = GetComponent<Animator>();
        
        if(Nhan_phase2AuraVFX) Nhan_phase2AuraVFX.SetActive(false);

        // Gọi sang BossUI
        if (Nhan_BossUI.Instance != null)
        {
            Nhan_BossUI.Instance.ShowBoss(bossName, Nhan_maxHealth);
        }
    }

    public HitResult TakeDamage(DamageInfo info)
    {
        Nhan_currentHealth -= info.amount;
        
        // Cập nhật BossUI
        if (Nhan_BossUI.Instance != null)
        {
            Nhan_BossUI.Instance.UpdateHP(Nhan_currentHealth);
        }
        
        if (_animator && info.attacker != gameObject) 
            _animator.SetTrigger("GetHit");

        CheckPhase();

        if (Nhan_currentHealth <= 0) Die();
        return HitResult.Hit;
    }

    public void BurnHealth(float amount)
    {
        Nhan_currentHealth -= amount;
        if (Nhan_BossUI.Instance != null)
        {
            Nhan_BossUI.Instance.UpdateHP(Nhan_currentHealth);
        }
        if (Nhan_currentHealth <= 0) Die();
    }

    void CheckPhase()
    {
        if (Nhan_currentHealth <= Nhan_maxHealth * Nhan_phase2Threshold && !Nhan_isPhase2)
        {
            Nhan_isPhase2 = true;
            if(Nhan_phase2AuraVFX) Nhan_phase2AuraVFX.SetActive(true);
            if (_ai) _ai.EnterPhase2();
        }
    }

    void Die()
    {
        if (_animator) _animator.SetTrigger("Die");
        if (Nhan_BossUI.Instance != null) Nhan_BossUI.Instance.HideBoss();
        Destroy(gameObject, 5f);
    }
}