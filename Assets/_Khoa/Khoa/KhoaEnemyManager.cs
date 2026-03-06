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
    
    public Transform player { get; private set; } 

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        allMonsters.Clear();
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) player = playerObj.transform;
    }

    public void RegisterMonster(MonsterController monster) => allMonsters.Add(monster);
    public void UnregisterMonster(MonsterController monster) => allMonsters.Remove(monster);

    void Update() { if (player != null) ExecuteAI(); }

    public bool CanSeePlayer(MonsterController m)
    {
        if (player == null) return false;
        
        float dist = Vector3.Distance(m.transform.position, player.position);

        float activeRange = m.data.detectionRange;
        if (m.isAlerted || m.isTracking) 
        {
            activeRange = Mathf.Max(m.data.detectionRange, m.data.attackRange) * 1.2f;
        }

        if (dist > activeRange) return false;

        Vector3 eyePos = m.transform.position + Vector3.up * 1.5f + m.transform.forward * 0.5f;
        Vector3 targetPos = player.position + Vector3.up * 1.5f;
        Vector3 dirToTarget = (targetPos - eyePos).normalized;
        float checkDist = Mathf.Max(0, dist - 0.5f);

        if (Physics.Raycast(eyePos, dirToTarget, checkDist, obstacleMask)) return false; 

        if (dist <= 3.0f) return true; 

        if (m.isAlerted || m.isTracking) return true;
        
        if (Vector3.Angle(m.transform.forward, dirToTarget) > m.data.viewAngle / 2f) return false; 

        return true; 
    }
    
    void ExecuteAI()
    {
        for (int i = 0; i < allMonsters.Count; i++)
        {
            MonsterController m = allMonsters[i];
            
            if (m == null) continue;
            if (m.isHit || m.isDead || m.isSearching || m.isReturning) continue; 

            bool canSee = CanSeePlayer(m);

            if (canSee && !m.isTracking)
            {
                if (!m.isAlerted)
                {
                    m.currentDetectionTime += Time.deltaTime;
                    if (m.currentDetectionTime >= m.data.detectionTime) m.isAlerted = true;
                }
            }
            else if (!m.isTracking && !m.isAlerted)
            {
                m.currentDetectionTime -= Time.deltaTime;
                if (m.currentDetectionTime < 0) m.currentDetectionTime = 0;
            }

            if (m.isTracking || m.isAlerted || canSee)
            {
                if (!m.isTracking) m.isTracking = true;
                
                if (canSee)
                {
                    m.lastKnownPosition = player.position;
                    m.OnCombatBehavior(player);
                }
                else if (m.lastKnownPosition.HasValue)
                {
                    m.MoveToPosition(m.lastKnownPosition.Value);
                    
                    if (m.HasReachedDestination())
                    {
                        m.isAlerted = false;
                        m.isTracking = false;
                        m.lastKnownPosition = null;
                        m.currentDetectionTime = 0f;
                    }
                }
            }
            else
            {
                m.WanderInPatrolArea();
            }
        }
    }
    
    public void AlertNearbyMonsters(Vector3 alarmPosition, float radius)
    {
        foreach (var monster in allMonsters)
        {
            if (monster is AlarmMonster) continue; 
            if (monster.isDead || monster.isReturning) continue;

            if (Vector3.Distance(monster.transform.position, alarmPosition) <= radius)
            {
                monster.isAlerted = true; 
                monster.currentDetectionTime = monster.data.detectionTime;
                monster.isTracking = true; 
                
                if (player != null)
                {
                    monster.lastKnownPosition = player.position;
                }

                Debug.Log($"<color=red>ALERT!</color> {monster.name} đã nghe thấy tiếng hú và chạy tới chi viện!");
            }
        }
    }
}