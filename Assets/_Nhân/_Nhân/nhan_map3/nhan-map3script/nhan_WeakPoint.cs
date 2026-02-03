using UnityEngine;

public class nhan_WeakPoint : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Tag của mũi tên (Hãy đặt Tag cho Prefab Mũi tên là 'Arrow')")]
    public string nhan_arrowTag = "Arrow"; 
    
    [Tooltip("Kéo object Bẫy (Đá/Cột) vào đây")]
    public nhan_EnvironmentTrap nhan_trapObject;

    [Header("Hiệu ứng")]
    public GameObject nhan_breakVFX; // Hiệu ứng vỡ chốt (nếu có)

    // Dùng OnCollisionEnter để bắt va chạm vật lý với ArrowProjectile
    private void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra xem vật va chạm có phải là Mũi tên không
        // (Hoặc check component ArrowProjectile nếu bạn lười đặt Tag)
        if (collision.gameObject.CompareTag(nhan_arrowTag) || collision.gameObject.GetComponent<ArrowProjectile>())
        {
            BreakLink();
        }
    }

    void BreakLink()
    {
        // 1. Kích hoạt bẫy rơi
        if (nhan_trapObject != null)
        {
            nhan_trapObject.nhan_ActivateTrap();
        }

        // 2. Tạo hiệu ứng vỡ
        if (nhan_breakVFX != null)
        {
            Instantiate(nhan_breakVFX, transform.position, transform.rotation);
        }

        // 3. Hủy chốt này
        // Lưu ý: Vì mũi tên đã Stick() vào chốt, khi hủy chốt mũi tên cũng mất theo (trông như bị gãy cùng).
        Destroy(gameObject);
    }
}