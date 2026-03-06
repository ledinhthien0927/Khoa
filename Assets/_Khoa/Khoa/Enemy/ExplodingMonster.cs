using UnityEngine;
using System.Collections;
using UnityEngine.AI; // Thêm thư viện này để dùng NavMesh.SamplePosition

public class ExplodingMonster : MonsterController
{
    [Header("Explosion Settings")]
    [Tooltip("Khoảng cách bắt đầu kích nổ (Ví dụ: 2m)")]
    public float detonationRange = 2f; 
    
    [Tooltip("Bán kính gây sát thương của vụ nổ (Ví dụ: 3m)")]
    public float explosionRadius = 3f; 
    
    [Tooltip("Sát thương gây ra cho Player")]
    public float explosionDamage = 50f;
    
    [Tooltip("Thời gian đứng rặn nổ (Để Player có 0.5s né tránh)")]
    public float explosionDelay = 0.5f; 
    
    public GameObject explosionVFX; 

    [Header("Weapons")]
    [Tooltip("Kéo vật thể trái bom vào đây để tắt cùng lúc khi nổ")]
    public GameObject bombModel; 

    [Header("Flanking AI")]
    [Tooltip("Khoảng cách quái bắt đầu bỏ bao vây để lao thẳng vào người (Ví dụ: 5m)")]
    public float rushDistance = 5f; 
    
    [Tooltip("Khoảng cách điểm đỗ ảo nằm bên hông Player (Ví dụ: 3m)")]
    public float flankOffset = 3f;

    private float myFlankAngle; // Góc tản ra ngẫu nhiên của riêng con quái này
    private bool isExploding = false;

    // --- [ĐÃ THÊM]: BỐC THĂM GÓC CHẠY LÚC SINH RA ---
    protected override void Start()
    {
        base.Start();
        
        // Random 1 góc từ 30 đến 70 độ
        float randomAngle = Random.Range(30f, 70f);
        // Bốc thăm 50/50: Chạy cánh trái hoặc cánh phải
        myFlankAngle = randomAngle * (Random.value > 0.5f ? 1f : -1f); 
    }

    // --- XỬ LÝ SÁT THƯƠNG VÀ KÍCH NỔ KHI HẾT MÁU ---
    public override HitResult TakeDamage(DamageInfo info)
    {
        // Nếu đã chết thì từ chối nhận thêm sát thương
        if (isDead) return HitResult.Ignored;
        if (isInvulnerable && info.type != DamageType.UltimateR) return HitResult.Ignored;

        currentHealth -= info.amount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        isAlerted = true; // Bị bắn là báo động luôn

        if (bloodPrefab != null)
        {
            GameObject blood = Instantiate(bloodPrefab, transform.position + Vector3.up, Quaternion.LookRotation(info.hitDirection));
            Destroy(blood, 1f);
        }

        // 1. NẾU MÁU TỤT XUỐNG 0 -> ÉP NỔ NGAY LẬP TỨC
        if (currentHealth <= 0)
        {
            StopAllCoroutines(); 
            StartCoroutine(ExplodeRoutine(true)); 
            return HitResult.Hit;
        }

        // 2. Nếu quái đang đứng "rặn nổ" mà bị bắn trúng -> Bỏ qua hiệu ứng giật lùi để nó rặn nổ tiếp
        if (isExploding) return HitResult.Hit;

        // Nếu bình thường bị bắn -> Bị choáng (Knockback)
        StopAllCoroutines(); 
        StartCoroutine(ApplyHitReaction(info));
        return HitResult.Hit;
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (isHit || isDead || isExploding) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // NẾU ĐÃ VÀO TẦM KÍCH NỔ -> BẮT ĐẦU NỔ (Có Delay)
        if (distance <= detonationRange)
        {
            StartCoroutine(ExplodeRoutine(false));
            return;
        }

        if (CheckSight() || isAlerted)
        {
            if (!isAlerted) isAlerted = true; 

            // --- [ĐÃ THÊM]: CHIẾN THUẬT GỌNG KÌM NGẪU NHIÊN ---
            if (distance > rushDistance) 
            {
                // Từ xa (> 5m): Chạy tản ra bọc sườn
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                Vector3 flankDir = Quaternion.AngleAxis(myFlankAngle, Vector3.up) * dirToPlayer;
                Vector3 targetPos = player.position - (flankDir * flankOffset);

                // Kiểm tra xem điểm ảo có kẹt tường không
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    MoveToPosition(hit.position, true); 
                }
                else
                {
                    MoveToPosition(player.position, true); // Kẹt tường thì lao thẳng
                }
            }
            else
            {
                // Lại gần (< 5m): Bỏ bao vây, lao thẳng vào mặt Player
                MoveToPosition(player.position, true); 
            }
        }
        else
        {
            StopMoving();
        }
    }

    private IEnumerator ExplodeRoutine(bool isInstant)
    {
        isExploding = true;
        isLockMovement = true; 
        StopMoving();

        // Nếu quái tự chạy lại gần rồi nổ thì diễn hoạt hình và chờ 1 chút
        if (!isInstant)
        {
            if (anim != null) anim.SetTrigger("attack"); 
            yield return new WaitForSeconds(explosionDelay);
        }

        // --- SÁT THƯƠNG VỤ NỔ ---
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo()
                    {
                        amount = explosionDamage,
                        hitDirection = (col.transform.position - transform.position).normalized,
                        knockbackForce = 15f
                    };
                    damageable.TakeDamage(info);
                }
            }
        }

        // --- HIỆU ỨNG ---
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position + Vector3.up, Quaternion.identity);
        }

        // --- QUÁI TỰ SÁT ---
        isDead = true;
        
        GetComponentInChildren<SkinnedMeshRenderer>().enabled = false; 
        GetComponent<Collider>().enabled = false;
        
        if (bombModel != null) bombModel.SetActive(false);
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        if (agent != null) agent.enabled = false;

        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
        Destroy(gameObject, 2f); 
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detonationRange); 

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius); 
        
        // Vẽ thêm vòng tròn hiển thị tầm bắt đầu lao thẳng (Rush Distance)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rushDistance);
    }
}