using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float lifeTime = 10f; // Tự hủy sau 10s nếu không trúng gì
    public float damage = 15f;   // Sát thương (sau này dùng)

    private Rigidbody _rb;
    private bool _hasHit = false;
    private Collider _myCollider;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _myCollider = GetComponent<Collider>();
    }

    void Start()
    {
        // Tự hủy sau thời gian lifeTime để đỡ nặng game
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Nếu chưa va chạm, xoay mũi tên theo hướng bay (tạo độ cong vật lý)
        if (!_hasHit && _rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Nếu đã chạm rồi hoặc chạm vào Player thì bỏ qua
        if (_hasHit || collision.gameObject.CompareTag("Player")) return;

        _hasHit = true;

        // 1. Dính vào vật thể (Cắm phập vào)
        StickToTarget(collision);

        // 2. Gây sát thương (Nếu vật đó là quái)
        // Ví dụ: if(collision.gameObject.CompareTag("Enemy")) { ... }
    }

    void StickToTarget(Collision collision)
    {
        // Tắt vật lý để mũi tên đứng im
        _rb.isKinematic = true; 
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Tắt Collider để không cản đường đi của Player
        _myCollider.enabled = false;

        // Gắn mũi tên làm con của vật thể (để nếu vật đó di chuyển, mũi tên đi theo)
        transform.SetParent(collision.transform);

        // Hủy mũi tên sau 5s kể từ khi cắm vào (để dọn dẹp scene)
        Destroy(gameObject, 5f);
    }
}