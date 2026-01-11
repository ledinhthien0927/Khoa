using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Check Distance Condition", story: "Distance to [Target] is less than [Range]", category: "Conditions", id: "MyGame.CheckDistanceCondition")]
public partial class CheckDistanceCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Range;

    public override bool IsTrue()
    {
        if (Agent.Value == null || Target.Value == null) return false;

        // Tính khoảng cách
        float dist = Vector3.Distance(Agent.Value.transform.position, Target.Value.transform.position);
        
        // Trả về True nếu khoảng cách nhỏ hơn Range (để dừng Repeat Until)
        return dist < Range.Value;
    }
}