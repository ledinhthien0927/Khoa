using UnityEngine;

namespace _Khoa.Khoa
{
    public class AutoAdjustHeight : MonoBehaviour
    {
        [Header("Settings")]
        public float offset = 0.5f; // Khoảng cách hở thêm so với đỉnh đầu (0.5 mét)

        void Start()
        {
            // 1. Tìm Collider của Enemy (Object cha)
            // Collider giúp ta biết con quái cao bao nhiêu
            Collider parentCollider = transform.parent.GetComponent<Collider>();

            if (parentCollider != null)
            {
                // 2. Tính toán độ cao
                // bounds.max.y là điểm cao nhất của Collider trong không gian
                // transform.parent.position.y là vị trí chân
                float enemyHeight = parentCollider.bounds.max.y - transform.parent.position.y;

                // 3. Cập nhật vị trí cho thanh máu
                // Chỉ thay đổi độ cao Y, giữ nguyên X và Z
                transform.localPosition = new Vector3(0, enemyHeight + offset, 0);
            }
            else
            {
                // Nếu Enemy không có Collider, dùng giá trị mặc định (ví dụ cao 2m)
                Debug.LogWarning("Enemy không có Collider! Đang dùng độ cao mặc định.");
                transform.localPosition = new Vector3(0, 2f + offset, 0);
            }
        }
    }
}