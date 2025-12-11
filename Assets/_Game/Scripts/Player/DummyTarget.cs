using UnityEngine;

// Kế thừa IDamageable để Player nhận diện được đây là "kẻ thù"
public class DummyTarget : MonoBehaviour, IDamageable
{
    public float health = 100f;

    public HitResult TakeDamage(DamageInfo info)
    {
        // 1. Trừ máu
        health -= info.amount;

        // 2. In ra Console để bạn biết là ĐÃ TRÚNG
        Debug.Log($"<color=red>BỊ ĐÁNH!</color> Sát thương: {info.amount} | Loại: {info.type} | Người đánh: {info.attacker.name}");

        // 3. Hiệu ứng rung nhẹ cái bao cát (để nhìn cho sướng mắt)
        StartCoroutine(ShakeEffect());

        // 4. Kiểm tra chết
        if (health <= 0)
        {
            Debug.Log("Bao cát đã bị phá hủy!");
            // health = 100f; // Tự hồi máu để test tiếp nếu muốn
        }

        return HitResult.Hit;
    }

    // Làm bao cát rung rung khi bị đánh
    System.Collections.IEnumerator ShakeEffect()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0.0f;
        while (elapsed < 0.2f)
        {
            float x = Random.Range(-0.1f, 0.1f);
            transform.position = originalPos + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }
}