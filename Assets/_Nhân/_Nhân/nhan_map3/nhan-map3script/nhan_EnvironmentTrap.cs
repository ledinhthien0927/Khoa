using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class nhan_EnvironmentTrap : MonoBehaviour
{
    [Header("Thông số Bẫy")]
    public float nhan_damage = 50f; 
    public float nhan_knockback = 10f;
    
    [Header("Loại Bẫy")]
    public bool nhan_isPushTrap = false; // True: Đẩy ngã (Cột), False: Rơi tự do (Đá)
    public Vector3 nhan_pushDirection = new Vector3(1, 0, 0);
    public float nhan_pushForce = 500f;

    private Rigidbody nhan_rb;
    private bool nhan_hasActivated = false;

    void Awake()
    {
        nhan_rb = GetComponent<Rigidbody>();
        nhan_rb.isKinematic = true; // Treo lơ lửng/Đứng yên lúc đầu
    }

    // Hàm này được gọi bởi nhan_WeakPoint
    public void nhan_ActivateTrap()
    {
        if (nhan_hasActivated) return;
        nhan_hasActivated = true;

        nhan_rb.isKinematic = false; // Bắt đầu rơi theo vật lý

        if (nhan_isPushTrap)
        {
            nhan_rb.AddForce(nhan_pushDirection.normalized * nhan_pushForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Chỉ gây damage khi bẫy đã được kích hoạt và đang di chuyển mạnh
        if (nhan_hasActivated && nhan_rb.linearVelocity.magnitude > 1f)
        {
            // Kiểm tra va chạm với Boss (hoặc bất kỳ ai có IDamageable)
            IDamageable target = collision.gameObject.GetComponent<IDamageable>();
            
            if (target != null)
            {
                // Tận dụng struct DamageInfo có sẵn trong code của bạn
                DamageInfo info = new DamageInfo
                {
                    amount = nhan_damage,
                    attacker = this.gameObject, // Kẻ tấn công là cái bẫy
                    hitPoint = collision.contacts[0].point,
                    hitDirection = nhan_rb.linearVelocity.normalized,
                    knockbackForce = nhan_knockback,
                    type = DamageType.Physical // Giả sử enum này có sẵn
                };

                target.TakeDamage(info);
                Debug.Log($"Bẫy đè trúng {collision.gameObject.name}!");
            }

            // Tự tắt script va chạm sau khi đè trúng để tránh spam damage liên tục
            // Hoặc Destroy(this) nếu muốn bẫy thành vật vô hại sau đó
            this.enabled = false; 
        }
    }
}