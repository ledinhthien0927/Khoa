using UnityEngine;

public class Nhan_BossWeapon : MonoBehaviour
{
    [Header("Damage Config")]
    public float damage = 15f;      // Chém thường
    public float heavyDamage = 35f; // Đâm mạnh
    public float knockback = 10f;

    private Collider _col;
    private bool _isHeavy = false;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.enabled = false; // Tắt collider lúc đầu
    }

    // Boss gọi hàm này để bật kiếm
    public void EnableHitbox(bool isHeavy)
    {
        _isHeavy = isHeavy;
        if (_col) _col.enabled = true;
    }

    // Boss gọi hàm này để tắt kiếm
    public void DisableHitbox()
    {
        if (_col) _col.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // Chỉ chém vào Player
        if (other.CompareTag("Player"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                // Tính toán damage
                float finalDmg = _isHeavy ? heavyDamage : damage;
                float finalForce = _isHeavy ? knockback * 2f : knockback;

                DamageInfo info = new DamageInfo
                {
                    amount = finalDmg,
                    attacker = transform.root.gameObject, // Lấy object Boss cha
                    hitPoint = other.ClosestPoint(transform.position),
                    hitDirection = (other.transform.position - transform.position).normalized,
                    knockbackForce = finalForce,
                    type = DamageType.Physical
                };

                target.TakeDamage(info);

                // Tắt collider ngay để không gây damage nhiều lần trong 1 cú chém
                if (_col) _col.enabled = false;
            }
        }
    }
}