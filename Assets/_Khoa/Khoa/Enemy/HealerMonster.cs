using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HealerMonster : MonsterController
{
    [Header("Heal Settings")]
    public float healAmount = 25f;
    public float healCooldown = 4f;
    public float healCastTime = 0.8f;
    public float searchAllyRadius = 12f;

    [Range(0.1f, 1f)]
    public float healHpPercentThreshold = 0.8f;

    [Header("Throw Settings")]
    public Transform throwPoint;
    public GameObject healPotionPrefab;
    public float followPlayerDistance = 10f;

    [Header("Layer Settings")]
    public LayerMask allyLayer;

    [Header("VFX")]
    public GameObject castVFX;

    private float lastHealTime = -999f;
    private bool isHealing = false;
    private MonsterController currentHealTarget;
    private Transform lockedTarget;

    private HealerAnimator customAnim;

    protected override void Start()
    {
        base.Start();
        customAnim = GetComponent<HealerAnimator>();
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
            StopHealing();
            return;
        }

        if (isHealing) return;

        currentHealTarget = FindLowestHealthAlly();

        if (currentHealTarget != null)
        {
            float healRange = Mathf.Max(data != null ? data.attackRange : 0f, 6f);
            float distToAlly = Vector3.Distance(transform.position, currentHealTarget.transform.position);

            // Ngoài t?m heal -> ch?y l?i g?n
            if (distToAlly > healRange)
            {
                MoveToPosition(currentHealTarget.transform.position, true);
                RotateTowards(currentHealTarget.transform.position);
            }
            else
            {
                // Vào t?m heal -> đ?ng l?i và ném thu?c
                StopMoving();
                RotateTowards(currentHealTarget.transform.position);

                if (Time.time >= lastHealTime + healCooldown)
                {
                    StartCoroutine(HealRoutine());
                }
            }

            return;
        }

        // Không có ai c?n heal th? đ?ng yên/quay v? player
        StopMoving();
        RotateTowards(player.position);
    }

    private MonsterController FindLowestHealthAlly()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchAllyRadius, allyLayer);

        MonsterController lowest = null;
        float lowestPercent = 1f;

        foreach (Collider hit in hits)
        {
            MonsterController ally = hit.GetComponentInParent<MonsterController>();

            if (ally == null) continue;
            if (ally == this) continue;
            if (ally.isDead) continue;
            if (ally.data == null) continue;
            if (ally.currentHealth <= 0f) continue;

            float hpPercent = ally.currentHealth / ally.data.maxHealth;

            if (hpPercent < lowestPercent && hpPercent < healHpPercentThreshold)
            {
                lowestPercent = hpPercent;
                lowest = ally;
            }
        }

        return lowest;
    }

    private IEnumerator HealRoutine()
    {
        if (currentHealTarget == null) yield break;
        if (throwPoint == null || healPotionPrefab == null) yield break;

        isHealing = true;
        lastHealTime = Time.time;

        MonsterController healTargetAtCast = currentHealTarget;

        StopMoving();
        RotateTowards(healTargetAtCast.transform.position);

        if (customAnim != null)
        {
            customAnim.PlayHeal();
        }

        if (castVFX != null)
        {
            castVFX.SetActive(true);
        }

        yield return new WaitForSeconds(healCastTime);

        if (isDead || isHit || healTargetAtCast == null || healTargetAtCast.isDead)
        {
            StopHealing();
            yield break;
        }

        GameObject potionObj = Instantiate(healPotionPrefab, throwPoint.position, Quaternion.identity);
        HealingPotionProjectile projectile = potionObj.GetComponent<HealingPotionProjectile>();

        if (projectile != null)
        {
            projectile.SetTarget(healTargetAtCast, healAmount);
        }
        else
        {
            Debug.LogWarning("healPotionPrefab chưa g?n script HealingPotionProjectile.");
            Destroy(potionObj);
        }

        if (castVFX != null)
        {
            castVFX.SetActive(false);
        }

        isHealing = false;
    }

    private void StopHealing()
    {
        isHealing = false;

        if (castVFX != null)
        {
            castVFX.SetActive(false);
        }
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        StopHealing();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        if (customAnim != null) customAnim.PlayHit();

        return base.TakeDamage(info);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, searchAllyRadius);

        if (data != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, data.attackRange);
        }

        if (throwPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(throwPoint.position, 0.12f);
            Gizmos.DrawRay(throwPoint.position, throwPoint.forward * 1.2f);
        }
    }
}