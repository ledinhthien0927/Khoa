using UnityEngine;
using System.Collections;

public class AoEZone : MonoBehaviour
{
    [Header("Settings")]
    public float WarningTime = 1.5f;  // Thời gian hiện vòng đỏ trước khi nổ
    public float Damage = 20f;        // Sát thương
    public float Radius = 1.5f;       // Bán kính vùng nổ
    public float Knockback = 2f;      // Lực đẩy
    
    [Header("Visuals")]
    public GameObject WarningCircle;  // Kéo cái hình tròn đỏ vào đây
    public GameObject ExplosionVFX;   // Kéo hiệu ứng lửa vào đây (Particle System)

    // Biến nội bộ để biết ai là người tung chiêu (tránh tự đánh phe mình nếu cần)
    private GameObject _attacker; 

    public void Setup(GameObject attacker, float damage)
    {
        _attacker = attacker;
        Damage = damage;
    }

    IEnumerator Start()
    {
        // GIAI ĐOẠN 1: CẢNH BÁO (VÒNG ĐỎ)
        if (WarningCircle != null) WarningCircle.SetActive(true);
        if (ExplosionVFX != null) ExplosionVFX.SetActive(false);

        // Đợi người chơi nhìn thấy để né
        yield return new WaitForSeconds(WarningTime);

        // GIAI ĐOẠN 2: BÙNG NỔ (GÂY DMG)
        if (WarningCircle != null) WarningCircle.SetActive(false); // Tắt vòng đỏ
        if (ExplosionVFX != null) ExplosionVFX.SetActive(true);    // Bật lửa lên

        CheckDamage(); // Gây sát thương ngay lập tức

        // Đợi hiệu ứng lửa diễn hết rồi xóa object
        yield return new WaitForSeconds(2.0f); 
        Destroy(gameObject);
    }

    void CheckDamage()
    {
        // Tìm tất cả Collider trong vùng nổ
        Collider[] hits = Physics.OverlapSphere(transform.position, Radius);

        foreach (Collider hit in hits)
        {
            // Chỉ gây damge cho Player (Có Interface IDamageable)
            if (hit.CompareTag("Player"))
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                {
                    DamageInfo info = new DamageInfo
                    {
                        amount = Damage,
                        attacker = _attacker,
                        hitPoint = transform.position,
                        type = DamageType.Magic, // Loại Magic (Lửa)
                        knockbackForce = Knockback
                    };
                    target.TakeDamage(info);
                }
            }
        }
    }

    // Vẽ vòng tròn trong Scene để dễ căn chỉnh
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}