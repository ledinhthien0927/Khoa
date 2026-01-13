using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mapPanel;      // Panel chứa bản đồ
    [SerializeField] private Button[] islandButtons;   // 3 nút: 0=Đảo 1, 1=Đảo 2, 2=Đảo 3
    [SerializeField] private GameObject[] lockIcons;   // 3 icon ổ khóa tương ứng
    
    // [MỚI] Icon đại diện nhân vật trên bản đồ
    [SerializeField] private GameObject playerMapIcon; 

    [Header("Gameplay References")]
    [SerializeField] private GameObject player;        // Kéo nhân vật vào đây
    [SerializeField] private Transform[] spawnPoints;  // 3 vị trí spawn
    
    [Header("Settings")]
    public int maxUnlockedIndex = 1; // Mặc định mở đến đảo 2

    private int _currentIslandIndex = 0; // Đảo hiện tại (Mặc định bắt đầu ở Đảo 1 - index 0)

    void Start()
    {
        mapPanel.SetActive(false);
        // Set vị trí ban đầu của player icon ngay khi game bắt đầu (nếu cần)
        UpdateMapUI();
    }

    public void OpenMap()
    {
        UpdateMapUI(); // Cập nhật vị trí icon và trạng thái nút trước khi hiện map
        mapPanel.SetActive(true);
        Time.timeScale = 0; 
    }

    public void CloseMap()
    {
        mapPanel.SetActive(false);
        Time.timeScale = 1; 
    }

    // --- CẬP NHẬT UI (Quan trọng nhất) ---
    void UpdateMapUI()
    {
        for (int i = 0; i < islandButtons.Length; i++)
        {
            // 1. Kiểm tra đã mở khóa chưa
            bool isUnlocked = (i <= maxUnlockedIndex);
            
            // 2. Kiểm tra có phải đảo đang đứng không
            bool isCurrentIsland = (i == _currentIslandIndex);

            // 3. Logic bật/tắt nút:
            // Nút chỉ bấm được khi: Đã mở khóa VÀ KHÔNG PHẢI đảo đang đứng
            islandButtons[i].interactable = isUnlocked && !isCurrentIsland;
            
            // 4. Ẩn/Hiện ổ khóa (Giữ nguyên)
            if (lockIcons[i] != null)
            {
                lockIcons[i].SetActive(!isUnlocked);
            }

            // 5. [MỚI] Di chuyển Icon nhân vật
            // Nếu đây là đảo hiện tại -> Di chuyển icon đến vị trí nút này
            if (isCurrentIsland && playerMapIcon != null)
            {
                playerMapIcon.transform.position = islandButtons[i].transform.position;
                
                // Đảm bảo icon luôn hiện (đề phòng bị ẩn)
                if (!playerMapIcon.activeSelf) playerMapIcon.SetActive(true);
            }
        }
    }

    public void OnTravelButtonClicked(int destinationIndex)
    {
        if (destinationIndex > maxUnlockedIndex) return;
        // Nếu bấm nhầm vào đảo đang đứng (dù đã tắt nút) thì return luôn
        if (destinationIndex == _currentIslandIndex) return; 

        StartCoroutine(TeleportRoutine(destinationIndex));
    }

    IEnumerator TeleportRoutine(int index)
    {
        CloseMap();
        
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = spawnPoints[index].position;
        player.transform.rotation = spawnPoints[index].rotation;

        if (cc != null) cc.enabled = true;

        // Logic mở khóa đảo tiếp theo
        if (index == maxUnlockedIndex && maxUnlockedIndex < islandButtons.Length - 1)
        {
            Debug.Log($"Đã đến Đảo {index + 1}. Mở khóa đường đến Đảo {index + 2}!");
            maxUnlockedIndex++; 
        }

        // Cập nhật đảo hiện tại là đảo vừa đến
        _currentIslandIndex = index;
        
        yield return null;
    }
}