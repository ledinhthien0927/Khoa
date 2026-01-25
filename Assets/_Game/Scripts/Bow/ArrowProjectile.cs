using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Stats")]
    public float damage = 15f;
    public float knockback = 3f;
    public float lifeTime = 5f;

    [Header("Effects")]
    public TrailRenderer arrowTrail; // [MỚI] Kéo cái WindTrail vào đây

    private Rigidbody _rb;
    private bool _hasHit = false;

    void Awake() => _rb = GetComponent<Rigidbody>();
    void Start() => Destroy(gameObject, lifeTime);

    void Update()
    {
        // Xoay theo hướng bay
        if (!_hasHit && _rb.linearVelocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        if (collision.gameObject.CompareTag("Player")) return;

        _hasHit = true;
        Stick(collision);

        // Xử lý sát thương (như cũ)
        if (collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable target = collision.gameObject.GetComponent<IDamageable>();
            if (target != null)
            {
                DamageInfo info = new DamageInfo
                {
                    amount = damage,
                    attacker = null, 
                    hitPoint = collision.contacts[0].point,
                    hitDirection = _rb.linearVelocity.normalized,
                    knockbackForce = knockback,
                    type = DamageType.Physical
                };
                target.TakeDamage(info);
            }
        }
    }

    void Stick(Collision col)
    {
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        GetComponent<Collider>().enabled = false;
        
        transform.SetParent(col.transform);
        
        // [MỚI] Tắt vệt gió khi cắm vào mục tiêu
        if (arrowTrail != null)
        {
            arrowTrail.emitting = false;
        }

        Destroy(gameObject, 5f);
    }
}