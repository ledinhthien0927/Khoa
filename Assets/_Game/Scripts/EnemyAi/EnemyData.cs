using UnityEngine;

[System.Serializable]
public class EnemyData
{
    [Header("Stats")]
    public float MaxHP = 100f;
    public float CurrentHP = 100f;
    public float RunSpeed = 5.0f;
    public float WalkSpeed = 2.5f;

    [Header("Combat Settings")]
    public float AggroRange = 10f;
    public float StrafingRange = 6.0f;
    public float AttackRange = 1.5f;

    [Header("State")]
    public bool HasToken = false;
    public bool IsDead = false;
    public bool IsBlocking = false; // [FIX] Thêm lại biến này
    public bool IsMoving = false;   // [FIX] Thêm lại biến này (để tương thích)
}