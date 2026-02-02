using UnityEngine;
using System.Collections;

public class BoatRepairStation : MonoBehaviour
{
    [Header("Requirements - Yêu cầu")]
    public int requiredWood = 3; 
    public int requiredMetal = 2;

    [Header("Boat Objects - Biến hình")]
    public GameObject brokenBoat; // Thuyền cũ
    public GameObject fixedBoat;  // Thuyền mới

    [Header("Settings")]
    public float repairTime = 4.0f;  // Tổng thời gian sửa
    public Transform repairPosition; // Vị trí đứng sửa
    public GameObject repairEffect;  // Hiệu ứng "Biến hình" (Khói/Bụi/Nổ)
    
    // [MỚI] Hiệu ứng trong lúc đang sửa (Tùy chọn: Tiếng gõ búa, bụi nhỏ bay lên)
    public GameObject workingEffect; 

    private bool isPlayerNearby;
    private PlayerController playerRef;
    private bool isRepaired = false;

    void Start()
    {
        // Đảm bảo trạng thái ban đầu
        if (brokenBoat) brokenBoat.SetActive(true);
        if (fixedBoat) fixedBoat.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerRef = other.GetComponent<PlayerController>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerRef = null;
        }
    }

    void Update()
    {
        // Điều kiện kích hoạt: Gần + Chưa sửa + Nhấn F
        if (isPlayerNearby && !isRepaired && Input.GetKeyDown(KeyCode.F))
        {
            // Kiểm tra đúng nhiệm vụ chưa
            if (QuestManager.Instance.princeState != PrinceQuestState.ShipQuest && 
                QuestManager.Instance.princeState != PrinceQuestState.ShipDoing)
            {
                Debug.Log("Chưa nhận nhiệm vụ sửa thuyền!");
                return;
            }

            TryRepair();
        }
    }

    void TryRepair()
    {
        if (playerRef == null) return;

        // Kiểm tra nguyên liệu trong túi
        if (playerRef.model.woodCount >= requiredWood && playerRef.model.metalCount >= requiredMetal)
        {
            StartCoroutine(ProcessRepair());
        }
        else
        {
            Debug.Log("Thiếu nguyên liệu! Cần đi nhặt thêm.");
        }
    }

    IEnumerator ProcessRepair()
    {
        isRepaired = true;

        // 1. SETUP: Khóa nhân vật & Đưa vào vị trí
        playerRef.model.currentVelocity = Vector3.zero;
        if (repairPosition)
        {
            playerRef.transform.position = repairPosition.position;
            playerRef.transform.rotation = repairPosition.rotation;
        }
        
        // 2. ANIMATION: Bắt đầu gõ búa
        playerRef.GetView().TriggerRepair();

        // (Tùy chọn) Hiệu ứng bụi bay lất phất khi đang gõ
        GameObject workVFX = null;
        if (workingEffect) workVFX = Instantiate(workingEffect, transform.position, Quaternion.identity);

        // 3. TÍNH TOÁN THỜI GIAN
        // Thời điểm tráo thuyền = 3/4 tổng thời gian
        float swapTime = repairTime * 0.75f; 
        float remainingTime = repairTime - swapTime;

        // Chờ đến thời điểm 3/4 (Giai đoạn gõ búa tích cực)
        yield return new WaitForSeconds(swapTime);

        // 4. BIẾN HÌNH: Đổi thuyền & Bùm hiệu ứng
        // Tạo hiệu ứng lớn ngay lúc đổi để che mắt người chơi
        if (repairEffect) Instantiate(repairEffect, transform.position, Quaternion.identity);

        // Tráo đổi Model
        if (brokenBoat) brokenBoat.SetActive(false);
        if (fixedBoat) fixedBoat.SetActive(true);

        // 5. KẾT THÚC: Chờ nốt 1/4 thời gian còn lại (để animation kết thúc mượt mà)
        yield return new WaitForSeconds(remainingTime);

        // Xóa hiệu ứng bụi (nếu có)
        if (workVFX) Destroy(workVFX);

        // 6. CẬP NHẬT NHIỆM VỤ
        if (QuestManager.Instance) QuestManager.Instance.SetPrince(PrinceQuestState.ShipDone);
        if (QuestUIManager.Instance) QuestUIManager.Instance.AutoClickMainQuest();

        Debug.Log("Sửa thuyền thành công!");
        
        // Tắt script này
        this.enabled = false;
    }
}