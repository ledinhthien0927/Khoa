using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, Unity.Properties.GeneratePropertyBag]
[NodeDescription(name: "Decrease Timer", story: "Decrease [Timer] by DeltaTime", category: "Time", id: "MyGame.DecreaseTimer")]
public partial class DecreaseTimerAction : Action
{
    // Biến Timer lấy từ Blackboard
    [SerializeReference] public BlackboardVariable<float> Timer;

    protected override Status OnUpdate()
    {
        // Trừ thời gian thực
        if (Timer.Value > 0)
        {
            Timer.Value -= Time.deltaTime;
        }
        
        // Luôn trả về Success để Sequence chạy tiếp xuống node Strafe bên dưới
        return Status.Success;
    }
}