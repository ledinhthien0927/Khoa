using UnityEngine;

public class RangedMonster : MonsterController
{
    public override void OnCombatBehavior(Transform player)
    {
        float distance = Vector3.Distance(transform.position, player.position);
        float safeDistance = data.attackRange * 0.5f;

        // 1. Xa quá -> Lại gần
        if (distance > data.attackRange)
        {
            MoveToPosition(player.position, false);
        }
        // 2. Gần quá -> Lùi lại (Kiting)
        else if (distance < safeDistance)
        {
            Vector3 dirAway = (transform.position - player.position).normalized;
            MoveToPosition(transform.position + dirAway * 5f, true);
        }
        // 3. Trong tầm bắn -> Kiểm tra tường
        else
        {
            // Gọi Manager để kiểm tra xem có bị tường che không
            if (MonsterManager.Instance.CanSeePlayer(this))
            {
                StopMoving();
                RotateTowards(player.position);

                if (CanAttack())
                {
                    if (anim != null) anim.SetTrigger("attack");
                    Debug.Log($"<color=cyan>[Ranged] {gameObject.name} BẮN!</color>");
                }
            }
            else
            {
                // Có tường -> Tiếp tục di chuyển để tìm góc bắn
                MoveToPosition(player.position, false);
            }
        }
    }
}