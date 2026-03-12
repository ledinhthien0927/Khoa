using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public abstract class MonsterController : MonoBehaviour, IDamageable
{
    public MonsterData data;

    [Header("Runtime Stats")]
    public float currentHealth;

    // [ĐÃ SỬA] Các biến tùy chỉnh Hồi máu theo nhịp
    [Header("Health Regeneration")]
    public float regenAmount = 5f;     // Lượng máu hồi mỗi lần
    public float regenInterval = 2f;   // Thời gian chờ giữa mỗi lần hồi (giây)
    private float regenTimer = 0f;     // Bộ đếm thời gian ẩn

    // Các biến trạng thái AI
    public bool isAlerted = false;
    public bool isTracking = false;
    public bool isDead = false;
    public bool isInvulnerable = false;
    public bool isSearching = false;
    public bool isReturning = false;

    protected Transform targetPlayer;

    [Header("UI & Effects")]
    public Slider healthSlider;
    public float hitStunDuration = 0.5f;
    public GameObject bloodPrefab;

    [Header("Search Behavior (Stealth)")]
    public float searchDuration = 5f;
    public float searchRadius = 6f;
    public float searchVisionMultiplier = 1.5f;

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
            agent.stoppingDistance = 0f;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = (data != null) ? data.maxHealth : currentHealth;
            healthSlider.value = currentHealth;
        }

        startPosition = transform.position;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) targetPlayer = p.transform;

        if (MonsterManager.Instance != null) MonsterManager.Instance.RegisterMonster(this);
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (agent != null && anim != null)
        {
            anim.SetFloat("speed", agent.velocity.magnitude);
        }

        if (isLockMovement || isHit)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        // --- [CẬP NHẬT NAVMESH LEASH] ---
        if (!isReturning && (isAlerted || isSearching || isTracking))
        {
            if (agent != null && !agent.pathPending && agent.hasPath)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                    {
                        StopAllCoroutines();
                        StartCoroutine(ReturnToTerritory());
                    }
                }
            }
        }

        // --- [ĐÃ SỬA] LOGIC HỒI MÁU THEO NHỊP (TICK RATE) ---
        // Quái chỉ tự bơm máu nếu KHÔNG lùng sục, KHÔNG báo động và KHÔNG truy đuổi
        if (!isAlerted && !isSearching && !isTracking)
        {
            float maxHP = (data != null) ? data.maxHealth : 100f;

            if (currentHealth < maxHP)
            {
                // Bắt đầu đếm thời gian
                regenTimer += Time.deltaTime;

                // Khi thời gian đếm đủ mức Interval (VD: 2 giây)
                if (regenTimer >= regenInterval)
                {
                    currentHealth += regenAmount; // Cộng lượng máu quy định (VD: 5 máu)

                    // Khóa lại ở mức Max HP, không cho hồi lố
                    if (currentHealth > maxHP)
                    {
                        currentHealth = maxHP;
                    }

                    // Cập nhật thanh máu trên đầu
                    if (healthSlider != null)
                    {
                        healthSlider.value = currentHealth;
                    }

                    // Reset bộ đếm về 0 để đếm lại cho nhịp tiếp theo
                    regenTimer = 0f;
                }
            }
            else
            {
                // Nếu máu đã đầy thì reset bộ đếm để không bị lưu nhịp thừa
                regenTimer = 0f;
            }
        }
        else
        {
            // Nếu đang trong trạng thái chiến đấu/báo động thì cũng reset bộ đếm
            regenTimer = 0f;
        }
    }

    protected IEnumerator ReturnToTerritory()
    {
        isReturning = true;
        isAlerted = false;
        isSearching = false;
        isTracking = false;

        lastKnownPosition = null;
        currentDetectionTime = 0f;

        if (agent != null) agent.speed = (data != null ? data.speed : 3.5f) * 1.5f;

        Debug.Log($"<color=orange>[NavMesh Leash] {gameObject.name} chạm ranh giới, từ bỏ mục tiêu!</color>");

        while (Vector3.Distance(transform.position, startPosition) > 1.5f)
        {
            if (isDead) yield break;

            MoveToPosition(startPosition, true);
            yield return new WaitForSeconds(0.5f);
        }

        StopMoving();
        isReturning = false;

        if (agent != null) agent.speed = data != null ? data.speed : 3.5f;
    }

    public void MoveToPosition(Vector3 targetPos, bool ignoreStoppingDistance = false)
    {
        if (isDead || isHit || isLockMovement) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        float stopDist = ignoreStoppingDistance ? 0f : (data != null ? data.attackRange : 1.5f);
        agent.stoppingDistance = stopDist;
        agent.SetDestination(targetPos);
    }

    public void StopMoving()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (agent.hasPath) agent.ResetPath();
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
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }
    }

    public bool HasReachedDestination()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.2f) return true;
        }
        return false;
    }

    public bool CheckSight()
    {
        if (targetPlayer == null) return false;

        Vector3 start = transform.position + Vector3.up * 1.5f;
        Vector3 end = targetPlayer.position + Vector3.up * 1.3f;
        Vector3 dir = end - start;
        float dist = Vector3.Distance(start, end);

        float viewDistance = (data != null) ? data.detectionRange : 15f;

        if (dist > viewDistance) return false;

        if (Physics.Raycast(start, dir.normalized, out RaycastHit hit, viewDistance))
        {
            if (hit.transform == targetPlayer || hit.transform.CompareTag("Player")) return true;
        }
        return false;
    }

    public virtual HitResult TakeDamage(DamageInfo info)
    {
        if (isDead || isReturning) return HitResult.Ignored;
        if (isInvulnerable && info.type != DamageType.UltimateR) return HitResult.Ignored;

        currentHealth -= info.amount;
        if (healthSlider != null) healthSlider.value = currentHealth;

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

        if (CheckSight())
        {
            isAlerted = true;
            isSearching = false;
        }
        else
        {
            StartCoroutine(SearchRoutine());
        }

        return HitResult.Hit;
    }

    protected IEnumerator ApplyHitReaction(DamageInfo info)
    {
        isHit = true;
        isLockMovement = true;

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

        if (info.knockbackForce > 0)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (agent != null) agent.enabled = false;

            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(info.hitDirection * info.knockbackForce, ForceMode.Impulse);
            }

            yield return new WaitForSeconds(0.2f);
            if (agent != null) agent.enabled = true;
        }

        yield return new WaitForSeconds(hitStunDuration);

        isHit = false;
        isLockMovement = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
    }

    protected IEnumerator SearchRoutine()
    {
        isSearching = true;

        yield return new WaitForSeconds(hitStunDuration + 0.2f);

        float timer = 0f;
        float baseVision = (data != null) ? data.detectionRange : 15f;
        float boostedVision = baseVision * searchVisionMultiplier;

        while (timer < searchDuration)
        {
            if (isDead) yield break;

            if (targetPlayer != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

                if (distToPlayer <= boostedVision)
                {
                    Vector3 start = transform.position + Vector3.up * 1.5f;
                    Vector3 end = targetPlayer.position + Vector3.up * 1.3f;
                    Vector3 dir = (end - start).normalized;

                    if (Physics.Raycast(start, dir, out RaycastHit hit, boostedVision))
                    {
                        if (hit.transform == targetPlayer || hit.transform.CompareTag("Player"))
                        {
                            isAlerted = true;
                            isSearching = false;
                            yield break;
                        }
                    }
                }
            }

            if (!isLockMovement && agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                {
                    Vector3 randomSpot = transform.position + Random.insideUnitSphere * searchRadius;
                    if (NavMesh.SamplePosition(randomSpot, out NavMeshHit navHit, searchRadius, NavMesh.AllAreas))
                    {
                        MoveToPosition(navHit.position, true);
                    }
                }
            }

            timer += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        isSearching = false;
        StopMoving();
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

    public void WanderInPatrolArea()
    {
        if (isDead || isHit || isLockMovement || isReturning) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            if (currentWanderWaitTime > 0)
            {
                currentWanderWaitTime -= Time.deltaTime;
                return;
            }

            float wRadius = (data != null) ? data.wanderRadius : 10f;
            Vector3 randomDir = Random.insideUnitSphere * wRadius;
            randomDir += startPosition;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wRadius, NavMesh.AllAreas))
            {
                MoveToPosition(hit.position, true);
                currentWanderWaitTime = (data != null) ? data.wanderWaitTime : 3f;
            }
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 drawPos = Application.isPlaying ? startPosition : transform.position;

        if (data != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(drawPos, data.wanderRadius);
        }
    }

    void OnDestroy()
    {
        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
    }
}