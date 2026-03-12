using UnityEngine;
using System.Collections;

public class ShieldMonster : MonsterController
{
    [Header("Shield Settings")]
    public float blockDuration = 2f;
    public float blockAngle = 120f;

    [Tooltip("Thời gian chờ để chạy xong hoạt ảnh hạ khiên trước khi được phép đi tiếp")]
    public float shieldLowerDelay = 0.5f;

    [Header("Hit VFX")]
    [Tooltip("Hiệu ứng xuất hiện khi kiếm trúng player, dùng cùng prefab như MeleeMonster nếu muốn y chang")]
    public GameObject hitPlayerFX;

    [Tooltip("Điểm gần mũi kiếm / lưỡi kiếm để spawn hiệu ứng")]
    public Transform weaponHitPoint;

    [Tooltip("Nếu prefab chưa có script tự hủy thì tự hủy sau từng này giây")]
    public float hitFXLifetime = 1.5f;

    [Tooltip("Thời gian từ lúc trigger animation attack tới lúc đòn thật sự chạm mục tiêu")]
    public float attackHitDelay = 0.35f;

    [Header("Weapon Trail")]
    [Tooltip("Trail Renderer gắn trên kiếm")]
    public TrailRenderer weaponTrail;

    private bool isBlocking = false;
    private bool isLoweringShield = false;
    private float blockTimer = 0f;

    private bool isAttacking = false;

    protected override void Start()
    {
        base.Start();

        if (weaponTrail != null)
        {
            weaponTrail.emitting = false;
            weaponTrail.Clear();
        }
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        if (isDead || isReturning) return HitResult.Ignored;

        float hitAngle = Vector3.Angle(transform.forward, -info.hitDirection);
        bool isFrontalHit = hitAngle <= blockAngle;

        // 1. ĐANG THỦ HOẶC ĐANG CẤT KHIÊN MÀ BỊ ĐÁNH TỪ TRƯỚC MẶT -> Đỡ đòn thành công
        if ((isBlocking || isLoweringShield) && isFrontalHit)
        {
            blockTimer = blockDuration;
            isBlocking = true;
            isLoweringShield = false;

            if (anim != null)
            {
                anim.SetBool("isBlocking", true);
                anim.SetTrigger("BlockHit");
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(info.hitDirection * (info.knockbackForce * 0.5f), ForceMode.Impulse);
            }

            return HitResult.Ignored;
        }

        // 2. KHÔNG THỦ HOẶC BỊ ĐÁNH LÉN TỪ SAU LƯNG -> Nhận sát thương thực sự
        HitResult result = base.TakeDamage(info);

        // Reset trạng thái khiên để tránh xung đột animation
        if (isBlocking || isLoweringShield)
        {
            isBlocking = false;
            isLoweringShield = false;
            if (anim != null) anim.SetBool("isBlocking", false);
        }

        if (isAttacking && weaponTrail != null)
        {
            weaponTrail.emitting = false;
        }

        // 3. BỊ ĐÁNH TỪ ĐẰNG TRƯỚC + CÒN SỐNG + KHÔNG SEARCHING -> BẬT KHIÊN
        // Bỏ phụ thuộc isAlerted để block ổn định hơn
        if (isFrontalHit && currentHealth > 0 && !isSearching)
        {
            isBlocking = true;
            isLoweringShield = false;
            blockTimer = blockDuration;

            if (anim != null) anim.SetBool("isBlocking", true);
        }

        return result;
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;

        // Chỉ đếm ngược thời gian thủ nếu KHÔNG TRONG LÚC đang hạ khiên
        if (isBlocking && !isLoweringShield)
        {
            blockTimer -= Time.deltaTime;

            if (blockTimer <= 0f)
            {
                // Đặt cờ trước để tránh StartCoroutine bị gọi lặp mỗi frame
                isLoweringShield = true;
                StartCoroutine(LowerShieldRoutine());
            }
        }
    }

    private IEnumerator LowerShieldRoutine()
    {
        if (anim != null) anim.SetBool("isBlocking", false);

        yield return new WaitForSeconds(shieldLowerDelay);

        // Nếu trong lúc đang hạ khiên mà bị đánh tiếp, TakeDamage sẽ set lại state
        if (!isLoweringShield) yield break;

        isBlocking = false;
        isLoweringShield = false;
        Debug.Log($"<color=cyan>[SHIELD DOWN]</color> {gameObject.name} đã hạ khiên XONG và bắt đầu di chuyển.");
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (player == null) return;

        // Bị khóa chân nếu ĐANG THỦ hoặc ĐANG TRONG LÚC CẤT KHIÊN
        if (isBlocking || isLoweringShield)
        {
            StopMoving();
            RotateTowards(player.position);
            return;
        }

        if (isAttacking) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (data != null && dist <= data.attackRange)
        {
            StopMoving();
            RotateTowards(player.position);

            if (CanAttack())
            {
                StartCoroutine(ExecuteShieldAttack(player));
            }
        }
        else
        {
            MoveToPosition(player.position, false);
        }
    }

    private IEnumerator ExecuteShieldAttack(Transform player)
    {
        isAttacking = true;

        if (anim != null) anim.SetTrigger("attack");

        if (weaponTrail != null)
        {
            weaponTrail.Clear();
            weaponTrail.emitting = true;
        }

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && player != null)
        {
            float currentDist = Vector3.Distance(transform.position, player.position);

            if (data != null && currentDist <= data.attackRange + 0.5f)
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
            spawnPos = player.position + Vector3.up * 1.2f;
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
        }

        if (weaponHitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(weaponHitPoint.position, 0.08f);
        }
    }
}