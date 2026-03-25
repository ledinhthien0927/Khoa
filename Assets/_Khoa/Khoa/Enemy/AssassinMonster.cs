using System.Collections;
using UnityEngine;

public class AssassinMonster : MonsterController
{
    [Header("Ambush Settings")]
    public float ambushTriggerDistance = 7f;
    public float warningDuration = 0.5f;
    public float ambushAttackRange = 1.8f;
    public float ambushDashSpeed = 12f;
    public float ambushRetreatDistance = 8f;
    public float ambushRetreatSpeed = 10f;
    public float ambushRetreatStopDistance = 0.25f;

    [Header("Attack Settings")]
    public float normalAttackRange = 2.2f;
    public float attackCooldown = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip ambushWarningClip;
    public AudioClip ambushAttackClip;

    private Transform lockedTarget;
    private AssassinAnimator customAnim;

    private bool hasStartedAmbush = false;
    private bool isWarning = false;
    private bool isAmbushDashing = false;
    private bool isRetreating = false;
    private bool hasAppliedAmbushDamage = false;
    private bool isDoingNormalAttack = false;
    private bool isWaitingToRetreat = false;

    private Vector3 retreatTarget;
    private Coroutine ambushRoutine;
    private Coroutine normalAttackRoutine;

    protected override void Start()
    {
        base.Start();
        customAnim = GetComponent<AssassinAnimator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    protected override void Update()
    {
        if (targetPlayer != null)
            lockedTarget = targetPlayer;

        base.Update();

        if (isDead) return;

        if (customAnim != null && agent != null)
        {
            bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
            customAnim.SetRunning(isMoving);
        }

        if (lockedTarget != null)
        {
            float dist = Vector3.Distance(transform.position, lockedTarget.position);

            if (dist < 35f && (isReturning || isSearching || targetPlayer == null))
            {
                isReturning = false;
                isSearching = false;
                isAlerted = true;
                targetPlayer = lockedTarget;
                lastKnownPosition = lockedTarget.position;
            }
            else if (dist >= 35f)
            {
                lockedTarget = null;
                ResetAssassinState();
            }
        }
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (player == null) return;

        if (isDead || isSearching || isReturning)
        {
            StopSpecialState();
            return;
        }

        if (isHit) return; // Chỉ tạm dừng, không Reset special state!

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Luôn sử dụng logic Hit-and-Run
        HandleAmbushPhase(player, distToPlayer);
    }

    private void HandleAmbushPhase(Transform player, float distToPlayer)
    {
        // Phát hiện player → lập tức bắt đầu dash-attack (không chờ player lại gần)
        if (!hasStartedAmbush)
        {
            StopMoving();
            RotateTowards(player.position);

            if (ambushRoutine != null)
                StopCoroutine(ambushRoutine);

            ambushRoutine = StartCoroutine(AmbushSequence(player));
            return;
        }

        // Đang phát tín hiệu cảnh báo ngắn
        if (isWarning)
        {
            StopMoving();
            RotateTowards(player.position);
            return;
        }

        // Đang lao nhanh tới chém 1 phát
        if (isAmbushDashing)
        {
            RotateTowards(player.position);

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.speed = ambushDashSpeed;
                agent.SetDestination(player.position);
            }

            if (!hasAppliedAmbushDamage && distToPlayer <= ambushAttackRange)
            {
                PerformAmbushHit(player);
                StartCoroutine(WaitThenRetreat(player, 0.5f));
            }

            return;
        }

        // Đang tạm dừng 0.5s sau cú chém
        if (isWaitingToRetreat)
        {
            StopMoving();
            RotateTowards(player.position);
            return;
        }

        // Chạy nhanh ra xa sau cú chém đầu
        if (isRetreating)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = ambushRetreatSpeed;
                agent.SetDestination(retreatTarget);
            }

            RotateTowards(retreatTarget);

            float distToRetreat = Vector3.Distance(transform.position, retreatTarget);
            if (distToRetreat <= ambushRetreatStopDistance || (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f))
            {
                Debug.Log("Assassin retreat finished, ready for next loop");
                FinishAmbush();
            }

            return;
        }

        StopMoving();
    }

    private IEnumerator AmbushSequence(Transform player)
    {
        hasStartedAmbush = true;
        isWarning = true;
        isAmbushDashing = false;
        isRetreating = false;
        hasAppliedAmbushDamage = false;

        StopMoving();
        RotateTowards(player.position);

        PlayClip(ambushWarningClip);

        yield return new WaitForSeconds(warningDuration);

        if (isDead || player == null)
        {
            ambushRoutine = null;
            yield break;
        }

        isWarning = false;
        isAmbushDashing = true;

        ambushRoutine = null;
    }

    private void PerformAmbushHit(Transform player)
    {
        hasAppliedAmbushDamage = true;
        lastAttackTime = Time.time;

        if (customAnim != null)
        {
            customAnim.PlaySlash();
            Debug.Log("Assassin ambush attack animation triggered at range");
        }

        PlayClip(ambushAttackClip);

        IDamageable damageable = player.GetComponent<IDamageable>();
        if (damageable != null && data != null)
        {
            DamageInfo info = new DamageInfo()
            {
                amount = data.damage,
                hitDirection = (player.position - transform.position).normalized,
                knockbackForce = 0f
            };

            damageable.TakeDamage(info);
        }
    }

    private IEnumerator WaitThenRetreat(Transform player, float delay)
    {
        isWaitingToRetreat = true;
        isAmbushDashing = false;
        
        yield return new WaitForSeconds(delay);
        
        isWaitingToRetreat = false;
        if (player != null && !isDead)
        {
            StartRetreat(player);
        }
    }

    private void StartRetreat(Transform player)
    {
        isAmbushDashing = false;
        isRetreating = true;

        Vector3 awayDir = (transform.position - player.position).normalized;
        awayDir.y = 0f;

        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = -transform.forward;

        Vector3 rawTarget = transform.position + awayDir * ambushRetreatDistance;
        rawTarget.y = transform.position.y;

        // Đảm bảo điểm retreat nằm trên NavMesh
        if (UnityEngine.AI.NavMesh.SamplePosition(rawTarget, out UnityEngine.AI.NavMeshHit navHit, ambushRetreatDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            retreatTarget = navHit.position;
        }
        else
        {
            retreatTarget = rawTarget;
        }

        Debug.Log($"Assassin retreating to {retreatTarget}, distance = {ambushRetreatDistance}");

        // Bắt đầu di chuyển ngay lập tức
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = ambushRetreatSpeed;
            agent.SetDestination(retreatTarget);
        }
    }

    private void FinishAmbush()
    {
        isWarning = false;
        isAmbushDashing = false;
        isRetreating = false;
        
        // Reset để chuẩn bị cho vòng lặp tiếp theo
        hasStartedAmbush = false;
        hasAppliedAmbushDamage = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void StopSpecialState()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();

        isWarning = false;
        isAmbushDashing = false;
        isRetreating = false;
        isWaitingToRetreat = false;
    }

    private void ResetAssassinState()
    {
        StopSpecialState();

        hasStartedAmbush = false;
        hasAppliedAmbushDamage = false;
        isDoingNormalAttack = false;

        if (ambushRoutine != null)
        {
            StopCoroutine(ambushRoutine);
            ambushRoutine = null;
        }

        if (normalAttackRoutine != null)
        {
            StopCoroutine(normalAttackRoutine);
            normalAttackRoutine = null;
        }
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        if (ambushRoutine != null)
        {
            StopCoroutine(ambushRoutine);
            ambushRoutine = null;
        }

        if (normalAttackRoutine != null)
        {
            StopCoroutine(normalAttackRoutine);
            normalAttackRoutine = null;
        }

        isDoingNormalAttack = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();

        if (customAnim != null)
        {
            customAnim.PlayHit();
            Debug.Log("Assassin hit animation triggered");
        }

        // Sửa lỗi kẹt Hit state ngay tại đây (không sửa ở class cha)
        isHit = false;
        isLockMovement = false;

        HitResult result = base.TakeDamage(info);

        // Nếu bị đánh trong khi đang lùi hoặc gầm -> Phản công ngay
        if (targetPlayer != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

            if (isWarning || isAmbushDashing || isRetreating || isWaitingToRetreat)
            {
                // Dừng chờ nếu đang bị đánh
                isWaitingToRetreat = false;

                // Nếu đủ gần -> Chém ngay rồi lùi tiếp
                if (distToPlayer <= ambushAttackRange * 1.5f)
                {
                    Debug.Log("Assassin reaction: Counter-attack!");
                    if (customAnim != null) customAnim.PlaySlash();
                    PerformAmbushHit(targetPlayer);
                    StartRetreat(targetPlayer);
                }
                else 
                {
                    // Nếu ở xa -> Bỏ lùi/Gầm, lao thẳng vào dash tiếp
                    isWarning = false;
                    isRetreating = false;
                    isAmbushDashing = true; 
                    hasStartedAmbush = true; // Giữ flag để Update chạy logic Dash
                }
            }
        }

        return result;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ambushTriggerDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ambushAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, normalAttackRange);

        if (isRetreating)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(retreatTarget, 0.15f);
            Gizmos.DrawLine(transform.position, retreatTarget);
        }
    }
}