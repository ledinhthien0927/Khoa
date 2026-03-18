using System.Collections;
using UnityEngine;

public class AssassinMonster : MonsterController
{
    [Header("Ambush Settings")]
    public float ambushTriggerDistance = 7f;
    public float warningDuration = 0.5f;
    public float ambushAttackRange = 1.8f;
    public float ambushDashSpeed = 12f;
    public float ambushRetreatDistance = 3.5f;
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
    private bool hasFinishedAmbush = false;
    private bool isWarning = false;
    private bool isAmbushDashing = false;
    private bool isRetreating = false;
    private bool hasAppliedAmbushDamage = false;
    private bool isDoingNormalAttack = false;

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

        if (isDead || isHit || isSearching || isReturning)
        {
            StopSpecialState();
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (!hasFinishedAmbush)
        {
            HandleAmbushPhase(player, distToPlayer);
            return;
        }

        HandleNormalCombat(player, distToPlayer);
    }

    private void HandleAmbushPhase(Transform player, float distToPlayer)
    {
        // Chưa bắt đầu phục kích: đứng chờ player lại gần
        if (!hasStartedAmbush)
        {
            StopMoving();
            RotateTowards(player.position);

            if (distToPlayer <= ambushTriggerDistance)
            {
                if (ambushRoutine != null)
                    StopCoroutine(ambushRoutine);

                ambushRoutine = StartCoroutine(AmbushSequence(player));
            }

            return;
        }

        // Đang phát tín hiệu
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
                StartRetreat(player);
            }

            return;
        }

        // Chạy ra sau cú chém đầu để player không đánh trả ngay
        if (isRetreating)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.speed = ambushRetreatSpeed;
                agent.SetDestination(retreatTarget);
            }

            RotateTowards(retreatTarget);

            float distToRetreat = Vector3.Distance(transform.position, retreatTarget);
            if (distToRetreat <= ambushRetreatStopDistance)
            {
                FinishAmbush();
            }

            return;
        }

        StopMoving();
    }

    private void HandleNormalCombat(Transform player, float distToPlayer)
    {
        if (isDoingNormalAttack) return;

        if (distToPlayer <= normalAttackRange)
        {
            StopMoving();
            RotateTowards(player.position);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (normalAttackRoutine != null)
                    StopCoroutine(normalAttackRoutine);

                normalAttackRoutine = StartCoroutine(DoNormalAttack());
            }
        }
        else
        {
            MoveToPosition(player.position, true);
            RotateTowards(player.position);
        }
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

        if (customAnim != null)
        {
            customAnim.PlaySlash();
            Debug.Log("Assassin ambush attack animation triggered");
        }

        PlayClip(ambushAttackClip);

        ambushRoutine = null;
    }

    private void PerformAmbushHit(Transform player)
    {
        hasAppliedAmbushDamage = true;
        lastAttackTime = Time.time;

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

    private void StartRetreat(Transform player)
    {
        isAmbushDashing = false;
        isRetreating = true;

        Vector3 awayDir = (transform.position - player.position).normalized;
        awayDir.y = 0f;

        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = -transform.forward;

        retreatTarget = transform.position + awayDir * ambushRetreatDistance;
        retreatTarget.y = transform.position.y;
    }

    private void FinishAmbush()
    {
        isWarning = false;
        isAmbushDashing = false;
        isRetreating = false;
        hasFinishedAmbush = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    private IEnumerator DoNormalAttack()
    {
        isDoingNormalAttack = true;
        lastAttackTime = Time.time;

        StopMoving();
        PlayClip(ambushAttackClip);

        if (customAnim != null)
        {
            customAnim.PlaySlash();
            Debug.Log("Assassin normal attack animation triggered");
        }

        yield return new WaitForSeconds(0.9f);

        isDoingNormalAttack = false;
        normalAttackRoutine = null;
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
    }

    private void ResetAssassinState()
    {
        StopSpecialState();

        hasStartedAmbush = false;
        hasFinishedAmbush = false;
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
        // Bị đánh trúng thì bỏ hẳn pha phục kích, vào combat thường luôn
        hasFinishedAmbush = true;
        isWarning = false;
        isAmbushDashing = false;
        isRetreating = false;

        if (ambushRoutine != null)
        {
            StopCoroutine(ambushRoutine);
            ambushRoutine = null;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();

        if (customAnim != null)
        {
            customAnim.PlayHit();
            Debug.Log("Assassin hit animation triggered");
        }

        return base.TakeDamage(info);
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