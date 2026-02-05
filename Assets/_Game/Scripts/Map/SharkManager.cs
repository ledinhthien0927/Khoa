using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SharkManager : MonoBehaviour
{
    public static SharkManager Instance;

    [Header("Core Settings")]
    public GameObject sharkPrefab;
    public int sharkCount = 12;

    [Header("Spawn Configuration")]
    public Transform[] fixedSpawnPoints;

    [Header("Smart Patrol Settings")]
    public float patrolRadius = 20f; // Giảm radius lại vì giờ patrol quanh Anchor
    public int searchIterations = 5; 

    public List<SharkController> ActiveSharks = new List<SharkController>();
    
    private PlayerController _playerRef;
    private IDamageable _playerDamageable; 

    void Awake()
    {
        // [AN TOÀN]: Singleton Check đúng chuẩn
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) 
        {
            _playerRef = playerObj.GetComponent<PlayerController>();
            _playerDamageable = playerObj.GetComponent<IDamageable>(); 
        }
        else Debug.LogWarning("SharkManager: Không tìm thấy Player!");

        SpawnSharks();
    }

    void Update()
    {
        if (_playerRef == null) return;

        bool isPlayerSafe = _playerRef.IsTraveling; 

        // [MỞ RỘNG SAU NÀY]: Có thể dùng vòng for ngược nếu có logic Destroy shark runtime
        for (int i = 0; i < ActiveSharks.Count; i++)
        {
            if (ActiveSharks[i] == null) continue;
            ActiveSharks[i].ManualUpdate(_playerRef, isPlayerSafe, _playerDamageable);
        }
    }

    public void SpawnSharks()
    {
        foreach (var shark in ActiveSharks) { if (shark != null) Destroy(shark.gameObject); }
        ActiveSharks.Clear();

        if (fixedSpawnPoints == null || fixedSpawnPoints.Length == 0) return;

        int sharksPerPoint = Mathf.CeilToInt((float)sharkCount / fixedSpawnPoints.Length);
        float angleStep = 360f / sharksPerPoint;

        for (int i = 0; i < sharkCount; i++)
        {
            int spawnIndex = i % fixedSpawnPoints.Length;
            int orderInPoint = i / fixedSpawnPoints.Length;
            Transform spawnOrigin = fixedSpawnPoints[spawnIndex];

            Vector3 rawSpawnPos = spawnOrigin.position + Random.insideUnitSphere * 1.0f;
            rawSpawnPos.y = spawnOrigin.position.y; 
            Vector3 finalSpawnPos = spawnOrigin.position; 
            NavMeshHit hit;

            if (NavMesh.SamplePosition(rawSpawnPos, out hit, 5.0f, NavMesh.AllAreas))
                finalSpawnPos = hit.position;

            GameObject newSharkObj = Instantiate(sharkPrefab, finalSpawnPos, Quaternion.identity);
            
            // Fix NavMeshAgent placement issue
            NavMeshAgent agent = newSharkObj.GetComponent<NavMeshAgent>();
            if (agent != null) { agent.enabled = false; newSharkObj.transform.position = finalSpawnPos; agent.enabled = true; }

            SharkController sharkCtrl = newSharkObj.GetComponent<SharkController>();
            if (sharkCtrl != null) 
            {
                // [LOGIC MỚI]: Gán Anchor Point là vị trí sinh ra
                sharkCtrl.AnchorPoint = finalSpawnPos;
                ActiveSharks.Add(sharkCtrl);
                
                // Burst setup
                float burstAngle = (angleStep * orderInPoint); 
                Vector3 burstDir = Quaternion.Euler(0, burstAngle, 0) * Vector3.forward;
                Vector3 burstTarget = finalSpawnPos + burstDir * 80f;
                sharkCtrl.SetupBurstMode(burstTarget);
            }
        }
    }

    public Vector3 GetSmartPatrolPoint(SharkController requestingShark)
    {
        Vector3 bestPoint = Vector3.zero;
        float bestScore = -1f;

        // [LOGIC MỚI]: Tâm tìm kiếm là Anchor của cá mập, KHÔNG PHẢI transform của Manager
        Vector3 searchCenter = requestingShark.AnchorPoint;

        for (int i = 0; i < searchIterations; i++)
        {
            Vector3 candidatePoint = GetRandomNavMeshPoint(searchCenter);
            if (candidatePoint == Vector3.zero) continue;
            
            float distScore = GetScoreBasedOnOthers(candidatePoint, requestingShark);
            float finalScore = distScore + Random.Range(0f, 25f); 
            
            if (finalScore > bestScore) { bestScore = finalScore; bestPoint = candidatePoint; }
        }
        return (bestPoint != Vector3.zero) ? bestPoint : GetRandomNavMeshPoint(searchCenter);
    }

    float GetScoreBasedOnOthers(Vector3 candidatePoint, SharkController me)
    {
        float minSqrDst = float.MaxValue; // Dùng bình phương khoảng cách
        
        // [TỐI ƯU]: Vòng lặp này vẫn O(N) nhưng dùng sqrMagnitude sẽ nhanh hơn nhiều
        foreach (var otherShark in ActiveSharks)
        {
            if (otherShark == null || otherShark == me) continue;
            
            // Nếu con kia ở quá xa (> 50m) thì không cần quan tâm (Optimization check)
            if ((otherShark.transform.position - candidatePoint).sqrMagnitude > 2500f) continue;

            float d1 = (candidatePoint - otherShark.transform.position).sqrMagnitude;
            float d2 = float.MaxValue;
            if (otherShark.CurrentDestination != Vector3.zero) 
                d2 = (candidatePoint - otherShark.CurrentDestination).sqrMagnitude;
            
            float riskDistance = Mathf.Min(d1, d2);
            if (riskDistance < minSqrDst) minSqrDst = riskDistance;
        }
        return minSqrDst; // Trả về điểm số (càng lớn càng tốt)
    }

    public Vector3 GetRandomNavMeshPoint(Vector3 center)
    {
        for (int i = 0; i < 5; i++) 
        {
            Vector3 randomPos = center + Random.insideUnitSphere * patrolRadius;
            randomPos.y = center.y;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 10f, NavMesh.AllAreas)) return hit.position;
        }
        return Vector3.zero;
    }
}