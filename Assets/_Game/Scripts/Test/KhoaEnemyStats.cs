using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Cấu hình chỉ số")]
    public float health = 100f;
    public float moveSpeed = 3f;
    public int scoreValue = 10;

    // Hàm nhận sát thương
    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log(gameObject.name + " còn " + health + " máu.");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Bạn có thể thêm hiệu ứng nổ hoặc âm thanh ở đây
        Debug.Log("Enemy đã bị tiêu diệt!");
        Destroy(gameObject);
    }
}