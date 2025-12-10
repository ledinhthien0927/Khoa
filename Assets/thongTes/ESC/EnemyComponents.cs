using Unity.Entities;
using Unity.Mathematics;

// 1. Tag để đánh dấu đây là Enemy
public struct EnemyTag : IComponentData { }

// 2. Component lưu vị trí đích đến
public struct MoveTarget : IComponentData
{
    public float3 Position;
    public bool HasTarget; // True nếu Behavior Tree đã ra lệnh di chuyển
}

// 3. Tốc độ di chuyển
public struct MoveSpeed : IComponentData
{
    public float Value;
}