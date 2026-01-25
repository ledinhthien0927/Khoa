using UnityEngine;

// Script này gắn vào Object Enemy (cần có Collider và Rigidbody/CharacterController)
public class PhatTestEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float _currentHealth;

    [Header("VFX")]
    public GameObject hitVFX;    // Hiệu ứng khi bị đánh trúng (máu me)
    public GameObject deathVFX;  // Hiệu ứng khi chết

    void Start()
    {
        _currentHealth = maxHealth;
    }

    // --- TRIỂN KHAI GIAO DIỆN IDamageable ---
   public HitResult TakeDamage(DamageInfo info)
    {
        if (_currentHealth <= 0) return HitResult.Ignored;

        // 1. Trừ máu
        _currentHealth -= info.amount;

        // [FIX LỖI NULL] Kiểm tra kỹ trước khi lấy tên người đánh
        string attackerName = (info.attacker != null) ? info.attacker.name : "Môi trường/Mũi tên lạ";
        
        Debug.Log($"Enemy bị đánh bởi {attackerName} - Mất {info.amount} HP - Còn {_currentHealth}");

        // 2. Hiệu ứng trúng đòn (Hit Reaction)
        if (hitVFX != null)
        {
            // Kiểm tra hướng đánh để tránh lỗi xoay (Quaternion Error)
            if (info.hitDirection != Vector3.zero)
            {
                Instantiate(hitVFX, info.hitPoint, Quaternion.LookRotation(info.hitDirection));
            }
            else
            {
                Instantiate(hitVFX, info.hitPoint, Quaternion.identity);
            }
        }

        // 3. Xử lý Knockback (Đẩy lùi)
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(info.hitDirection * info.knockbackForce, ForceMode.Impulse);
        }

        // 4. Kiểm tra chết
        if (_currentHealth <= 0)
        {
            Die();
        }

        return HitResult.Hit;
    }

    void Die()
    {
        Debug.Log("Enemy đã chết!");
        if (deathVFX != null) Instantiate(deathVFX, transform.position, Quaternion.identity);
        
        // Tạm thời destroy, sau này có thể dùng Object Pooling hoặc Animation chết
        Destroy(gameObject);
    }
}