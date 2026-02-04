using System.Collections;
using UnityEngine;

public class AlarmMonster : MonsterController
{
    private bool hasCalledAlarm = false; 
    private bool isScreaming = false;         

    protected override void Start()
    {
        base.Start();
        hasCalledAlarm = false;
        isScreaming = false;
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        // Bị đánh thì ngắt hết
        if (isScreaming)
        {
            StopAllCoroutines();
            isScreaming = false;
            hasCalledAlarm = true;
            isLockMovement = false; // Mở khóa ngay để còn bị knockback
        }
        return base.TakeDamage(info);
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (isHit || isDead) return;
        if (isScreaming) return; // Đang hú thì kệ, Update cha đã khóa chân rồi

        if (hasCalledAlarm)
        {
            // --- GIAI ĐOẠN 2: ĐÁNH NHAU ---
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= data.attackRange)
            {
                StopMoving();
                RotateTowards(player.position);
                if (CanAttack())
                {
                    if (anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                        anim.SetTrigger("attack");
                }
            }
            else
            {
                MoveToPosition(player.position);
            }
        }
        else
        {
            // --- GIAI ĐOẠN 1: HÚ ---
            // Thấy là hú, không cần chạy lại gần
            if (MonsterManager.Instance.CanSeePlayer(this))
            {
                StartCoroutine(ScreamRoutine(player));
            }
            else
            {
                MoveToPosition(player.position);
            }
        }
    }

    IEnumerator ScreamRoutine(Transform player)
    {
        isScreaming = true; 
        isLockMovement = true; // <--- KHÓA CỨNG: Không cho phép di chuyển nữa
        
        // Quay mặt về player 1 lần duy nhất lúc bắt đầu
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        if (anim != null) anim.SetTrigger("callAlarm");
        
        yield return new WaitForSeconds(2.0f); // Thời gian hú

        if (MonsterManager.Instance != null)
            MonsterManager.Instance.AlertNearbyMonsters(transform.position, data.callRange);
        
        hasCalledAlarm = true; 
        isScreaming = false;
        isLockMovement = false; // <--- MỞ KHÓA: Cho phép di chuyển lại
    }
}