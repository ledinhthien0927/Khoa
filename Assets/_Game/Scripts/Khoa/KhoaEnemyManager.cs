using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance;
    
    [Header("Settings")]
    public string playerTag = "Player";
    
    public List<MonsterController> allMonsters = new List<MonsterController>();

    // Lưu trữ Transform của Player để các hàm AI truy cập nhanh
    private Transform playerTransform;

    void Awake()
    {
        // Khởi tạo Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Tối ưu hóa: Tìm Player một lần duy nhất khi bắt đầu game
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    public void RegisterMonster(MonsterController monster) => allMonsters.Add(monster);
    public void UnregisterMonster(MonsterController monster) => allMonsters.Remove(monster);

    void Update()
    {
        // Chỉ chạy AI nếu Player tồn tại trong Scene
        if (playerTransform != null)
        {
            ExecuteAI();
        }
    }

    void ExecuteAI()
    {
        // Duyệt qua danh sách quái hiện có
        for (int i = 0; i < allMonsters.Count; i++)
        {
            MonsterController monster = allMonsters[i];
            if (monster == null) continue;

            switch (monster.data.type)
            {
                case MonsterData.MonsterType.Melee:
                    HandleMeleeBehavior(monster);
                    break;
                case MonsterData.MonsterType.Ranged:
                    HandleRangedBehavior(monster);
                    break;
                case MonsterData.MonsterType.Alarm:
                    HandleAlarmBehavior(monster);
                    break;
            }
        }
    }
    void HandleMeleeBehavior(MonsterController m)
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(m.transform.position, playerTransform.position);

        // Nếu Player vào tầm đánh cận chiến (ví dụ 8m)
        if (distanceToPlayer <= 8f) 
        {
            m.MoveToPosition(playerTransform.position);
        }
    }
    
    void HandleRangedBehavior(MonsterController m)
    {
        // Logic giữ khoảng cách tại đây
        if (playerTransform == null) return;

        float distance = Vector3.Distance(m.transform.position, playerTransform.position);
        float attackRange = m.data.attackRange; // Tầm bắn tối đa (ví dụ 10m)
        float safeDistance = attackRange * 0.6f; // Khoảng cách an toàn để đứng lại (ví dụ 6m)

        if (distance > attackRange)
        {
            // 1. Nếu Player ở quá xa -> Đi tới phía Player
            m.MoveToPosition(playerTransform.position);
        }
        else if (distance < safeDistance)
        {
            // 2. Nếu Player quá gần -> Lùi lại hoặc giữ khoảng cách
            Vector3 dirToPlayer = (m.transform.position - playerTransform.position).normalized;
            Vector3 retreatPos = m.transform.position + dirToPlayer * 3f;
            m.MoveToPosition(retreatPos);
        }
        else
        {
            // 3. Nếu đang ở khoảng cách đẹp -> Đứng lại và Bắn
            m.StopMoving(); 
            m.Attack(playerTransform.position);
        }
    }

    void HandleAlarmBehavior(MonsterController m)
    {
        // Sử dụng playerTransform đã được tìm ở Awake
        float distanceToPlayer = Vector3.Distance(m.transform.position, playerTransform.position);

        if (distanceToPlayer <= m.data.detectionRange)
        {
            Debug.Log($"<color=red>{m.gameObject.name} đã phát hiện Player và đang báo động!</color>");
            AlertNearbyMonsters(m.transform.position, m.data.callRange);
        }
    }

    void AlertNearbyMonsters(Vector3 alarmPosition, float radius)
    {
        foreach (var monster in allMonsters)
        {
            // Không ra lệnh cho chính con quái đang báo động hoặc các con quái báo động khác
            if (monster.data.type == MonsterData.MonsterType.Alarm) continue;

            if (Vector3.Distance(monster.transform.position, alarmPosition) <= radius)
            {
                // Ra lệnh cho quái di chuyển tới vị trí Player (hoặc vị trí báo động)
                monster.MoveToPosition(playerTransform.position); 
            }
        }
    }
}