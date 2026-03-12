using System.Collections;
using UnityEngine;

public class AlarmMonster : MonsterController
{
    private bool hasCalledAlarm = false;
    private bool isScreaming = false;
    private bool isAttacking = false;

    [Header("Detection")]
    [Tooltip("Ra khỏi vùng này thì quái sẽ bỏ truy đuổi và reset về trạng thái chưa gặp player")]
    public float detectRange = 12f;

    [Header("Hit VFX")]
    [Tooltip("Hiệu ứng xuất hiện khi kiếm trúng player")]
    public GameObject hitPlayerFX;

    [Tooltip("Điểm gần mũi kiếm / lưỡi kiếm để spawn hiệu ứng")]
    public Transform weaponHitPoint;

    [Tooltip("Nếu không có weaponHitPoint, VFX sẽ spawn ở vị trí player + offset này")]
    public Vector3 fallbackHitOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("Tự hủy VFX sau bao lâu nếu prefab chưa có script tự hủy")]
    public float hitFXLifetime = 1.5f;

    [Header("Combat Settings")]
    [Tooltip("Thời gian từ lúc trigger animation attack tới lúc đòn thật sự chạm mục tiêu")]
    public float attackHitDelay = 0.35f;

    [Header("Weapon Trail")]
    [Tooltip("Trail Renderer gắn trên kiếm")]
    public TrailRenderer weaponTrail;

    protected override void Start()
    {
        base.Start();

        hasCalledAlarm = false;
        isScreaming = false;
        isAttacking = false;

        if (weaponTrail != null)
        {
            weaponTrail.emitting = false;
            weaponTrail.Clear();
        }
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        // Bị đánh thì ngắt quá trình hú
        if (isScreaming)
        {
            StopAllCoroutines();
            isScreaming = false;
            hasCalledAlarm = true;
            isLockMovement = false;
        }

        // Nếu đang chém thì tắt trail để tránh bị kẹt hiệu ứng
        if (isAttacking && weaponTrail != null)
        {
            weaponTrail.emitting = false;
        }

        return base.TakeDamage(info);
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (player == null) return;

        // Giữ nguyên các khóa trạng thái cũ
        if (isHit || isDead || isSearching || isReturning || isAttacking) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Nếu player ra khỏi vùng truy đuổi -> reset về trạng thái ban đầu
        if (distance > detectRange)
        {
            ResetAlarmState();
            return;
        }

        // Chưa hú thì ưu tiên hú khi nhìn thấy player
        if (!hasCalledAlarm)
        {
            if (!isScreaming && MonsterManager.Instance.CanSeePlayer(this))
            {
                StartCoroutine(ScreamRoutine(player));
            }

            return;
        }

        // Đã hú xong thì chuyển sang truy đuổi / tấn công
        if (distance <= data.attackRange)
        {
            StopMoving();
            RotateTowards(player.position);

            if (CanAttack())
            {
                StartCoroutine(ExecuteAlarmAttack(player));
            }
        }
        else
        {
            MoveToPosition(player.position);
        }
    }

    private void ResetAlarmState()
    {
        StopAllCoroutines();

        hasCalledAlarm = false;
        isScreaming = false;
        isAttacking = false;
        isLockMovement = false;

        if (weaponTrail != null)
        {
            weaponTrail.emitting = false;
            weaponTrail.Clear();
        }

        if (anim != null)
        {
            anim.ResetTrigger("callAlarm");
            anim.ResetTrigger("attack");
        }

        StopMoving();
    }

    IEnumerator ScreamRoutine(Transform player)
    {
        isScreaming = true;
        isLockMovement = true;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);

        if (anim != null) anim.SetTrigger("callAlarm");

        yield return new WaitForSeconds(data.alarmDelay);

        if (isDead) yield break;

        if (MonsterManager.Instance != null)
            MonsterManager.Instance.AlertNearbyMonsters(transform.position, data.callRange);

        hasCalledAlarm = true;
        isScreaming = false;
        isLockMovement = false;
    }

    private IEnumerator ExecuteAlarmAttack(Transform player)
    {
        isAttacking = true;

        if (anim != null) anim.SetTrigger("attack");

        if (weaponTrail != null)
        {
            weaponTrail.Clear();
            weaponTrail.emitting = true;
        }

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
                }
            }
        }

        if (weaponTrail != null)
        {
            weaponTrail.emitting = false;
        }

        isAttacking = false;
    }

    private void SpawnHitPlayerFX(Transform player, Vector3 hitDirection)
    {
        if (hitPlayerFX == null || player == null) return;

        Vector3 spawnPos;
        Quaternion spawnRot;

        Vector3 safeDir = hitDirection.sqrMagnitude > 0.0001f ? hitDirection : transform.forward;

        if (weaponHitPoint != null)
        {
            spawnPos = weaponHitPoint.position;
            spawnRot = Quaternion.LookRotation(safeDir);
        }
        else
        {
            spawnPos = player.position + fallbackHitOffset;
            spawnRot = Quaternion.LookRotation(safeDir);
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

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.callRange);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        if (weaponHitPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(weaponHitPoint.position, 0.08f);
        }
    }
}