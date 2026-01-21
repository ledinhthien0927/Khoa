using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance;
    
    [Header("Settings")]
    public string playerTag = "Player";
    public LayerMask obstacleMask; 
    
    // List này giờ chứa cả Melee, Ranged, Alarm (vì tụi nó đều là con của MonsterController)
    public List<MonsterController> allMonsters = new List<MonsterController>();
    private Transform playerTransform;

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        allMonsters.Clear();
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    public void RegisterMonster(MonsterController monster) => allMonsters.Add(monster);
    public void UnregisterMonster(MonsterController monster) => allMonsters.Remove(monster);

    void Update() { if (playerTransform != null) ExecuteAI(); }

    // [QUAN TRỌNG] Đổi thành public để RangedMonster có thể gọi dùng ké
    public bool CanSeePlayer(MonsterController m)
    {
        if (playerTransform == null) return false;
        float dist = Vector3.Distance(m.transform.position, playerTransform.position);
        if (dist > m.data.detectionRange) return false;

        Vector3 dir = (playerTransform.position - m.transform.position).normalized;
        bool ignoreFOV = (dist < 1.0f || m.isAlerted);

        if (!ignoreFOV && Vector3.Angle(m.transform.forward, dir) > m.data.viewAngle / 2f) return false; 
        if (Physics.Raycast(m.transform.position + Vector3.up, dir, dist, obstacleMask)) return false; 

        return true; 
    }

    void ExecuteAI()
    {
        for (int i = 0; i < allMonsters.Count; i++)
        {
            MonsterController monster = allMonsters[i];
            if (monster == null) continue;

            bool canSee = CanSeePlayer(monster);

            // --- A. LOGIC PHÁT HIỆN ---
            if (canSee && !monster.isTracking)
            {
                if (!monster.isAlerted)
                {
                    monster.currentDetectionTime += Time.deltaTime;
                    if (monster.currentDetectionTime >= monster.data.detectionTime) monster.isAlerted = true;
                }
            }
            else if (!monster.isTracking && !monster.isAlerted)
            {
                monster.currentDetectionTime -= Time.deltaTime;
                if (monster.currentDetectionTime < 0) monster.currentDetectionTime = 0;
            }

            // --- B. LOGIC TRACKING (GPS) ---
            if (monster.isTracking || (monster.isAlerted && canSee))
            {
                monster.lastKnownPosition = playerTransform.position;
                monster.searchWaitTime = 0f;
            }

            // --- C. QUYẾT ĐỊNH HÀNH VI ---
            if (monster.isTracking || (monster.isAlerted && canSee))
            {
                // [ĐIỂM SÁNG GIÁ NHẤT]
                // Không cần switch case nữa! Gọi thẳng hàm này.
                // Nếu là Melee -> nó tự chạy code Melee.
                // Nếu là Ranged -> nó tự chạy code Ranged.
                monster.OnCombatBehavior(playerTransform);
            }
            else if (monster.lastKnownPosition != null)
            {
                if (monster.isAlerted) monster.isAlerted = false;
                HandleSearchBehavior(monster);
            }
            else
            {
                HandleWanderBehavior(monster);
            }
        }
    }

    // Các hàm Đi tuần / Tìm kiếm giữ nguyên vì logic giống nhau
    void HandleWanderBehavior(MonsterController m)
    {
        if (m.HasReachedDestination())
        {
            m.currentWanderWaitTime += Time.deltaTime;
            if (m.currentWanderWaitTime >= m.data.wanderWaitTime)
            {
                Vector3 newPos = GetRandomPoint(m.transform.position, m.data.wanderRadius);
                m.MoveToPosition(newPos, true); 
                m.currentWanderWaitTime = 0f;
            }
            else m.StopMoving();
        }
    }
    
    Vector3 GetRandomPoint(Vector3 center, float range)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas)) return hit.position;
        return center;
    }

    void HandleSearchBehavior(MonsterController m)
    {
        m.MoveToPosition(m.lastKnownPosition.Value, true); 
        if (m.HasReachedDestination())
        {
            m.StopMoving(); 
            m.searchWaitTime += Time.deltaTime;
            if (m.searchWaitTime > 3f)
            {
                m.lastKnownPosition = null; m.isAlerted = false; m.isTracking = false; m.currentDetectionTime = 0f;
            }
        }
    }

    // Hàm hỗ trợ cho AlarmMonster gọi
    public void AlertNearbyMonsters(Vector3 alarmPosition, float radius)
    {
        foreach (var monster in allMonsters)
        {
            // Tránh việc Alarm gọi Alarm khác tạo vòng lặp vô tận (nếu muốn)
            if (monster is AlarmMonster) continue;

            if (Vector3.Distance(monster.transform.position, alarmPosition) <= radius)
            {
                monster.isAlerted = true; 
                monster.currentDetectionTime = monster.data.detectionTime;
                monster.isTracking = true; 
                monster.searchWaitTime = 0f;
                monster.StopMoving();
            }
        }
    }
}