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
        // Bị đánh thì ngắt hết quá trình hú
        if (isScreaming)
        {
            StopAllCoroutines();
            isScreaming = false;
            hasCalledAlarm = true;
            isLockMovement = false; // Mở khóa ngay để còn bị knockback
        }
        
        // Gọi base.TakeDamage để class cha xử lý trừ máu, văng máu, 
        // và quyết định xem nên Báo động ngay hay rơi vào trạng thái Tìm kiếm (isSearching)
        return base.TakeDamage(info);
    }

    public override void OnCombatBehavior(Transform player)
    {
        // --- [ĐÃ CẬP NHẬT] Thêm isSearching vào đây ---
        // Nếu đang bị đau, đã chết, đang lùng sục tìm kiếm, hoặc đang đứng rặn hú -> Không làm gì thêm
        if (isHit || isDead || isSearching || isScreaming || isReturning) return; 

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
        
        // Lấy trực tiếp thông số từ MonsterData
        yield return new WaitForSeconds(data.alarmDelay); 

        // [ĐÃ BỔ SUNG] Nếu lỡ quái chết trong lúc đang đứng chờ rặn hú thì thoát luôn, không cho gọi hội
        if (isDead) yield break;

        // Sau khi đợi xong thời gian delay mới bắt đầu gọi hội
        if (MonsterManager.Instance != null)
            MonsterManager.Instance.AlertNearbyMonsters(transform.position, data.callRange);
        
        hasCalledAlarm = true; 
        isScreaming = false;
        isLockMovement = false; // <--- MỞ KHÓA: Cho phép di chuyển lại
    }
}