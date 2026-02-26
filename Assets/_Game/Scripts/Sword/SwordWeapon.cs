using UnityEngine;
using System.Collections.Generic;

public class SwordWeapon : MonoBehaviour
{
    [Header("Config")]
    public float damage = 20f;
    public float knockback = 5f;

    [Header("Attached VFX - Kéo 3 cái VFX con vào đây")]
    // [THAY ĐỔI] Đây là list các object đang gắn trên người, không phải Prefab
    public List<ParticleSystem> slashVfxObjects; 

    [Header("Hit VFX - Hiệu ứng nổ khi trúng (Vẫn dùng Prefab)")]
    public GameObject hitVfxPrefab; 

    private Collider _col;
    private List<GameObject> _hitList = new List<GameObject>();

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.enabled = false; 

        // Tắt hết các VFX lúc đầu game cho chắc ăn
        if (slashVfxObjects != null)
        {
            foreach (var vfx in slashVfxObjects)
            {
                if (vfx != null) vfx.gameObject.SetActive(false);
            }
        }
    }

    // Hàm gọi khi chém (Nhận vào step 1, 2, 3)
    public void StartAttack(int comboStep)
    {
        _col.enabled = true;
        _hitList.Clear();

        // 1. Tính toán index (Combo 1 là index 0)
        int index = comboStep - 1;

        // 2. Kích hoạt VFX có sẵn trên người
        if (slashVfxObjects != null && index >= 0 && index < slashVfxObjects.Count)
        {
            ParticleSystem vfx = slashVfxObjects[index];
            
            if (vfx != null)
            {
                // Bật GameObject lên
                vfx.gameObject.SetActive(true);
                
                // Reset và Chạy lại từ đầu (Quan trọng để nó chém cái mới)
                vfx.Stop(); 
                vfx.Play();
            }
        }
    }

    public void StopAttack()
    {
        _col.enabled = false;
        // Không cần tắt VFX ở đây, cứ để nó chạy hết vòng đời (Lifetime) rồi tự tắt
        // Hoặc nếu muốn tắt ngay lập tức thì gọi vfx.Stop()
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !_hitList.Contains(other.gameObject))
        {
            _hitList.Add(other.gameObject);

            // Hiệu ứng nổ trúng đích (Vẫn cần Instantiate vì nó nằm ở vị trí va chạm)
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