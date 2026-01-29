using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SharkManager : MonoBehaviour
{
    public static SharkManager Instance;

    [Header("Core Settings")]
    [Tooltip("Prefab phải có SharkController và NavMeshAgent")]
    public GameObject sharkPrefab;
    [Tooltip("Tổng số lượng cá mập muốn sinh ra")]
    public int sharkCount = 12;

    [Header("Spawn Configuration")]
    [Tooltip("Kéo 4 điểm Spawn (GameObject) vào đây")]
    public Transform[] fixedSpawnPoints;

    [Header("Smart Patrol Settings")]
    [Tooltip("Bán kính hoạt động của cá")]
    public float patrolRadius = 200f;
    [Tooltip("Số lần thử tìm điểm để chọn ra điểm tốt nhất (Càng cao càng thông minh)")]
    public int searchIterations = 10; 

    // Danh sách quản lý công khai
    public List<SharkController> ActiveSharks = new List<SharkController>();
    
    // Cache Player để tối ưu hiệu năng
    private PlayerController _playerRef;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) _playerRef = playerObj.GetComponent<PlayerController>();
        else Debug.LogWarning("SharkManager: Không tìm thấy Player!");

        SpawnSharks();
    }

    // --- MAIN UPDATE LOOP (TỐI ƯU HÓA) ---
    void Update()
    {
        if (_playerRef == null) return;

        // Kiểm tra trạng thái an toàn của người chơi 1 lần duy nhất cho cả đàn
        bool isPlayerSafe = _playerRef.IsTraveling; 

        for (int i = 0; i < ActiveSharks.Count; i++)
        {
            if (ActiveSharks[i] == null) continue;
            // Gọi hàm update thủ công cho từng con
            ActiveSharks[i].ManualUpdate(_playerRef, isPlayerSafe);
        }
    }

    // --- LOGIC SPAWN & STARBURST (TỎA HÌNH SAO) ---
    public void SpawnSharks()
    {
        // 1. Dọn dẹp cá cũ
        foreach (var shark in ActiveSharks) { if (shark != null) Destroy(shark.gameObject); }
        ActiveSharks.Clear();

        if (fixedSpawnPoints == null || fixedSpawnPoints.Length == 0)
        {
            Debug.LogError("Chưa gán Fixed Spawn Points!");
            return;
        }

        // Tính toán góc chia: Ví dụ 12 con / 4 điểm = 3 con/điểm -> Mỗi con cách nhau 120 độ
        int sharksPerPoint = Mathf.CeilToInt((float)sharkCount / fixedSpawnPoints.Length);
        float angleStep = 360f / sharksPerPoint;

        for (int i = 0; i < sharkCount; i++)
        {
            int spawnIndex = i % fixedSpawnPoints.Length;      // Điểm spawn số mấy
            int orderInPoint = i / fixedSpawnPoints.Length;    // Con thứ mấy tại điểm đó
            
            Transform spawnOrigin = fixedSpawnPoints[spawnIndex];

            // Vị trí sinh ra (Ngẫu nhiên nhẹ 1 chút để không trùng mesh)
            Vector3 spawnPos = spawnOrigin.position + Random.insideUnitSphere * 1.0f;
            spawnPos.y = spawnOrigin.position.y; 

            // Instantiate
            GameObject newSharkObj = Instantiate(sharkPrefab, spawnPos, Quaternion.identity);
            
            // Fix lỗi NavMesh: Tắt Agent -> Đặt vị trí -> Bật Agent
            NavMeshAgent agent = newSharkObj.GetComponent<NavMeshAgent>();
            if (agent != null) { agent.enabled = false; newSharkObj.transform.position = spawnPos; agent.enabled = true; }

            SharkController sharkCtrl = newSharkObj.GetComponent<SharkController>();
            if (sharkCtrl != null) 
            {
                ActiveSharks.Add(sharkCtrl);

                // --- TÍNH TOÁN HƯỚNG BURST (TỎA RA) ---
                // Góc bơi = Góc cơ bản * Thứ tự
                float burstAngle = (angleStep * orderInPoint); 
                
                // Chuyển góc thành Vector hướng
                Vector3 burstDir = Quaternion.Euler(0, burstAngle, 0) * Vector3.forward;

                // Điểm đến: Cách xa 80m theo hướng đó (15f * 5s = 75m -> lấy 80m cho dư)
                Vector3 burstTarget = spawnPos + burstDir * 80f;

                // Kích hoạt chế độ bơi nhanh
                sharkCtrl.SetupBurstMode(burstTarget);
            }
        }
        Debug.Log($"SharkManager: Spawned {sharkCount} sharks with Burst Mode.");
    }

    // --- LOGIC TÌM ĐIỂM THÔNG MINH (RESERVATION SYSTEM) ---
    public Vector3 GetSmartPatrolPoint(SharkController requestingShark)
    {
        Vector3 bestPoint = Vector3.zero;
        float bestScore = -1f;

        for (int i = 0; i < searchIterations; i++)
        {
            Vector3 candidatePoint = GetRandomNavMeshPoint();
            if (candidatePoint == Vector3.zero) continue;

            // Tính điểm dựa trên vị trí VÀ điểm đến của các con khác
            float distScore = GetScoreBasedOnOthers(candidatePoint, requestingShark);
            
            // Cộng thêm nhiễu (Noise) để phá vỡ sự đồng bộ
            float finalScore = distScore + Random.Range(0f, 25f); 

            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestPoint = candidatePoint;
            }
        }
        
        // Nếu tìm được điểm tốt thì trả về, không thì trả về điểm random thường
        return (bestPoint != Vector3.zero) ? bestPoint : GetRandomNavMeshPoint();
    }

    // Tính xem điểm candidatePoint có xa các con cá khác không
    float GetScoreBasedOnOthers(Vector3 candidatePoint, SharkController me)
    {
        float minDst = float.MaxValue;

        foreach (var otherShark in ActiveSharks)
        {
            if (otherShark == null) continue;
            if (otherShark == me) continue; // Bỏ qua chính mình

            // 1. Khoảng cách tới VỊ TRÍ HIỆN TẠI của con kia
            float d1 = Vector3.Distance(candidatePoint, otherShark.transform.position);
            
            // 2. Khoảng cách tới ĐIỂM ĐẾN DỰ KIẾN của con kia (Cơ chế Đặt Chỗ)
            float d2 = float.MaxValue;
            if (otherShark.CurrentDestination != Vector3.zero)
            {
                d2 = Vector3.Distance(candidatePoint, otherShark.CurrentDestination);
            }

            // Lấy khoảng cách nhỏ nhất (Rủi ro cao nhất)
            float riskDistance = Mathf.Min(d1, d2);

            if (riskDistance < minDst) minDst = riskDistance;
        }
        return minDst;
    }

    // Tìm điểm ngẫu nhiên trên NavMesh (Public để Shark gọi nếu cần)
    public Vector3 GetRandomNavMeshPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * patrolRadius;
            randomPos.y = transform.position.y;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 30f, NavMesh.AllAreas)) return hit.position;
        }
        return Vector3.zero;
    }
}