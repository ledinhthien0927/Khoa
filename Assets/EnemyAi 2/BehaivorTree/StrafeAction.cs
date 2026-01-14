using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Strafe Around", story: "Strafe around Player", category: "Movement", id: "MyGame.Strafe")]
public partial class StrafeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnUpdate()
    {
        var ctrl = Agent.Value.GetComponent<EnemyController>();
        if (ctrl != null)
        {
            ctrl.StrafeAround(); // Gọi logic vờn
            return Status.Running;
        }
        return Status.Failure;
    }
}