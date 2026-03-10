using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class MeleeMonster : MonsterController
{
    [Header("Flanking AI")]
    public float rushDistance = 4f;
    public float flankOffset = 2.5f;

    [Header("Combat Settings")]
    [Tooltip("Thời gian chờ từ lúc giơ tay đến lúc đấm trúng (Ví dụ: 0.4s)")]
    public float attackHitDelay = 0.4f;

    [Header("Hit VFX")]
    [Tooltip("Hiệu ứng xuất hiện khi kiếm trúng player, ví dụ BoneChipHitFX")]
    public GameObject hitPlayerFX;

    [Tooltip("Điểm xuất phát gần lưỡi kiếm / mũi kiếm để spawn VFX")]
    public Transform weaponHitPoint;

    [Tooltip("Nếu không có weaponHitPoint, VFX sẽ spawn ở vị trí player + offset này")]
    public Vector3 fallbackHitOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("Tự hủy VFX sau bao lâu nếu prefab chưa có script tự hủy")]
    public float hitFXLifetime = 1.5f;

    private float myFlankAngle;
    private bool isAttacking = false;

    protected override void Start()
    {
        base.Start();
        float randomAngle = Random.Range(30f, 70f);
        myFlankAngle = randomAngle * (Random.value > 0.5f ? 1f : -1f);
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (isHit || isDead || isSearching || isReturning) return;

        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= data.attackRange)
        {
            StopMoving();
            RotateTowards(player.position);

            if (CanAttack())
            {
                StartCoroutine(ExecuteMeleeAttack(player));
            }
        }
        else
        {
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

    private IEnumerator ExecuteMeleeAttack(Transform player)
    {
        isAttacking = true;

        if (anim != null) anim.SetTrigger("attack");

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && !isHit && player != null)
        {
            float currentDist = Vector3.Distance(transform.position, player.position);

            if (currentDist <= data.attackRange + 0.5f)
            {
                IDamageable damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo()
                    {
                        amount = data.damage,
                        hitDirection = (player.position - transform.position).normalized,
                        knockbackForce = 0f
                    };

                    damageable.TakeDamage(info);

                    SpawnHitPlayerFX(player, info.hitDirection);

                    Debug.Log($"<color=red>[Melee] {gameObject.name} đã đánh trúng Player, trừ {info.amount} máu!</color>");
                }
            }
            else
            {
                Debug.Log($"<color=yellow>[Melee] Player đã né được đòn của {gameObject.name}!</color>");
            }
        }

        isAttacking = false;
    }

    private void SpawnHitPlayerFX(Transform player, Vector3 hitDirection)
    {
        if (hitPlayerFX == null || player == null) return;

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (weaponHitPoint != null)
        {
            spawnPos = weaponHitPoint.position;
            spawnRot = Quaternion.LookRotation(hitDirection.sqrMagnitude > 0.0001f ? hitDirection : transform.forward);
        }
        else
        {
            spawnPos = player.position + fallbackHitOffset;
            spawnRot = Quaternion.LookRotation(hitDirection.sqrMagnitude > 0.0001f ? hitDirection : transform.forward);
        }

        GameObject fx = Instantiate(hitPlayerFX, spawnPos, spawnRot);
        Destroy(fx, hitFXLifetime);
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

        if (weaponHitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(weaponHitPoint.position, 0.08f);
        }
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        isAttacking = false;
        return base.TakeDamage(info);
    }
}