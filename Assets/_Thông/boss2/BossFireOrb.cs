using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class BossFireOrb : MonoBehaviour
{
    private Transform _bossCenter;
    private float _maxRadius;
    private float _damage;
    private float _duration;
    private float _targetSpeed; // Tốc độ mong muốn để duy trì

    private Rigidbody _rb;

    public void Setup(Transform boss, float speed, float radius, float dmg, float duration)
    {
        _bossCenter = boss;
        _maxRadius = radius;
        _damage = dmg;
        _duration = duration;
        _targetSpeed = speed;

        _rb = GetComponent<Rigidbody>();

        // [QUAN TRỌNG] Cài đặt Rigidbody để khóa trục Y và xoay
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Bắn quả cầu đi theo hướng ngẫu nhiên (chỉ trục X, Z)
        Vector2 rndDir = Random.insideUnitCircle.normalized;
        Vector3 initialVel = new Vector3(rndDir.x, 0, rndDir.y) * _targetSpeed;
        _rb.velocity = initialVel;

        Destroy(gameObject, _duration);
    }

    void FixedUpdate()
    {
        if (_bossCenter == null) 
        {
            Destroy(gameObject);
            return;
        }

        // 1. Duy trì tốc độ không đổi (Để không bị chậm lại sau khi va chạm)
        // Nếu tốc độ hiện tại khác tốc độ mục tiêu, ta ép nó về tốc độ mục tiêu
        if (_rb.velocity.magnitude != _targetSpeed)
        {
            _rb.velocity = _rb.velocity.normalized * _targetSpeed;
        }

        // 2. Logic biên giới hạn (Tường ảo hình tròn)
        Vector3 offset = transform.position - _bossCenter.position;
        offset.y = 0; // Chỉ tính khoảng cách ngang

        if (offset.magnitude > _maxRadius)
        {
            // Tính hướng từ tâm ra quả cầu
            Vector3 normal = offset.normalized;

            // Nếu đang bay ra ngoài (Velocity cùng hướng với Normal) thì mới phản xạ
            if (Vector3.Dot(_rb.velocity, normal) > 0)
            {
                // Phản xạ vận tốc vật lý
                Vector3 reflectVel = Vector3.Reflect(_rb.velocity, -normal);
                _rb.velocity = reflectVel;
            }

            // Đẩy nhẹ vào trong để không bị kẹt
            Vector3 clampedPos = _bossCenter.position + normal * (_maxRadius - 0.2f);
            clampedPos.y = transform.position.y; // Giữ nguyên độ cao Y
            _rb.MovePosition(clampedPos);
        }
    }

    // Xử lý va chạm
    private void OnCollisionEnter(Collision collision)
    {
        // Va chạm với Player
        if (collision.gameObject.CompareTag("Player"))
        {
            var playerCombat = collision.gameObject.GetComponent<IDamageable>();
            if (playerCombat != null)
            {
                playerCombat.TakeDamage(new DamageInfo { 
                    amount = _damage, 
                    attacker = gameObject, 
                    hitPoint = collision.contacts[0].point, 
                    type = DamageType.Magic, 
                    knockbackForce = 8f 
                });
            }
        }
        
        // Lưu ý: Va chạm giữa "Orb" với "Orb" sẽ tự động được Rigidbody xử lý nảy ra
    }
}