using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "ScriptableObjects/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("Thông tin chung")]
    public string monsterName;
    public enum MonsterType { Melee, Ranged, Alarm }
    public MonsterType type;
    public GameObject modelPrefab; 

    [Header("Chỉ số cơ bản")]
    // [SỬA] Đổi tên thành maxHealth để biết đây là máu tối đa cố định
    public float maxHealth; 
    public float speed;

    [Header("Hệ thống AI & Tầm nhìn")]
    public float detectionRange; 
    [Range(0, 360)] public float viewAngle = 90f;
    
    [Tooltip("Thời gian nhìn thấy liên tục để báo động")]
    public float detectionTime = 1.5f; 
    
    [Header("Cơ chế Báo động (Alarm)")]
    public float callRange;      
    [Tooltip("Thời gian chờ trước khi hú")]
    public float alarmDelay = 2.0f; 

    [Header("Cơ chế Đi tuần (Wander)")] 
    public float wanderRadius = 10f; 
    public float wanderWaitTime = 3f;

    [Header("Tấn công")]
    public float attackRange;
    
    [Tooltip("Lượng sát thương gây ra mỗi đòn đánh")]
    public float damage = 10f;
    
    [Header("Tốc độ đánh")]
    [Tooltip("Thời gian chờ giữa các đòn đánh (giây). Ví dụ: 2.0 nghĩa là đánh xong nghỉ 2 giây.")]
    public float attackCooldown = 2.0f; 
}