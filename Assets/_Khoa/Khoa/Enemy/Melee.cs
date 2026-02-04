using UnityEngine;

public class MeleeMonster : MonsterController
{
    // Viết lại logic chiến đấu (Override)
    public override void OnCombatBehavior(Transform player)
    {
        // --- SỬA LỖI TẠI ĐÂY ---
        // Nếu đang bị đánh (isHit = true) thì thoát ngay, không được đánh trả!
        if (isHit) return; 
        // -----------------------

        float distance = Vector3.Distance(transform.position, player.position);
        
        // Logic Melee đơn giản: Gần thì đánh, Xa thì chạy lại
        if (distance <= data.attackRange)
        {
            StopMoving();
            RotateTowards(player.position);
            
            if (CanAttack())
            {
                if (anim != null) anim.SetTrigger("attack");
                Debug.Log($"<color=red>[Melee] {gameObject.name} ĐẤM!</color>");
            }
        }
        else
        {
            MoveToPosition(player.position, false);
        }
    }
}