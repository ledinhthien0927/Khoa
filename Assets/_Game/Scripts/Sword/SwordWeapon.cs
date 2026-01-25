using UnityEngine;
using System.Collections.Generic;

public class SwordWeapon : MonoBehaviour
{
    [Header("Config")]
    public float damage = 20f;
    public float knockback = 5f;

    [Header("Effects")]
    public TrailRenderer swordTrail; // [MỚI] Kéo cái SwordTrail vào đây

    private Collider _col;
    private List<GameObject> _hitList = new List<GameObject>();

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.enabled = false; // Tắt va chạm

        // Đảm bảo lúc đầu trail không vẽ bậy
        if (swordTrail != null) swordTrail.emitting = false;
    }

    // Hàm này được PlayerController gọi khi bắt đầu vung kiếm
    public void StartAttack()
    {
        _col.enabled = true;
        _hitList.Clear();

        // [MỚI] Bật vệt chém
        if (swordTrail != null) 
        {
            swordTrail.Clear(); // Xóa vệt cũ nếu còn sót
            swordTrail.emitting = true;
        }
    }

    // Hàm này được PlayerController gọi khi kết thúc vung kiếm
    public void StopAttack()
    {
        _col.enabled = false;

        // [MỚI] Tắt vệt chém (nhưng vệt cũ vẫn mờ dần tự nhiên)
        if (swordTrail != null) 
        {
            swordTrail.emitting = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !_hitList.Contains(other.gameObject))
        {
            _hitList.Add(other.gameObject);

            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                DamageInfo info = new DamageInfo
                {
                    amount = damage,
                    attacker = transform.root.gameObject,
                    hitPoint = other.ClosestPoint(transform.position),
                    hitDirection = (other.transform.position - transform.position).normalized,
                    knockbackForce = knockback,
                    type = DamageType.Physical
                };

                target.TakeDamage(info);
            }
        }
    }
}