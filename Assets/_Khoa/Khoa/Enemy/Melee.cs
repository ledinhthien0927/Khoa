using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class MeleeMonster : MonsterController
{
    [Header("Flanking AI")]
    public float rushDistance = 4f; 
    public float flankOffset = 2.5f;
    
    [Header("Combat Settings")]
    [Tooltip("Thời gian chờ từ lúc giơ tay đến lúc đấm trúng (Ví dụ: 0.4s)")]
    public float attackHitDelay = 0.4f;

    private float myFlankAngle; 
    private bool isAttacking = false;

    protected override void Start()
    {
        base.Start();
        float randomAngle = Random.Range(30f, 70f);
        myFlankAngle = randomAngle * (Random.value > 0.5f ? 1f : -1f); 
    }

    public override void OnCombatBehavior(Transform player)
    {
        // --- [ĐÃ CẬP NHẬT] Thêm isSearching vào đây ---
        if (isHit || isDead || isSearching || isReturning) return; 
        
        // Nếu đang trong quá trình vung tay đấm thì không xử lý di chuyển hay đấm bồi nữa
        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, player.position);
        
        // 1. NẾU ĐÃ VÀO TẦM ĐÁNH -> ĐỨNG LẠI VÀ XUẤT CHIÊU
        if (distance <= data.attackRange)
        {
            StopMoving();
            RotateTowards(player.position);
            
            if (CanAttack())
            {
                StartCoroutine(ExecuteMeleeAttack(player));
            }
        }
        else
        {
            // 2. CHƯA TỚI TẦM ĐÁNH -> ÁP DỤNG CHIẾN THUẬT GỌNG KÌM
            if (distance > rushDistance) 
            {
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                Vector3 flankDir = Quaternion.AngleAxis(myFlankAngle, Vector3.up) * dirToPlayer;
                Vector3 targetPos = player.position - (flankDir * flankOffset);

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    MoveToPosition(hit.position, false); 
                }
                else
                {
                    MoveToPosition(player.position, false); 
                }
            }
            else
            {
                MoveToPosition(player.position, false); 
            }
        }
    }

    // --- LUỒNG XỬ LÝ ĐÒN ĐẤM ---
    private IEnumerator ExecuteMeleeAttack(Transform player)
    {
        isAttacking = true;

        if (anim != null) anim.SetTrigger("attack");

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && !isHit)
        {
            float currentDist = Vector3.Distance(transform.position, player.position);
            
            if (currentDist <= data.attackRange + 0.5f)
            {
                IDamageable damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo()
                    {
                        amount = data.damage, 
                        hitDirection = (player.position - transform.position).normalized,
                        knockbackForce = 0f 
                    };
                    damageable.TakeDamage(info);
                    Debug.Log($"<color=red>[Melee] {gameObject.name} đã đấm trúng Player, trừ {info.amount} máu!</color>");
                }
            }
            else
            {
                Debug.Log($"<color=yellow>[Melee] Player đã lướt né được đòn của {gameObject.name}!</color>");
            }
        }

        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.attackRange); 
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rushDistance);
    }
    // --- BỔ SUNG: NGẮT ĐÒN VÀ CHỐNG ĐƠ AI ---
    public override HitResult TakeDamage(DamageInfo info)
    {
        // Mở khóa AI ngay lập tức. 
        // Lỡ Coroutine ExecuteMeleeAttack bị ngắt giữa chừng thì quái vẫn không bị kẹt.
        isAttacking = false; 

        // Gọi logic trừ máu, văng máu, giật mình của lớp cha như bình thường
        return base.TakeDamage(info);
    }
}