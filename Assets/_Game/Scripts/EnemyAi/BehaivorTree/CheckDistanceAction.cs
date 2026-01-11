using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Check Distance Action", story: "Check if [Target] distance < [Range]", category: "Conditions", id: "MyGame.CheckDistanceAction")]
public partial class CheckDistanceAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Vector3> Target;
    [SerializeReference] public BlackboardVariable<float> Range;

    protected override Status OnStart()
    {
        if (Agent.Value == null) return Status.Failure;
        
        float dist = Vector3.Distance(Agent.Value.transform.position, Target.Value);
        
        // LOGIC QUAN TRỌNG:
        // Nếu khoảng cách LỚN HƠN Range -> Trả về Success -> Cho phép chạy tiếp các node bên dưới.
        // Nếu khoảng cách NHỎ HƠN Range -> Trả về Failure -> Selector sẽ chuyển sang nhánh tiếp theo.
        if (dist < Range.Value)
        {
            return Status.Success;
        }
        else
        {
            return Status.Failure;
        }
    }
}