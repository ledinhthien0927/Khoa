using UnityEngine;
using System.Collections.Generic;

// Dòng này giúp Unity tự động thêm Collider nếu bạn quên
[RequireComponent(typeof(Collider))] 
public class SwordWeapon : MonoBehaviour
{
    [Header("Config")]
    public float damage = 20f;
    public float knockback = 5f;

    [Header("Hit VFX - Hiệu ứng nổ khi trúng")]
    public GameObject hitVfxPrefab; 

    private Collider _col;
    private List<GameObject> _hitList = new List<GameObject>();

    void Awake()
    {
        _col = GetComponent<Collider>();
        
        // Kiểm tra an toàn
        if (_col != null)
        {
            _col.isTrigger = true; // Đảm bảo nó là dạng Trigger để không đẩy lùi nhân vật
            _col.enabled = false; 
        }
        else
        {
            Debug.LogError("⚠️ Cây kiếm chưa được gắn BoxCollider!");
        }
    }

    // Hàm gọi khi chém (Nhận vào step 1, 2, 3)
    public void StartAttack(int comboStep)
    {
        if (_col != null) _col.enabled = true;
        _hitList.Clear();
        
        // Đã xóa phần VFX lặp ở đây vì PlayerController đã đảm nhận
    }

    public void StopAttack()
    {
        // Thêm kiểm tra an toàn: Nếu có _col thì mới tắt, tránh lỗi NullReferenceException
        if (_col != null) _col.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !_hitList.Contains(other.gameObject))
        {
            _hitList.Add(other.gameObject);

            // Hiệu ứng nổ trúng đích
            if (hitVfxPrefab != null)
            {
                Vector3 hitPos = other.ClosestPoint(transform.position);
                Instantiate(hitVfxPrefab, hitPos, Quaternion.identity);
            }

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