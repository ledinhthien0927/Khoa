using System.Collections;
using UnityEngine;

public class PlayerSmithing : MonoBehaviour
{
    public static PlayerSmithing Instance;

    public Animator animator;
    public float delayTime = 0.4f; // THỜI GIAN CHỜ BÚA CHẠM ĐẤT (Chỉnh số này cho khớp)
[Header("Âm Thanh")]
    public AudioSource audioSource; // Kéo cái Loa vào đây
    public AudioClip hitSound;      // Kéo file tiếng Búa vào đây
    void Awake()
    {
        Instance = this;
    }

    public void SmashAt(Vector3 targetPos, int score)
    {
        // 1. Xoay người về hướng vòng tròn
        Vector3 direction = targetPos - transform.position;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 2. Chạy Animation
        if(animator != null) animator.SetTrigger("Smash");

        // 3. THAY VÌ DÙNG EVENT, TA DÙNG COROUTINE ĐỂ TỰ ĐẾM GIỜ
        StartCoroutine(WaitAndHit(targetPos, score));
    }

    IEnumerator WaitAndHit(Vector3 pos, int score)
    {
        // Chờ khoảng 0.4 giây (hoặc số bạn chỉnh) để búa kịp giơ lên đập xuống
        yield return new WaitForSeconds(delayTime);
if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        // SAU KHI CHỜ XONG -> BÁO CÁO CHO QUẢN LÝ
        if (SmithingManager.Instance != null)
        {
            // Hàm này sẽ sinh hiệu ứng nổ + Cộng điểm + Kiểm tra Win
            SmithingManager.Instance.SpawnHammerEffect(pos, score);
        }
    }

    // Xóa hoặc bỏ qua hàm OnHammerHit cũ, không cần dùng nữa
}