using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;      // Tốc độ bay
    public float lifeTime = 3f;    // Thời gian tự hủy (tránh bay vô tận)

    void Start()
    {
        // Tự hủy sau 3 giây nếu không trúng gì để đỡ lag game
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Bay thẳng về phía trước (trục Z của viên đạn)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    // Xử lý va chạm
    void OnTriggerEnter(Collider other)
    {
        // 1. Nếu trúng Player
        if (other.CompareTag("Player"))
        {
            Debug.Log($"<color=red>ĐẠN TRÚNG PLAYER! (Bùm)</color>");
            // Sau này sẽ gọi hàm trừ máu ở đây: player.TakeDamage()...
            
            Destroy(gameObject); // Viên đạn biến mất
        }
        // 2. Nếu trúng Tường/Đất (Layer Default hoặc Wall)
        // Lưu ý: Phải loại trừ chính thằng Enemy bắn ra (tag Enemy) và đạn khác (tag Projectile)
        else if (!other.CompareTag("Enemy") && !other.CompareTag("Projectile"))
        {
            Debug.Log("Đạn trúng tường.");
            Destroy(gameObject);
        }
    }
}