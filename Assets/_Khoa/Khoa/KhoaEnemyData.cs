using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "ScriptableObjects/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public enum MonsterType { Melee, Ranged, Alarm }
    
    public MonsterType type;
    public float health;
    public float speed;
    public float attackRange;
    public float detectionRange;
    public float callRange;
    public GameObject modelPrefab; // Prefab của quái
}