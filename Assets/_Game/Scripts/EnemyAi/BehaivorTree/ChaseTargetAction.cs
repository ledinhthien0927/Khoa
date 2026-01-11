using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Chase Target", story: "Chase Player", category: "Movement", id: "MyGame.Chase")]
public partial class ChaseTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnUpdate()
    {
        if (Agent.Value == null) return Status.Failure;
        var ctrl = Agent.Value.GetComponent<EnemyController>();
        
        if (ctrl != null)
        {
            ctrl.ChaseTarget(); // Gọi hàm trong Controller mới
            return Status.Running;
        }
        return Status.Failure;
    }
}