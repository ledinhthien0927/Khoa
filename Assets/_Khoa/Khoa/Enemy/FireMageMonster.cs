using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class FireMageMonster : MonsterController
{
    [Header("Attack Settings")]
    public float attackCooldown = 4f;
    public float castTime = 0.8f;
    public float attackRange = 8f;

    [Header("Throw Settings")]
    public Transform throwPoint;
    public GameObject firePotionPrefab;

    [Header("Facing Settings")]
    public float faceAngleThreshold = 10f;

    [Header("VFX")]
    public GameObject castVFX;


    private bool isCasting = false;
    private Transform lockedTarget;

    private FireMageAnimator customAnim;

    protected override void Start()
    {
        base.Start();
        customAnim = GetComponent<FireMageAnimator>();
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
            customAnim.SetWalking(isMoving);
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
            }
        }
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (player == null) return;

        if (isDead || isHit || isSearching || isReturning)
        {
            StopCasting();
            return;
        }

        if (isCasting) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer > attackRange)
        {
            MoveToPosition(player.position, true);
            RotateTowards(player.position);
            return;
        }

        StopMoving();
        RotateTowards(player.position);

        if (!IsFacingTarget(player))
            return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(CastRoutine(player));
        }
    }

    private IEnumerator CastRoutine(Transform player)
    {
        if (player == null) yield break;
        if (throwPoint == null || firePotionPrefab == null) yield break;

        isCasting = true;
        lastAttackTime = Time.time;

        StopMoving();
        RotateTowards(player.position);

        if (customAnim != null)
        {
            customAnim.PlayThrowPotion();
        }

        if (castVFX != null)
        {
            castVFX.SetActive(true);
        }

        yield return new WaitForSeconds(castTime);

        if (isDead || isHit || player == null)
        {
            StopCasting();
            yield break;
        }

        GameObject potionObj = Instantiate(firePotionPrefab, throwPoint.position, Quaternion.identity);
        FirePotionProjectile projectile = potionObj.GetComponent<FirePotionProjectile>();

        if (projectile != null)
        {
            projectile.SetTarget(player, throwPoint.position);
        }
        else
        {
            Debug.LogWarning("firePotionPrefab chưa g?n script FirePotionProjectile.");
            Destroy(potionObj);
        }

        if (castVFX != null)
        {
            castVFX.SetActive(false);
        }

        isCasting = false;
    }

    private bool IsFacingTarget(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return true;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle <= faceAngleThreshold;
    }

    private void StopCasting()
    {
        isCasting = false;

        if (castVFX != null)
        {
            castVFX.SetActive(false);
        }
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        StopCasting();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (customAnim != null) customAnim.PlayHit();

        return base.TakeDamage(info);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (throwPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(throwPoint.position, 0.12f);
            Gizmos.DrawRay(throwPoint.position, throwPoint.forward * 1.2f);
        }
    }
}