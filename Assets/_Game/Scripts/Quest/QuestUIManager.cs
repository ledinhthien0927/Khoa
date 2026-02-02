using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestUIManager : MonoBehaviour
{
    public static QuestUIManager Instance;

    [Header("UI Panels - Khung Giao Diện")]
    public GameObject questPanel;         // Panel tổng chứa nhiệm vụ
    public Transform contentParent;       // Object 'Content' trong Scroll View (để rebuild layout)

    [Header("Main Quest Elements - Nhiệm vụ Chính")]
    public GameObject mainQuestObj;       // Object cha của dòng Main Quest
    public TextMeshProUGUI mainQuestText; // Text hiển thị nội dung
    public QuestTaskButton mainNavBtn;    // Nút dẫn đường chính

    [Header("Side Quest Elements - Nhiệm vụ Phụ")]
    public GameObject sideQuestObj;       // Object cha của dòng Side Quest
    public TextMeshProUGUI sideQuestText; // Text hiển thị nội dung
    public QuestTaskButton sideNavBtn;    // Nút dẫn đường phụ

    [Header("Targets - Các Mục Tiêu Dẫn Đường")]
    public Transform princeTransform;     // Vị trí NPC Hoàng tử
    public Transform anvilTransform;      // Vị trí Lò rèn (Sửa kiếm)
    public Transform boatTransform;       // Vị trí Chiếc thuyền (Sửa thuyền)
    public Transform fragmentZone;        // Khu vực tìm Mảnh vỡ
    public Transform materialZone;        // Khu vực tìm nguyên liệu (Rừng/Bãi phế liệu) - Tùy chọn

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Cập nhật UI ngay khi vào game
        UpdateQuestUI();
    }

    // Hàm cập nhật toàn bộ giao diện dựa trên trạng thái QuestManager và Player Inventory
    public void UpdateQuestUI()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        // 1. Reset trạng thái: Hiện Main, Ẩn Side (để clean trước khi set)
        mainQuestObj.SetActive(true);
        sideQuestObj.SetActive(false);

        // Lấy thông tin Player để check số lượng nguyên liệu
        PlayerController player = FindFirstObjectByType<PlayerController>();
        int curWood = player ? player.model.woodCount : 0;
        int curMetal = player ? player.model.metalCount : 0;
        
        // Cấu hình số lượng cần thiết (phải khớp với BoatRepairStation)
        int maxWood = 3;
        int maxMetal = 2;

        // 2. Kiểm tra trạng thái cốt truyện
        switch (qm.princeState)
        {
            // --- GIAI ĐOẠN 1: GẶP HOÀNG TỬ ---
            case PrinceQuestState.None:
                SetupMain("Đến gặp Hoàng tử tại quảng trường", princeTransform);
                break;

            // --- GIAI ĐOẠN 2: SỬA KIẾM ---
            case PrinceQuestState.IntroDone:
            case PrinceQuestState.Accepted: 
                SetupMain("Sửa chữa thanh kiếm tại Lò rèn", anvilTransform);
                break;

            // --- GIAI ĐOẠN 3: TRẢ KIẾM ---
            case PrinceQuestState.Completed:
                SetupMain("Quay lại báo cáo với Hoàng tử", princeTransform);
                break;

            // --- GIAI ĐOẠN 4: SỬA THUYỀN & THU THẬP NGUYÊN LIỆU ---
            case PrinceQuestState.ShipQuest:
            case PrinceQuestState.ShipDoing:
                SetupMain("Đến bến cảng để sửa chữa thuyền", boatTransform);

                // KÍCH HOẠT NHIỆM VỤ PHỤ: Thu thập
                sideQuestObj.SetActive(true);

                if (curWood >= maxWood && curMetal >= maxMetal)
                {
                    // Đã đủ đồ -> Báo đi sửa
                    SetupSide("<color=green>Đã đủ nguyên liệu! Hãy nhấn F tại thuyền để sửa.</color>", boatTransform);
                }
                else
                {
                    // Chưa đủ -> Hiện tiến độ
                    string progress = $"Thu thập vật liệu sửa thuyền:\n" +
                                      $"- Gỗ: {curWood}/{maxWood}\n" +
                                      $"- Sắt: {curMetal}/{maxMetal}";
                    // Nút dẫn đường phụ có thể dẫn ra khu rừng (materialZone) hoặc ẩn đi nếu để null
                    SetupSide(progress, materialZone); 
                }
                break;

            // --- GIAI ĐOẠN 5: TRẢ THUYỀN ---
            case PrinceQuestState.ShipDone:
                SetupMain("Báo cáo hoàn thành với Hoàng tử", princeTransform);
                break;

            // --- GIAI ĐOẠN 6: TÌM MẢNH VỠ ---
            case PrinceQuestState.ShipDoneForever:
                SetupMain("Thu thập 3 mảnh vỡ Vương miện", fragmentZone);
                
                // (Tùy chọn) Có thể hiện thêm nhiệm vụ phụ khác ở đây nếu muốn
                // sideQuestObj.SetActive(true);
                // SetupSide("Giúp đỡ dân làng (Nhiệm vụ phụ)", someVillagerTransform);
                break;
        }
        
        // 3. Rebuild Layout để Scroll View không bị lỗi hiển thị
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
    }

    // Hàm hỗ trợ setup Nhiệm vụ Chính
    void SetupMain(string desc, Transform target)
    {
        mainQuestText.text = "<b>CHÍNH:</b> " + desc;
        
        if (mainNavBtn != null)
        {
            mainNavBtn.Setup(target, desc);
            // Chỉ hiện nút nếu có mục tiêu cụ thể
            mainNavBtn.gameObject.SetActive(target != null);
        }
    }

    // Hàm hỗ trợ setup Nhiệm vụ Phụ
    void SetupSide(string desc, Transform target)
    {
        sideQuestText.text = "<i>PHỤ:</i> " + desc;
        
        if (sideNavBtn != null)
        {
            sideNavBtn.Setup(target, desc);
            sideNavBtn.gameObject.SetActive(target != null);
        }
    }

    // --- CÁC HÀM TIỆN ÍCH ---

    // Hàm bật/tắt bảng nhiệm vụ (Gán vào nút J hoặc ESC)
    public void TogglePanel()
    {
        bool isActive = !questPanel.activeSelf;
        questPanel.SetActive(isActive);

        if (isActive)
        {
            UpdateQuestUI(); // Cập nhật lại số liệu khi mở bảng
            
            // Mở khóa chuột để bấm
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // Khóa chuột lại để chơi tiếp
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Hàm tự động kích hoạt dẫn đường (Được gọi từ PrinceNPC hoặc BoatRepairStation)
    public void AutoClickMainQuest()
    {
        // Kiểm tra xem nút có tồn tại và đang active không
        if (mainNavBtn != null && mainNavBtn.gameObject.activeSelf)
        {
            Debug.Log("Auto-Nav: Đang kích hoạt dẫn đường tới mục tiêu hiện tại.");
            mainNavBtn.OnClickTask();
        }
    }
}