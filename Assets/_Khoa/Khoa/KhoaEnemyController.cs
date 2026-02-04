using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; 
using System.Collections;
// Đã xóa namespace _Khoa.Khoa để khớp với project nhóm

public abstract class MonsterController : MonoBehaviour, IDamageable
{
    public MonsterData data;

    [Header("Runtime Stats")]
    public float currentHealth;
    public bool isAlerted = false;
    public bool isTracking = false;
    public bool isDead = false;          
    public bool isInvulnerable = false;  

    [Header("UI & Effects")]
    public Slider healthSlider;          
    public float hitStunDuration = 0.5f;

    [Header("Base Logic")]
    public float currentDetectionTime = 0f;
    public Vector3? lastKnownPosition = null;
    public float searchWaitTime = 0f;
    public Vector3 startPosition;
    public float currentWanderWaitTime = 0f;

    protected NavMeshAgent agent;
    protected Animator anim;
    protected float lastAttackTime = -999f;
    
    public bool isHit = false; 
    
    // --- [MỚI] BIẾN KHÓA CHỐNG TRƯỢT ---
    public bool isLockMovement = false; 

    // [FIX GIẬT] Biến này giúp giảm tải cho NavMesh
    private float pathUpdateTimer = 0f; 

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>(); 

        if (data != null) currentHealth = data.maxHealth;
        
        // [QUAN TRỌNG] Trả về 0 để code tự xử lý
        if (agent != null) 
        {
            agent.speed = data.speed;
            agent.stoppingDistance = 0f; 
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = (data != null) ? data.maxHealth : currentHealth;
            healthSlider.value = currentHealth;
        }
        
        startPosition = transform.position;
        if (MonsterManager.Instance != null) MonsterManager.Instance.RegisterMonster(this);
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // --- [LOGIC CHỐNG TRƯỢT + SAFE CHECK] ---
        // Nếu đang bị khóa (Hú, Chết, Choáng) -> Ép đứng im tuyệt đối
        if (isLockMovement || isHit)
        {
            // Thêm check isOnNavMesh để tránh lỗi đỏ khi quái chưa chạm đất
            if (agent != null && agent.enabled && agent.isOnNavMesh) 
            {
                agent.velocity = Vector3.zero; // Triệt tiêu quán tính ngay lập tức
                agent.isStopped = true;
            }
            // Ngắt animation di chuyển
            if (anim != null) anim.SetFloat("speed", 0f); 
            return; 
        }
        // ---------------------------------------

        // Đồng bộ Animation chạy bình thường
        if (agent != null && anim != null) 
        {
            anim.SetFloat("speed", agent.velocity.magnitude);
        }
    }

    // --- HÀM DI CHUYỂN AN TOÀN ---
    public void MoveToPosition(Vector3 targetPos, bool ignoreStoppingDistance = false)
    {
        // Nếu đang bị khóa thì không nhận lệnh di chuyển
        if (isDead || isHit || isLockMovement) return;

        // [SAFE CHECK] Chỉ chạy khi Agent đã nằm trên NavMesh (Đã chạm đất xanh)
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (agent.isStopped) agent.isStopped = false;

        // Xử lý Stopping Distance
        agent.stoppingDistance = ignoreStoppingDistance ? 0f : data.attackRange;

        // [FIX GIẬT] Giảm tần suất gọi SetDestination
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= 0.2f)
        {
            agent.SetDestination(targetPos);
            pathUpdateTimer = 0f;
        }
    }

    public void StopMoving()
    {
        // Thêm Safe Check isOnNavMesh
        if (agent != null && agent.enabled && agent.isOnNavMesh) 
        { 
            if (agent.hasPath) agent.ResetPath();
            agent.velocity = Vector3.zero; 
            agent.isStopped = true;
        }
    }

    public void RotateTowards(Vector3 target)
    {
        if (isDead || isHit) return; 
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }

    // --- XỬ LÝ DAMAGE ---
    public virtual HitResult TakeDamage(DamageInfo info)
    {
        if (isDead) return HitResult.Ignored;
        if (isInvulnerable && info.type != DamageType.UltimateR) return HitResult.Ignored;

        currentHealth -= info.amount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        if (currentHealth <= 0)
        {
            Die(info);
            return HitResult.Hit;
        }

        StopAllCoroutines(); 
        StartCoroutine(ApplyHitReaction(info));

        return HitResult.Hit;
    }

    protected IEnumerator ApplyHitReaction(DamageInfo info)
    {
        isHit = true; 
        isLockMovement = true; // Khóa di chuyển
        
        StopMoving();

        if (anim != null)
        {
            anim.ResetTrigger("attack"); 
            anim.ResetTrigger("Hurt");
            anim.ResetTrigger("Knockback");
            
            if (info.type == DamageType.Heavy || info.type == DamageType.EarthUp)
                anim.SetTrigger("Knockback");
            else
                anim.SetTrigger("Hurt");
        }

        ApplyKnockbackPhysics(info); 

        yield return new WaitForSeconds(hitStunDuration);
        
        isHit = false; 
        isLockMovement = false; // Mở khóa lại
        
        // Safe Check trước khi mở lại Agent
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
    }

    private void ApplyKnockbackPhysics(DamageInfo info)
    {
        if (info.knockbackForce > 0)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(info.hitDirection * info.knockbackForce, ForceMode.Impulse);
            }
        }
    }

    protected void Die(DamageInfo finalHit)
    {
        if (isDead) return;
        isDead = true;
        isHit = true; 
        isLockMovement = true; // Khóa chết cứng

        StopMoving();
        if (anim != null) anim.SetTrigger("Die");
        
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        if (agent != null) agent.enabled = false;
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
        Destroy(gameObject, 3f);
    }

    public abstract void OnCombatBehavior(Transform player);

    protected bool CanAttack()
    {
        if (Time.time >= lastAttackTime + data.attackCooldown)
        {
            lastAttackTime = Time.time;
            return true;
        }
        return false;
    }

    // Hàm tiện ích check đến nơi chưa (Safe Check)
    public bool HasReachedDestination()
    {
        // Phải check isOnNavMesh trước khi check pathPending
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                return (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
        }
        return false;
    }

    void OnDestroy()
    {
        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
    }
}