using UnityEngine;
using System.Collections; // Thêm thư viện này để dùng IEnumerator
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
    private bool isAttacking = false; // Biến khóa để quái không spam đấm liên tục lúc đang vung tay

    protected override void Start()
    {
        base.Start();
        float randomAngle = Random.Range(30f, 70f);
        myFlankAngle = randomAngle * (Random.value > 0.5f ? 1f : -1f); 
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (isHit || isDead) return; 
        
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
                // Thay vì đấm luôn, ta gọi Coroutine để xử lý nhịp điệu đòn đánh
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

        // 1. Kích hoạt Animation vung vũ khí / đấm
        if (anim != null) anim.SetTrigger("attack");

        // 2. Tạm dừng một khoảng thời gian ngắn để khớp với khoảnh khắc vũ khí chạm vào Player
        yield return new WaitForSeconds(attackHitDelay);

        // 3. Kiểm tra an toàn: Nếu trong lúc vung tay mà quái bị chém chết hoặc choáng thì hủy sát thương
        if (!isDead && !isHit)
        {
            // 4. Kiểm tra lại khoảng cách: Khoảnh khắc đấm xuống, Player còn ở đó không?
            float currentDist = Vector3.Distance(transform.position, player.position);
            
            // Cho phép sai số một chút (cộng thêm 0.5m) để Player cảm thấy hitbox công bằng
            if (currentDist <= data.attackRange + 0.5f)
            {
                // Trúng đòn! Gây sát thương cho Player
                IDamageable damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo()
                    {
                        // Lưu ý: Đổi data.damage thành tên biến sát thương đúng trong MonsterData của bạn
                        amount = data.damage, 
                        hitDirection = (player.position - transform.position).normalized,
                        knockbackForce = 0f // Đẩy nhẹ Player lùi lại
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

        // Tùy thuộc vào animation của bạn dài bao nhiêu, có thể chờ thêm 1 chút trước khi mở khóa
        // yield return new WaitForSeconds(0.5f); 
        
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
}