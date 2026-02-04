using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance;
    
    [Header("Settings")]
    public string playerTag = "Player";
    public LayerMask obstacleMask; 
    
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

    // --- LOGIC TẦM NHÌN (GIỮ NGUYÊN CỦA BẠN - VÌ ĐANG TỐT) ---
    public bool CanSeePlayer(MonsterController m)
    {
        if (playerTransform == null) return false;
        
        float dist = Vector3.Distance(m.transform.position, playerTransform.position);

        // Sticky Vision
        float activeRange = m.data.detectionRange;
        if (m.isAlerted || m.isTracking) 
        {
            activeRange = Mathf.Max(m.data.detectionRange, m.data.attackRange) * 1.2f;
        }

        if (dist > activeRange) return false;

        // Raycast
        Vector3 eyePos = m.transform.position + Vector3.up * 1.5f + m.transform.forward * 0.5f;
        Vector3 targetPos = playerTransform.position + Vector3.up * 1.5f;
        Vector3 dirToTarget = (targetPos - eyePos).normalized;
        float checkDist = Mathf.Max(0, dist - 0.5f);

        if (Physics.Raycast(eyePos, dirToTarget, checkDist, obstacleMask)) return false; 

        // FOV
        if (m.isAlerted || m.isTracking || dist < 2.0f) return true;
        if (Vector3.Angle(m.transform.forward, dirToTarget) > m.data.viewAngle / 2f) return false; 

        return true; 
    }
    
    // --- [SỬA LẠI LOGIC AI CORE] ---
    void ExecuteAI()
    {
        for (int i = 0; i < allMonsters.Count; i++)
        {
            MonsterController monster = allMonsters[i];
            
            if (monster == null) continue;
            if (monster.isHit || monster.isDead) continue; 

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

            // --- C. QUYẾT ĐỊNH HÀNH VI (ĐÃ FIX) ---
            // [FIX QUAN TRỌNG]: Điều kiện cũ của bạn là (isAlerted && canSee).
            // Điều này sai, vì Range Enemy nghe báo động (Alerted) nhưng đang ở xa chưa thấy Player (CanSee=false) -> Nó sẽ không đánh.
            // SỬA THÀNH: (isAlerted || canSee || isTracking) -> Nghe thấy là chiến luôn!
            
            if (monster.isTracking || monster.isAlerted || canSee)
            {
                // Khi đã vào mode chiến đấu, bật luôn tracking để nó bám theo dai dẳng
                if (!monster.isTracking) monster.isTracking = true;
                
                monster.OnCombatBehavior(playerTransform);
            }
            else if (monster.lastKnownPosition != null)
            {
                // Nếu mất dấu thì tìm kiếm
                if (monster.isAlerted) monster.isAlerted = false;
                HandleSearchBehavior(monster);
            }
            else
            {
                // Không có gì thì đi tuần
                HandleWanderBehavior(monster);
            }
        }
    }

    void HandleWanderBehavior(MonsterController m)
    {
        // Dùng hàm HasReachedDestination có sẵn trong MonsterController (code trước đã có)
        // Nếu bạn chưa copy code MonsterController mới thì dùng logic cũ: 
        // if (m.agent.remainingDistance <= m.agent.stoppingDistance + 0.5f)
        
        if (m.HasReachedDestination())
        {
            m.currentWanderWaitTime += Time.deltaTime;
            if (m.currentWanderWaitTime >= m.data.wanderWaitTime)
            {
                Vector3 newPos = GetRandomPoint(m.transform.position, m.data.wanderRadius);
                // False = đi tuần dùng stopping distance mặc định
                m.MoveToPosition(newPos); 
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
        // True = Ép chạy đến tận điểm nghi ngờ
        m.MoveToPosition(m.lastKnownPosition.Value); 
        
        if (m.HasReachedDestination())
        {
            m.StopMoving(); 
            m.searchWaitTime += Time.deltaTime;
            
            if (m.searchWaitTime > 3f)
            {
                m.lastKnownPosition = null; 
                m.isAlerted = false; 
                m.isTracking = false; 
                m.currentDetectionTime = 0f;
            }
        }
    }

    // --- [ĐÃ FIX] LOGIC GỌI HỘI ---
    public void AlertNearbyMonsters(Vector3 alarmPosition, float radius)
    {
        foreach (var monster in allMonsters)
        {
            if (monster is AlarmMonster) continue; // Alarm không gọi Alarm khác để tránh lặp vô tận
            if (monster.isDead) continue;

            if (Vector3.Distance(monster.transform.position, alarmPosition) <= radius)
            {
                // 1. Bật cờ Báo động
                monster.isAlerted = true; 
                monster.currentDetectionTime = monster.data.detectionTime;
                
                // 2. Bật chế độ Tracking ngay lập tức
                monster.isTracking = true; 
                monster.searchWaitTime = 0f;
                
                // 3. [FIX] Cập nhật vị trí Player cho quái biết đường mà chạy tới
                if (playerTransform != null)
                {
                    monster.lastKnownPosition = playerTransform.position;
                }

                // 4. Debug để kiểm tra
                Debug.Log($"<color=red>ALERT!</color> {monster.name} đã nghe thấy tiếng hú!");
            }
        }
    }
}