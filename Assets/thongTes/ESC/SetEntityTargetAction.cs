using System;
using Unity.Behavior;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Alias tránh xung đột
using Action = Unity.Behavior.Action; 

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Set ECS Target", story: "Set ECS Target to [TargetPos]", category: "Action/ECS", id: "MyGame.SetECSTarget")]
public partial class SetEntityTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> TargetPos;

    // Cache Entity ID
    private Entity _myEntity;
    private EntityManager _entityManager;
    private bool _hasEntity = false;

    protected override Status OnStart()
    {
        // 1. Lấy script Connector từ chính GameObject đang chạy Behavior này
        if (!GameObject.TryGetComponent<EnemyConnector>(out var connector))
        {
            Debug.LogError("GameObject này thiếu script EnemyConnector!");
            return Status.Failure;
        }

        // 2. Lấy Entity ID từ Connector
        _myEntity = connector.MyEntity;
        
        // Lấy EntityManager
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return Status.Failure;
        _entityManager = world.EntityManager;

        // Kiểm tra xem Entity đã được tạo chưa (do hàm Start có thể chạy lệch frame)
        if (!_entityManager.Exists(_myEntity)) return Status.Failure;

        _hasEntity = true;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!_hasEntity || !_entityManager.Exists(_myEntity)) return Status.Failure;

        // Ghi dữ liệu mục tiêu
        var targetData = new MoveTarget
        {
            Position = TargetPos.Value,
            HasTarget = true
        };

        _entityManager.SetComponentData(_myEntity, targetData);

        // Kiểm tra khoảng cách
        var currentPos = _entityManager.GetComponentData<LocalTransform>(_myEntity).Position;
        if (math.distance(currentPos, targetData.Position) < 0.5f)
        {
            return Status.Success;
        }

        return Status.Running;
    }
}