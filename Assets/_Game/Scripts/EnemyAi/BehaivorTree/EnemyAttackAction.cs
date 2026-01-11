using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Enemy Attack", story: "[Agent] attacks", category: "Combat", id: "MyGame.EnemyAttack")]
public partial class EnemyAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent.Value == null) return Status.Failure;
        var ctrl = Agent.Value.GetComponent<EnemyController>();
        
        if (ctrl != null)
        {
            ctrl.PerformAttack(); // Gọi hàm mới trong Controller
            return Status.Success;
        }
        return Status.Failure;
    }
}