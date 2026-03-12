using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class AssassinMonster : MonsterController
{
    [Header("Assassin Settings")]
    public float flankDistance = 2.2f;
    public float flankOffsetAngle = 35f;
    public float flankRepathInterval = 0.25f;
    public float flankStopDistance = 0.9f;
    public float backstabRange = 1.8f;
    public float backstabAngleThreshold = 55f;
    public float attackCooldown = 2f;

    [Header("Attack Settings")]
    public float normalAttackRange = 2.2f;

    private float lastFlankRepathTime = -999f;

    private bool isFlanking = false;
    private bool hasDoneOpeningFlank = false;   // ch? flank 1 l?n lúc m?i phát hi?n player
    private Vector3 currentFlankPoint;
    private Transform lockedTarget;

    private AssassinAnimator customAnim;

    protected override void Start()
    {
        base.Start();
        customAnim = GetComponent<AssassinAnimator>();
    }

    protected override void Update()
    {
        if (targetPlayer != null)
        {
            lockedTarget = targetPlayer;
        }

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
                hasDoneOpeningFlank = false; // m?t target th? reset đ? l?n sau phát hi?n l?i s? flank l?n đ?u
                StopFlank();
            }
        }
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (player == null) return;

        if (isDead || isHit || isSearching || isReturning)
        {
            StopFlank();
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // CHƯA flank m? combat l?n đ?u -> ưu tiên v?ng ra sau lưng
        if (!hasDoneOpeningFlank)
        {
            if (CanBackstab(player))
            {
                StopFlank();
                StopMoving();
                RotateTowards(player.position);

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    hasDoneOpeningFlank = true;
                    StartCoroutine(DoAttack("attack"));
                }

                return;
            }

            // chưa ra sau lưng đư?c th? ti?p t?c flank
            HandleFlankMovement(player);
            return;
        }

        // Đ? flank xong 1 l?n -> t? nay ch? đánh tr?c di?n
        StopFlank();

        if (distToPlayer <= normalAttackRange)
        {
            StopMoving();
            RotateTowards(player.position);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(DoAttack("attack"));
            }
        }
        else
        {
            MoveToPosition(player.position, true);
            RotateTowards(player.position);
        }
    }

    private void HandleFlankMovement(Transform player)
    {
        if (Time.time >= lastFlankRepathTime + flankRepathInterval || !isFlanking)
        {
            currentFlankPoint = CalculateFlankPoint(player);
            lastFlankRepathTime = Time.time;
            isFlanking = true;
        }

        float distToFlankPoint = Vector3.Distance(transform.position, currentFlankPoint);

        if (distToFlankPoint > flankStopDistance)
        {
            MoveToPosition(currentFlankPoint, true);
            RotateTowards(currentFlankPoint);
        }
        else
        {
            MoveToPosition(player.position, true);
            RotateTowards(player.position);
        }
    }

    private Vector3 CalculateFlankPoint(Transform player)
    {
        Vector3 backDir = -player.forward;
        backDir.y = 0f;
        backDir.Normalize();

        Vector3 right = Quaternion.Euler(0f, flankOffsetAngle, 0f) * backDir;
        Vector3 left = Quaternion.Euler(0f, -flankOffsetAngle, 0f) * backDir;

        Vector3 rightPoint = player.position + right * flankDistance;
        Vector3 leftPoint = player.position + left * flankDistance;

        float rightDist = Vector3.Distance(transform.position, rightPoint);
        float leftDist = Vector3.Distance(transform.position, leftPoint);

        Vector3 chosen = rightDist < leftDist ? rightPoint : leftPoint;
        chosen.y = transform.position.y;

        return chosen;
    }

    private bool CanBackstab(Transform player)
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > backstabRange) return false;

        Vector3 toAssassin = transform.position - player.position;
        toAssassin.y = 0f;

        if (toAssassin.sqrMagnitude < 0.001f) return false;

        float angle = Vector3.Angle(player.forward, toAssassin.normalized);

        return angle >= (180f - backstabAngleThreshold);
    }

    private IEnumerator DoAttack(string triggerName)
    {
        lastAttackTime = Time.time;
        StopMoving();

        // --- SOUND: Tấn công cận chiến ---
        if (EnemySoundManager.Instance != null)
            EnemySoundManager.Instance.PlayMeleeAttack(transform.position);

        if (customAnim != null)
        {
            customAnim.PlaySlash();
            Debug.Log("Assassin attack animation triggered");
        }

        yield return new WaitForSeconds(0.9f);
    }

    private void StopFlank()
    {
        isFlanking = false;
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        StopFlank();

        // b? đánh th? coi như đ? vào combat r?i, không flank n?a
        hasDoneOpeningFlank = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (customAnim != null)
        {
            customAnim.PlayHit();
            Debug.Log("Assassin hit animation triggered");
        }
        return base.TakeDamage(info);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, normalAttackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, backstabRange);

        if (isFlanking)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentFlankPoint, 0.15f);
            Gizmos.DrawLine(transform.position, currentFlankPoint);
        }
    }
}