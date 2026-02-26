using UnityEngine;

public enum ItemType { Wood, Metal }

public class CollectibleItem : MonoBehaviour
{
    public ItemType type;
    public GameObject pickupEffect; // Hiệu ứng nổ/bụi khi nhặt
    
    [Header("UI")]
    public GameObject promptUI; // Kéo thả UI (VD: Text "Nhấn F để nhặt" hoặc Canvas) vào đây

    private bool _isPlayerNearby = false;
    private PlayerController _playerRef;

    void Start()
    {
        // Đảm bảo UI ẩn đi khi mới bắt đầu game
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        // Nếu người chơi đứng trong vùng an toàn và nhấn phím F
        if (_isPlayerNearby && _playerRef != null && Input.GetKeyDown(KeyCode.F))
        {
            CollectItem();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerRef = other.GetComponent<PlayerController>();
            if (_playerRef != null)
            {
                _isPlayerNearby = true;
                
                // Hiện UI phím F lên
                if (promptUI != null) promptUI.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = false;
            _playerRef = null;
            
            // Ẩn UI phím F đi khi người chơi rời khỏi
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    private void CollectItem()
    {
        // 1. Cộng đồ vào túi
        if (type == ItemType.Wood) _playerRef.model.woodCount++;
        else _playerRef.model.metalCount++;

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