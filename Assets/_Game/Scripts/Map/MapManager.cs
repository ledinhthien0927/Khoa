using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // Dùng cho TextMeshPro

public class MapManager : MonoBehaviour
{
    public static MapManager Instance; // Singleton để gọi từ PlayerController

    [Header("UI References")]
    [SerializeField] private GameObject mapPanel;      // Panel bản đồ
    [SerializeField] private Button[] islandButtons;   // Danh sách nút bấm các đảo (0, 1, 2...)
    [SerializeField] private GameObject[] lockIcons;   // Danh sách icon ổ khóa tương ứng
    [SerializeField] private GameObject playerMapIcon; // Icon đầu người chơi trên bản đồ
    [SerializeField] private TextMeshProUGUI statusText; // Dòng chữ "Chọn địa điểm..."

    [Header("Boat System")]
    [SerializeField] private BoatController boat;       // Script điều khiển thuyền
    [SerializeField] private Transform[] boatDockPoints; // Vị trí thuyền đỗ tại các đảo (Trên mặt nước)
    
    [Header("Player & Spawn")]
    [SerializeField] private GameObject player;         // Nhân vật
    [SerializeField] private Transform[] playerSpawnPoints; // Vị trí spawn trên bờ sau khi xuống thuyền
    
    [Header("Settings")]
    public int maxUnlockedIndex = 1; // Mặc định mở đến đảo 2 (Index 1)

    // Các biến trạng thái nội bộ
    private int _currentIslandIndex = 0; // Đang ở đảo nào
    private bool _isMapOpen = false;

    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        // Đảm bảo map tắt khi bắt đầu game
        CloseMap();
        
        // Cập nhật giao diện lần đầu để icon nằm đúng đảo 1
        UpdateMapUI();
    }

    // --- CÁC HÀM ĐÓNG/MỞ MAP (Gọi từ PlayerController) ---

    public void ToggleMap()
    {
        _isMapOpen = !_isMapOpen;
        if (_isMapOpen) OpenMap();
        else CloseMap();
    }

    public void OpenMap()
    {
        _isMapOpen = true;
        mapPanel.SetActive(true);
        UpdateMapUI(); // Cập nhật trạng thái nút/khóa mỗi khi mở
        if (statusText != null) statusText.text = "Chọn địa điểm muốn đến...";
    }

    public void CloseMap()
    {
        _isMapOpen = false;
        mapPanel.SetActive(false);
    }

    // --- CẬP NHẬT GIAO DIỆN BẢN ĐỒ ---
    void UpdateMapUI()
    {
        for (int i = 0; i < islandButtons.Length; i++)
        {
            // 1. Kiểm tra điều kiện mở khóa
            bool isUnlocked = (i <= maxUnlockedIndex);
            
            // 2. Kiểm tra có phải đảo đang đứng không
            bool isCurrentIsland = (i == _currentIslandIndex);

            // 3. Logic nút bấm: Chỉ bấm được khi Đã mở khóa VÀ Không phải đảo đang đứng
            islandButtons[i].interactable = isUnlocked && !isCurrentIsland;

            // 4. Ẩn/Hiện ổ khóa
            if (lockIcons[i] != null) lockIcons[i].SetActive(!isUnlocked);

            // 5. Di chuyển Icon đầu người chơi đến vị trí nút của đảo hiện tại
            if (isCurrentIsland && playerMapIcon != null)
            {
                playerMapIcon.transform.position = islandButtons[i].transform.position;
                
                // Đảm bảo icon luôn hiện
                if (!playerMapIcon.activeSelf) playerMapIcon.SetActive(true);
            }
        }
    }

    // --- SỰ KIỆN KHI BẤM NÚT TRÊN BẢN ĐỒ ---
    // Gán hàm này vào OnClick của từng Button (0, 1, 2)
    public void OnTravelButtonClicked(int destinationIndex)
    {
        // Kiểm tra an toàn lần cuối
        if (destinationIndex > maxUnlockedIndex || destinationIndex == _currentIslandIndex) return;
        
        // Bắt đầu chuỗi hành động di chuyển
        StartCoroutine(TravelSequence(destinationIndex));
    }

    // --- CHUỖI HÀNH ĐỘNG DI CHUYỂN (CORE LOGIC) ---
    IEnumerator TravelSequence(int index)
    {
        // BƯỚC 1: Đóng Map ngay lập tức
        CloseMap(); 

        // BƯỚC 2: Khóa điều khiển nhân vật
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        
        // BƯỚC 3: Animation nhân vật leo lên thuyền (Lerp vị trí)
        float t = 0;
        Vector3 startPos = player.transform.position;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f; // Tốc độ leo
            player.transform.position = Vector3.Lerp(startPos, boat.steeringPos.position, t);
            yield return null;
        }
        
        // Gắn nhân vật vào thuyền để không bị trôi
        player.transform.SetParent(boat.transform);
        player.transform.position = boat.steeringPos.position;
        player.transform.rotation = boat.steeringPos.rotation;

        // BƯỚC 4: Chuyển Camera sang góc nhìn thuyền
        if (Camera.main != null) Camera.main.gameObject.SetActive(false);
        boat.SetBoatCamera(true);

        // BƯỚC 5: Thuyền bắt đầu chạy (Chờ đến khi tới nơi)
        Debug.Log("Thuyền bắt đầu rời bến...");
        yield return StartCoroutine(boat.MoveToTarget(boatDockPoints[index].position));
        Debug.Log("Thuyền đã cập bến!");

        // BƯỚC 6: Trả lại Camera chính
        boat.SetBoatCamera(false); 
        // Tìm và bật lại Main Camera (Gọi qua PlayerController cho an toàn)
        if (player.GetComponent<PlayerController>() != null)
        {
            player.GetComponent<PlayerController>().EnableMainCamera();
        }
        else 
        {
            // Fallback: Tìm thủ công nếu không có script
            GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCam != null) mainCam.SetActive(true);
        }

        // BƯỚC 7: Cập nhật dữ liệu tiến độ (QUAN TRỌNG)
        // Nếu đến đảo xa nhất hiện tại -> Mở khóa đảo kế tiếp
        if (index == maxUnlockedIndex && maxUnlockedIndex < islandButtons.Length - 1)
        {
            maxUnlockedIndex++;
            Debug.Log("Đã mở khóa hòn đảo mới!");
        }
        
        // Cập nhật đảo hiện tại
        _currentIslandIndex = index;

        // Gọi cập nhật UI ngay để lần sau mở map thấy đúng vị trí
        UpdateMapUI(); 

        // BƯỚC 8: Nhân vật leo xuống đảo (Spawn lên bờ)
        player.transform.SetParent(null); // Gỡ khỏi thuyền
        player.transform.position = playerSpawnPoints[index].position;
        player.transform.rotation = playerSpawnPoints[index].rotation;

        // Trả lại điều khiển
        if (cc != null) cc.enabled = true;
    }
}