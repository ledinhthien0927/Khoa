using UnityEngine;

public enum ItemType { Wood, Metal }

public class CollectibleItem : MonoBehaviour
{
    public ItemType type;
    public GameObject pickupEffect; // Hiệu ứng nổ/bụi khi nhặt

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // 1. Cộng đồ vào túi
                if (type == ItemType.Wood) player.model.woodCount++;
                else player.model.metalCount++;

                // 2. [QUAN TRỌNG] Cập nhật ngay UI Nhiệm vụ phụ
                if (QuestUIManager.Instance != null)
                {
                    QuestUIManager.Instance.UpdateQuestUI();
                }

                // 3. Hiệu ứng & Xóa vật phẩm
                if (pickupEffect) Instantiate(pickupEffect, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}