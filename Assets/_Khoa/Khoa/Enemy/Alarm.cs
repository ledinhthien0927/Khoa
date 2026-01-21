using UnityEngine;

public class AlarmMonster : MonsterController
{
    // Alarm cần biến riêng mà Melee/Ranged không cần
    private bool hasCalledAlarm = false;
    private float alarmTimer = 0f;

    // Reset biến khi bắt đầu
    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của cha (để setup máu, agent...)
        hasCalledAlarm = false;
        alarmTimer = 0f;
    }

    public override void OnCombatBehavior(Transform player)
    {
        // Giai đoạn 1: Chưa hú -> Đứng lại hú
        if (!hasCalledAlarm)
        {
            StopMoving();
            RotateTowards(player.position);
            
            alarmTimer += Time.deltaTime;
            if (alarmTimer >= data.alarmDelay)
            {
                PlayAlarmAnimation();
                Debug.Log($"<color=yellow>[Alarm] {gameObject.name} HÚ CÒI GỌI HỘI!</color>");
                
                // Gọi hàm của Manager để báo động cho anh em
                MonsterManager.Instance.AlertNearbyMonsters(transform.position, data.callRange);
                
                hasCalledAlarm = true;
            }
        }
        // Giai đoạn 2: Hú xong -> Lao vào cắn xé như Melee
        else
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= data.attackRange)
            {
                StopMoving();
                RotateTowards(player.position);
                if (CanAttack())
                {
                    if (anim != null) anim.SetTrigger("attack");
                    Debug.Log($"<color=red>[Alarm] {gameObject.name} CẮN!</color>");
                }
            }
            else
            {
                MoveToPosition(player.position, false);
            }
        }
    }
}