using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; 
using System.Collections;

public abstract class MonsterController : MonoBehaviour, IDamageable
{
    public MonsterData data;

    [Header("Runtime Stats")]
    public float currentHealth;
    
    // Các biến trạng thái AI
    public bool isAlerted = false;
    public bool isTracking = false;
    public bool isDead = false;          
    public bool isInvulnerable = false;  

    protected Transform targetPlayer;

    [Header("UI & Effects")]
    public Slider healthSlider;          
    public float hitStunDuration = 0.5f;
    public GameObject bloodPrefab; 

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
    public bool isLockMovement = false; 

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>(); 

        if (data != null) currentHealth = data.maxHealth;
        
        if (agent != null) 
        {
            agent.speed = (data != null) ? data.speed : 3.5f;
            agent.stoppingDistance = 0f; // Để code tự xử lý
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = (data != null) ? data.maxHealth : currentHealth;
            healthSlider.value = currentHealth;
        }
        
        startPosition = transform.position;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) targetPlayer = p.transform;

        // Đăng ký với Manager
        if (MonsterManager.Instance != null) MonsterManager.Instance.RegisterMonster(this);
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // 1. Logic Animation (Luôn cập nhật để không bị trượt chân)
        if (agent != null && anim != null) 
        {
            // Dùng velocity của NavMesh để sync animation chuẩn nhất
            anim.SetFloat("speed", agent.velocity.magnitude);
        }

        // 2. Logic Khóa hành động khi bị đánh
        if (isLockMovement || isHit)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return; 
        }

        // --- QUAN TRỌNG: Đã XÓA đoạn gọi OnCombatBehavior ở đây ---
        // Lý do: Việc gọi liên tục ở đây xung đột với MonsterManager.
        // Manager sẽ chịu trách nhiệm gọi OnCombatBehavior hoặc MoveToPosition.
    }

    // --- HÀM DI CHUYỂN (Đã sửa lỗi bỏ qua lệnh) ---
    public void MoveToPosition(Vector3 targetPos, bool ignoreStoppingDistance = false)
    {
        if (isDead || isHit || isLockMovement) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        
        // Cập nhật Stopping Distance
        float stopDist = ignoreStoppingDistance ? 0f : (data != null ? data.attackRange : 1.5f);
        agent.stoppingDistance = stopDist;

        // FIX: Gọi SetDestination trực tiếp. 
        // NavMeshAgent của Unity đã tự tối ưu rồi, không cần timer chặn ở đây gây lỗi mất lệnh.
        agent.SetDestination(targetPos);
    }

    public void StopMoving()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh) 
        { 
            agent.isStopped = true;
            agent.velocity = Vector3.zero; 
            if(agent.hasPath) agent.ResetPath();
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
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f); // Tăng tốc độ xoay lên 10 cho mượt
        }
    }

    public bool HasReachedDestination()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending)
        {
            // Tăng sai số lên một chút để tránh việc quái đứng mãi không tới đích
            if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                return true;
            }
        }
        return false;
    }

    // --- HÀM CHECK TẦM NHÌN (Đã sửa tầm nhìn) ---
    public bool CheckSight()
    {
        if (targetPlayer == null) return false;
        
        Vector3 start = transform.position + Vector3.up * 1.5f;
        Vector3 end = targetPlayer.position + Vector3.up * 1.3f;
        Vector3 dir = end - start;
        float dist = Vector3.Distance(start, end);

        // FIX: Tầm nhìn phải xa hơn tầm đánh. 
        // Nếu data có detectionRange thì dùng, không thì mặc định 15m.
        float viewDistance = 15f; 
        
        if (dist > viewDistance) return false; // Quá xa thì không cần raycast tốn performance

        if (Physics.Raycast(start, dir.normalized, out RaycastHit hit, viewDistance))
        {
            if (hit.transform == targetPlayer || hit.transform.CompareTag("Player"))
            {
                return true; 
            }
        }
        return false;
    }

    public virtual HitResult TakeDamage(DamageInfo info)
    {
        if (isDead) return HitResult.Ignored;
        if (isInvulnerable && info.type != DamageType.UltimateR) return HitResult.Ignored;

        currentHealth -= info.amount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        // Effect máu
        if (bloodPrefab != null)
        {
            GameObject blood = Instantiate(bloodPrefab, transform.position + Vector3.up, Quaternion.LookRotation(info.hitDirection));
            Destroy(blood, 1f);
        }

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
        isLockMovement = true;
        
        // Dừng ngay lập tức khi bị đánh
        if (agent != null && agent.enabled) 
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

        if (anim != null)
        {
            anim.ResetTrigger("attack"); 
            anim.SetTrigger("Hurt");
        }
        
        // Knockback (Đẩy lùi)
        if (info.knockbackForce > 0)
        {
             Rigidbody rb = GetComponent<Rigidbody>();
             // Tạm tắt NavMeshAgent để bị đẩy lùi vật lý
             if(agent != null) agent.enabled = false; 
             
             if (rb != null && !rb.isKinematic) 
             {
                rb.AddForce(info.hitDirection * info.knockbackForce, ForceMode.Impulse);
             }

             yield return new WaitForSeconds(0.2f); // Đợi vật lý tác động xong
             if(agent != null) agent.enabled = true; // Bật lại AI
        }

        yield return new WaitForSeconds(hitStunDuration);
        
        isHit = false; 
        isLockMovement = false; 
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
    }

    protected void Die(DamageInfo finalHit)
    {
        if (isDead) return;
        isDead = true;
        isLockMovement = true;
        StopMoving();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        if (anim != null) anim.SetTrigger("Die");
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        
        if (agent != null) agent.enabled = false;
        
        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
        Destroy(gameObject, 3f);
    }

    // Abstract để lớp con (MonsterMelee/Ranged) tự định nghĩa cách đánh
    public abstract void OnCombatBehavior(Transform player);

    protected bool CanAttack()
    {
        if (data == null) return false;
        if (Time.time >= lastAttackTime + data.attackCooldown)
        {
            lastAttackTime = Time.time;
            return true;
        }
        return false;
    }
    
    void OnDestroy()
    {
        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
    }
}