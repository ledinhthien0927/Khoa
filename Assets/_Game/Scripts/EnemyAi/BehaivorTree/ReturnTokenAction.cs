using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Return Token", story: "Return Attack Token", category: "Combat", id: "MyGame.ReturnToken")]
public partial class ReturnTokenAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        var ctrl = Agent.Value.GetComponent<EnemyController>();
        if (ctrl.Data.HasToken)
        {
            EnemyManager.Instance.ReturnAttackToken();
            ctrl.Data.HasToken = false;
        }
        return Status.Success;
    }
}