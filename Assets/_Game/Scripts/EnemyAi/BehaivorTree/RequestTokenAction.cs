using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Request Token", story: "Try get Attack Token", category: "Combat", id: "MyGame.RequestToken")]
public partial class RequestTokenAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        var ctrl = Agent.Value.GetComponent<EnemyController>();
        
        // Gọi thẳng EnemyManager, không qua CombatTokenManager cũ nữa
        if (EnemyManager.Instance.RequestAttackToken())
        {
            ctrl.Data.HasToken = true;
            return Status.Success; // XIN ĐƯỢC -> ĐÁNH
        }
        return Status.Failure; // KHÔNG ĐƯỢC -> VỀ ĐI VỜN TIẾP
    }
}