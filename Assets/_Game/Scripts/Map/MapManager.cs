using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Button[] islandButtons;
    [SerializeField] private GameObject[] lockIcons;
    [SerializeField] private GameObject playerMapIcon;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Boat System")]
    [SerializeField] private BoatController boat;       
    [SerializeField] private Transform[] boatDockPoints; 
    
    [Header("Player & Spawn")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform[] playerSpawnPoints; 
    
    [Header("Settings")]
    public int maxUnlockedIndex = 1;

    private int _currentIslandIndex = 0;
    private bool _isMapOpen = false;

    void Awake() { Instance = this; }

    void Start()
    {
        CloseMap();
        UpdateMapUI();
    }

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
        UpdateMapUI();
        if (statusText != null) statusText.text = "Hãy chọn nơi muốn đến...";
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseMap()
    {
        _isMapOpen = false;
        mapPanel.SetActive(false);
        if (!player.GetComponent<CharacterController>().enabled) {
        } else {
             Cursor.visible = false;
             Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void UpdateMapUI()
    {
        for (int i = 0; i < islandButtons.Length; i++)
        {
            bool isUnlocked = (i <= maxUnlockedIndex);
            bool isCurrentIsland = (i == _currentIslandIndex);
            islandButtons[i].interactable = isUnlocked && !isCurrentIsland;
            if (lockIcons[i] != null) lockIcons[i].SetActive(!isUnlocked);
            if (isCurrentIsland && playerMapIcon != null)
                playerMapIcon.transform.position = islandButtons[i].transform.position;
        }
    }

    public void OnTravelButtonClicked(int destinationIndex)
    {
        if (destinationIndex > maxUnlockedIndex || destinationIndex == _currentIslandIndex) return;
        StartCoroutine(TravelSequence(destinationIndex));
    }

    // --- CHUỖI HÀNH ĐỘNG DI CHUYỂN (ĐÃ ĐIỀU CHỈNH) ---
    IEnumerator TravelSequence(int destinationIndex)
    {
        // 1. Setup
        CloseMap(); 
        PlayerController pc = player.GetComponent<PlayerController>();
        PlayerView view = pc.GetView();
        pc.SetTravelMode(true); 

        // --- GIAI ĐOẠN 1: ĐI TỚI CHÂN CẦU THANG (Access Point) ---
        Vector3 targetAccessPos = boat.accessPoint.position; 
        
        // Chỉ di chuyển đến vị trí X, Z của AccessPoint, giữ nguyên Y của Player (để không bị chìm/nổi sai)
        // Hoặc nếu AccessPoint đặt chuẩn thì đi thẳng tới đó.
        
        player.transform.LookAt(new Vector3(targetAccessPos.x, player.transform.position.y, targetAccessPos.z));

        while (Vector3.Distance(player.transform.position, targetAccessPos) > 0.1f)
        {
            Vector3 dir = (targetAccessPos - player.transform.position).normalized;
            if(dir != Vector3.zero) player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetAccessPos, 4f * Time.deltaTime);
            yield return null;
        }
        
        // Đứng đúng vị trí chân cầu thang
        player.transform.position = targetAccessPos;
        player.transform.rotation = boat.accessPoint.rotation;

        // --- GIAI ĐOẠN 2: LEO LÊN (XỬ LÝ LỰC ĐẨY TẠI ĐÂY) ---
        view.TriggerClimbUp();
        
        // Thay vì chờ 1.5s, ta sẽ nâng Player lên độ cao của sàn tàu trong 1.5s
        float climbDuration = 1.5f; 
        float t = 0;
        Vector3 startClimbPos = player.transform.position;
        // Điểm đích leo lên: Lấy độ cao Y của SteeringPos (sàn tàu) nhưng giữ nguyên X,Z hiện tại
        Vector3 endClimbPos = new Vector3(startClimbPos.x, boat.steeringPos.position.y, startClimbPos.z);

        while (t < 1f)
        {
            t += Time.deltaTime / climbDuration;
            // Di chuyển thẳng đứng lên trên
            player.transform.position = Vector3.Lerp(startClimbPos, endClimbPos, t);
            yield return null;
        }

        // --- DI CHUYỂN VÀO VÔ LĂNG ---
        t = 0;
        Vector3 currentPosOnDeck = player.transform.position;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f; // Tốc độ chạy vào chỗ lái
            player.transform.position = Vector3.Lerp(currentPosOnDeck, boat.steeringPos.position, t);
            // Xoay người về hướng vô lăng cho đẹp
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, boat.steeringPos.rotation, t);
            yield return null;
        }
        
        // Gắn Player vào thuyền
        player.transform.SetParent(boat.transform);
        player.transform.position = boat.steeringPos.position;
        player.transform.rotation = boat.steeringPos.rotation;
        
        // CHỜ 0.5 GIÂY
        yield return new WaitForSeconds(0.5f);

        // --- GIAI ĐOẠN 3: TÀU CHẠY ---
        view.SetSteering(true);
        if (Camera.main != null) Camera.main.gameObject.SetActive(false);
        boat.SetBoatCamera(true);

        boat.SetDestination(boatDockPoints[destinationIndex].position);

        while (!boat.IsReachedDestination())
        {
            player.transform.position = boat.steeringPos.position; 
            yield return null; 
        }

        // --- GIAI ĐOẠN 4: ĐI RA MÉP ĐỂ XUỐNG ---
        view.SetSteering(false);
        boat.SetBoatCamera(false); 
        pc.EnableMainCamera();

        // Đi từ vô lăng ra mép tàu (vị trí AccessPoint nhưng ở độ cao sàn tàu)
        Vector3 disembarkPosOnDeck = new Vector3(boat.accessPoint.position.x, boat.steeringPos.position.y, boat.accessPoint.position.z);
        
        t = 0;
        Vector3 startDisembarkPos = player.transform.position;
        player.transform.LookAt(disembarkPosOnDeck);

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            player.transform.position = Vector3.Lerp(startDisembarkPos, disembarkPosOnDeck, t);
            yield return null;
        }
        
        // Xoay mặt ra ngoài
        player.transform.rotation = Quaternion.LookRotation(-boat.accessPoint.forward); 

        // --- GIAI ĐOẠN 5: LEO XUỐNG (HẠ ĐỘ CAO) ---
        view.TriggerClimbDown();
        
        t = 0;
        Vector3 startDownPos = player.transform.position;
        // Điểm đích leo xuống: Chính là vị trí gốc của AccessPoint (dưới thấp)
        Vector3 endDownPos = boat.accessPoint.position;

        while (t < 1f)
        {
            t += Time.deltaTime / climbDuration; // Dùng lại thời gian leo
            // Hạ dần độ cao xuống
            player.transform.position = Vector3.Lerp(startDownPos, endDownPos, t);
            yield return null;
        }

        player.transform.SetParent(null);
        player.transform.position = playerSpawnPoints[destinationIndex].position;
        player.transform.rotation = playerSpawnPoints[destinationIndex].rotation;

        if (destinationIndex == maxUnlockedIndex && maxUnlockedIndex < islandButtons.Length - 1) maxUnlockedIndex++;
        _currentIslandIndex = destinationIndex;
        UpdateMapUI(); 
        pc.SetTravelMode(false);
    }
}