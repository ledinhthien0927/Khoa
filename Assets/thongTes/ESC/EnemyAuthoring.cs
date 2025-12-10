using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public float Speed = 5f;

    class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemyTag());
            AddComponent(entity, new MoveSpeed { Value = authoring.Speed });
            AddComponent(entity, new MoveTarget { HasTarget = false });
        }
    }
}