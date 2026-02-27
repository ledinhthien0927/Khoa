using UnityEngine;

public class Nhan_DarkProjectile : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 20f;      // Tốc độ bay (vừa phải để né được)
    public float damage = 10f;     // Sát thương
    public float lifeTime = 3f;    // Thời gian tồn tại

    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifeTime); // Tự hủy sau 3s nếu bắn trượt
    }

    void Update()
    {
        // Bay thẳng về phía trước (Hướng đã được set lúc Spawn)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Chỉ gây damage cho Player
        if (other.CompareTag("Player"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                DamageInfo info = new DamageInfo
                {
                    amount = damage,
                    attacker = null, // Hoặc gán Boss nếu muốn
                    hitPoint = other.ClosestPoint(transform.position),
                    hitDirection = transform.forward,
                    knockbackForce = 5f,
                    type = DamageType.Physical
                };
                target.TakeDamage(info);
            }
            Destroy(gameObject); // Trúng thì biến mất
        }
        // Va vào tường cũng biến mất
        else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle") || other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            Destroy(gameObject);
        }
    }
}