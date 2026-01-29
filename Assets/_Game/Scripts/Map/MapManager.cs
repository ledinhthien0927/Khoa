using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// MapManager Final Version - Graph Pathfinding
/// Tính năng: Di chuyển theo mạng lưới điểm định sẵn (Points 4->2->0->Ladder).
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    #region --- CẤU HÌNH (CONFIGURATION) ---
    [Header("UI References")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Button[] islandButtons;
    [SerializeField] private GameObject[] lockIcons;
    [SerializeField] private GameObject playerMapIcon;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Boat System")]
    [SerializeField] private BoatController boat;
    [SerializeField] private Transform[] boatDockPoints;
    
    [Header("--- PATHFINDING SETUP (QUAN TRỌNG) ---")]
    [Tooltip("Kéo 6 điểm vào đây. Điểm 0,1 phải gần cầu thang nhất.")]
    [SerializeField] private Transform[] approachWaypoints;

    [Tooltip("Quy định điểm tiếp theo. Ví dụ: Element 4 điền số 2 nghĩa là từ điểm 4 sẽ đi về điểm 2. Điền -1 nghĩa là về Cầu thang.")]
    [SerializeField] private int[] pathConnections; 
    // Gợi ý setup: [-1, -1, 0, 1, 2, 3]

    [Header("Boat Waypoints (Leo trèo)")]
    [SerializeField] private Transform boatRailingPoint; 
    [SerializeField] private Transform boatDeckEdgePoint; 
    [SerializeField] private Transform boatMiddlePoint; 
    [SerializeField] private Transform boatDeparturePoint; 

    [Header("Player & Spawn")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("Travel Settings")]
    [SerializeField] private float walkSpeed = 4.0f;
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private float climbUpDuration = 2.0f; 
    [SerializeField] private float climbDownDuration = 1.5f; 
    [SerializeField] private float dropSpeed = 8.0f;     
    [SerializeField] private float stopDistance = 0.05f; 
    #endregion

    // --- TRẠNG THÁI NỘI BỘ ---
    [HideInInspector] public int maxUnlockedIndex = 1;
    private int _currentIslandIndex = 0;
    private bool _isMapOpen = false;

    // --- CACHED COMPONENTS ---
    private PlayerController _pc;
    private PlayerView _view;
    private CharacterController _cc;
    private Animator _playerAnim;

    void Awake()
    {
        Instance = this;
        if (player)
        {
            _pc = player.GetComponent<PlayerController>();
            _cc = player.GetComponent<CharacterController>();
            _playerAnim = player.GetComponentInChildren<Animator>();
            if (_pc) _view = _pc.GetView();
        }
    }

    void Start()
    {
        CloseMap();
        UpdateMapUI();
    }

    #region --- MAP UI LOGIC ---
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
        if (statusText) statusText.text = "Hãy chọn nơi muốn đến...";

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseMap()
    {
        _isMapOpen = false;
        mapPanel.SetActive(false);
        if (_cc != null && _cc.enabled)
        {
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
            if (lockIcons[i]) lockIcons[i].SetActive(!isUnlocked);

            if (isCurrentIsland && playerMapIcon)
                playerMapIcon.transform.position = islandButtons[i].transform.position;
        }
    }
    #endregion

    #region --- TRAVEL LOGIC (MAIN FLOW) ---
    public void OnTravelButtonClicked(int destinationIndex)
    {
        if (destinationIndex > maxUnlockedIndex || destinationIndex == _currentIslandIndex) return;
        StartCoroutine(TravelSequence(destinationIndex));
    }

    IEnumerator TravelSequence(int destinationIndex)
    {
        // 1. SETUP
        CloseMap();
        _pc.SetTravelMode(true);

        // ==========================================================
        // 2. TÌM ĐƯỜNG VỀ CẦU THANG (LOGIC MỚI)
        // ==========================================================
        
        // Bước 2.1: Tìm điểm gần nhất trong 6 điểm để bắt đầu
        int currentWaypointIndex = GetClosestWaypointIndex();

        // Bước 2.2: Di chuyển lần lượt theo chuỗi kết nối (Chain)
        // Vòng lặp này sẽ chạy mãi cho đến khi gặp điểm có kết nối là -1 (Tức là về cầu thang)
        if (currentWaypointIndex != -1 && pathConnections != null && pathConnections.Length > currentWaypointIndex)
        {
            // Đầu tiên, đi đến điểm gần nhất đã tìm thấy
            yield return StartCoroutine(MovePlayerTo(approachWaypoints[currentWaypointIndex].position));

            // Sau đó, liên tục hỏi: "Điểm tiếp theo của tôi là ai?" và đi đến đó
            while (true)
            {
                int nextIndex = pathConnections[currentWaypointIndex];

                if (nextIndex == -1) 
                {
                    // Nếu là -1 thì thoát vòng lặp để đi ra cầu thang
                    break; 
                }
                
                // Đi đến điểm tiếp theo
                yield return StartCoroutine(MovePlayerTo(approachWaypoints[nextIndex].position));
                
                // Cập nhật điểm hiện tại thành điểm vừa đến để tiếp tục dò
                currentWaypointIndex = nextIndex;
            }
        }

        // Bước 2.3: Điểm cuối cùng luôn là Chân Cầu Thang (Access Point)
        yield return StartCoroutine(MovePlayerTo(boat.accessPoint.position));


        // ==========================================================
        // 3. LEO LÊN TÀU
        // ==========================================================
        SetPhysicsEnabled(false); 
        _view.TriggerClimbUp();

        yield return StartCoroutine(LerpPlayerPosition(player.transform.position, boatRailingPoint.position, climbUpDuration * 0.6f));

        Transform entryPoint = boatDeckEdgePoint != null ? boatDeckEdgePoint : boat.deckEdgePoint;
        yield return StartCoroutine(LerpPlayerPosition(player.transform.position, entryPoint.position, climbUpDuration * 0.4f));

        player.transform.position += Vector3.up * 0.05f; 
        yield return new WaitForEndOfFrame();
        SetPhysicsEnabled(true); 
        // ==========================================================


        // 4. ĐI ĐẾN CHỖ LÁI
        if (boatMiddlePoint != null)
            yield return StartCoroutine(MovePlayerTo(boatMiddlePoint.position));

        yield return StartCoroutine(MovePlayerTo(boat.steeringPos.position, boat.steeringPos.rotation));
        
        AttachPlayerToBoat(true);
        _view.SetSteering(true);

        // 5. TÀU CHẠY
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(BoatTravelRoutine(destinationIndex));

        // 6. CHUẨN BỊ XUỐNG
        _view.SetSteering(false);
        AttachPlayerToBoat(false); 

        // 7. DI CHUYỂN RA VỊ TRÍ XUỐNG TÀU
        if (boatMiddlePoint != null)
            yield return StartCoroutine(MovePlayerTo(boatMiddlePoint.position));

        if (boatDeparturePoint != null)
            yield return StartCoroutine(MovePlayerTo(boatDeparturePoint.position));
        else
            yield return StartCoroutine(MovePlayerTo(entryPoint.position));


        // ==========================================================
        // 8. LEO XUỐNG & RƠI TỰ DO
        // ==========================================================
        SetPhysicsEnabled(false);
        _view.TriggerClimbDown();

        yield return StartCoroutine(LerpPlayerPosition(player.transform.position, boatRailingPoint.position, climbDownDuration));

        Vector3 dropStartPos = boatRailingPoint.position;
        Vector3 groundPos = new Vector3(dropStartPos.x, boat.accessPoint.position.y, dropStartPos.z);
        yield return StartCoroutine(SimulateDrop(groundPos));
        
        SetPhysicsEnabled(true);
        // ==========================================================


        // 9. VỀ ĐÍCH
        player.transform.SetParent(null); 
        yield return StartCoroutine(MovePlayerTo(playerSpawnPoints[destinationIndex].position, playerSpawnPoints[destinationIndex].rotation));

        FinishTravel(destinationIndex);
    }
    #endregion

    #region --- PATHFINDING LOGIC ---
    int GetClosestWaypointIndex()
    {
        if (approachWaypoints == null || approachWaypoints.Length == 0) return -1;

        int bestIndex = 0;
        float minDst = float.MaxValue;

        for (int i = 0; i < approachWaypoints.Length; i++)
        {
            if (approachWaypoints[i] == null) continue;
            
            float dst = Vector3.Distance(player.transform.position, approachWaypoints[i].position);
            if (dst < minDst)
            {
                minDst = dst;
                bestIndex = i;
            }
        }
        return bestIndex;
    }
    #endregion

    #region --- HELPER COROUTINES ---

    IEnumerator MovePlayerTo(Vector3 targetPos, Quaternion? targetRot = null)
    {
        while (Vector3.Distance(player.transform.position, targetPos) > stopDistance)
        {
            player.transform.position = Vector3.MoveTowards(player.transform.position, targetPos, walkSpeed * Time.deltaTime);
            
            Vector3 dir = (targetPos - player.transform.position).normalized;
            dir.y = 0; 
            if (dir != Vector3.zero)
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
            }
            yield return null;
        }
        player.transform.position = targetPos;
        
        if (targetRot.HasValue)
        {
            while (Quaternion.Angle(player.transform.rotation, targetRot.Value) > 1f)
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot.Value, rotationSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    IEnumerator LerpPlayerPosition(Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            player.transform.position = Vector3.Lerp(start, end, t);
            
            Vector3 dir = (end - start).normalized;
            dir.y = 0; 
            if (dir != Vector3.zero) 
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

            yield return null;
        }
        player.transform.position = end;
    }

    IEnumerator BoatTravelRoutine(int destinationIndex)
    {
        if (Camera.main) Camera.main.gameObject.SetActive(false);
        boat.SetBoatCamera(true);
        boat.SetDestination(boatDockPoints[destinationIndex].position);

        bool originalRootMotion = false;
        if (_playerAnim) { originalRootMotion = _playerAnim.applyRootMotion; _playerAnim.applyRootMotion = false; }

        while (!boat.IsReachedDestination())
        {
            player.transform.position = boat.steeringPos.position;
            player.transform.rotation = boat.steeringPos.rotation;
            yield return null;
        }

        if (_playerAnim) _playerAnim.applyRootMotion = originalRootMotion;
        
        boat.SetBoatCamera(false);
        if (_pc) _pc.EnableMainCamera();
    }

    IEnumerator SimulateDrop(Vector3 targetGroundPos)
    {
        float currentX = player.transform.position.x;
        float currentZ = player.transform.position.z;
        
        while (player.transform.position.y > targetGroundPos.y + stopDistance)
        {
            float newY = Mathf.MoveTowards(player.transform.position.y, targetGroundPos.y, dropSpeed * Time.deltaTime);
            player.transform.position = new Vector3(currentX, newY, currentZ);
            yield return null;
        }
        player.transform.position = new Vector3(currentX, targetGroundPos.y, currentZ);
    }
    #endregion

    #region --- UTILITIES ---
    void SetPhysicsEnabled(bool isEnabled)
    {
        if (_cc) _cc.enabled = isEnabled;
    }

    void AttachPlayerToBoat(bool attach)
    {
        if (attach)
        {
            player.transform.SetParent(boat.transform);
            player.transform.position = boat.steeringPos.position;
            player.transform.rotation = boat.steeringPos.rotation;
        }
    }

    void FinishTravel(int destinationIndex)
    {
        if (destinationIndex == maxUnlockedIndex && maxUnlockedIndex < islandButtons.Length - 1) maxUnlockedIndex++;
        _currentIslandIndex = destinationIndex;
        UpdateMapUI();
        _pc.SetTravelMode(false);
    }
    #endregion
}   