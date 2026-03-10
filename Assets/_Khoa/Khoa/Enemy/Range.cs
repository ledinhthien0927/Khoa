using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class RangedMonster : MonsterController
{
    [Header("Ranged Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float aimDuration = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("Tên trigger tấn công. Quái thường điền 'attack', Sniper điền 'Fire'")]
    public string attackAnimTrigger = "attack";

    [Tooltip("Thời gian đứng yên sau khi bắn để diễn animation giật súng")]
    public float fireAnimationTime = 0.5f;

    [Header("Kiting Settings")]
    [Tooltip("Đứng khựng lại bao lâu khi player áp sát rồi mới lùi")]
    public float retreatDelay = 1f;

    [Tooltip("Khoảng cách lùi ra khi bị áp sát")]
    public float retreatDistance = 3f;

    [Tooltip("Ngưỡng quá gần = attackRange * retreatThreshold")]
    [Range(0.1f, 1f)]
    public float retreatThreshold = 0.4f;

    [Header("VFX Settings")]
    [Tooltip("Hiệu ứng đầu nòng kiểu súng cổ: flash + smoke + sparks")]
    public GameObject muzzleFXPrefab;

    [Tooltip("Tự hủy muzzle FX sau từng này giây nếu prefab chưa có script tự hủy")]
    public float muzzleFXLifetime = 1.5f;

    private bool isAiming = false;
    private bool isFiring = false;
    private bool isRetreating = false;

    private float aimTimer = 0f;
    private float retreatTimer = 0f;

    private Vector3 retreatTarget;
    private LineRenderer laserLine;

    protected override void Start()
    {
        base.Start();

        laserLine = GetComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = 0.05f;
        laserLine.endWidth = 0.05f;

        if (laserLine.material == null)
            laserLine.material = new Material(Shader.Find("Sprites/Default"));

        laserLine.startColor = Color.red;
        laserLine.endColor = new Color(1f, 0f, 0f, 0f);
        laserLine.enabled = false;
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (player == null) return;

        // Nếu bị đánh, chết, đang search hoặc return thì hủy các trạng thái chiến đấu hiện tại
        if (isHit || isDead || isSearching || isReturning)
        {
            StopAiming();
            retreatTimer = 0f;
            isRetreating = false;
            return;
        }

        // Nếu đang trong animation bắn thì khóa AI
        if (isFiring) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = MonsterManager.Instance.CanSeePlayer(this);

        // Nếu đang aim thì ưu tiên aim cho xong
        if (isAiming)
        {
            HandleAiming(player);
            return;
        }

        // Nếu đang lùi thì tiếp tục lùi cho xong, tránh bị StopMoving() cắt giữa chừng
        if (isRetreating)
        {
            MoveToPosition(retreatTarget, true);

            RotateTowards(player.position);

            if (Vector3.Distance(transform.position, retreatTarget) <= 0.3f)
            {
                isRetreating = false;
                retreatTimer = 0f;
                StopMoving();
            }

            return;
        }

        // Xa quá -> chạy lại gần
        if (distance > data.attackRange)
        {
            retreatTimer = 0f;
            MoveToPosition(player.position, true);
            return;
        }

        // Gần quá -> đứng khựng lại một lúc rồi mới lùi
        if (distance < data.attackRange * retreatThreshold)
        {
            retreatTimer += Time.deltaTime;

            StopMoving();
            RotateTowards(player.position);

            // Nếu muốn nó vẫn có thể bắn khi bị dí sát thì giữ phần này
            if (canSeePlayer && CanAttack())
            {
                StartAiming();
                return;
            }

            if (retreatTimer >= retreatDelay)
            {
                Vector3 dir = (transform.position - player.position).normalized;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = -transform.forward;

                retreatTarget = transform.position + dir * retreatDistance;
                isRetreating = true;
                retreatTimer = 0f;
            }

            return;
        }

        // Vừa tầm -> đứng lại, thấy player thì aim bắn
        retreatTimer = 0f;

        if (canSeePlayer)
        {
            StopMoving();
            RotateTowards(player.position);

            if (CanAttack())
                StartAiming();
        }
        else
        {
            // Bị che tầm nhìn -> tìm góc bắn
            MoveToPosition(player.position, true);
        }
    }

    void HandleAiming(Transform player)
    {
        if (player == null)
        {
            StopAiming();
            return;
        }

        StopMoving();
        RotateTowards(player.position);

        if (firePoint != null)
        {
            laserLine.SetPosition(0, firePoint.position);
            laserLine.SetPosition(1, player.position + Vector3.up);
        }

        if (!MonsterManager.Instance.CanSeePlayer(this))
        {
            StopAiming();
            return;
        }

        aimTimer += Time.deltaTime;

        if (aimTimer >= aimDuration && !isFiring)
        {
            StartCoroutine(FireRoutine(player.position + Vector3.up));
        }
    }

    void StartAiming()
    {
        isAiming = true;
        aimTimer = 0f;

        if (laserLine != null)
            laserLine.enabled = true;

        if (anim != null)
            anim.SetBool("isAiming", true);
    }

    void StopAiming()
    {
        isAiming = false;
        aimTimer = 0f;

        if (laserLine != null)
            laserLine.enabled = false;

        if (anim != null)
            anim.SetBool("isAiming", false);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    IEnumerator FireRoutine(Vector3 targetPos)
    {
        isFiring = true;

        if (anim != null)
        {
            anim.SetBool("isAiming", false);
            anim.SetTrigger(attackAnimTrigger);
        }

        // Hiệu ứng đầu nòng kiểu súng cổ
        SpawnMuzzleFX();

        // Spawn đạn
        if (bulletPrefab != null && firePoint != null)
        {
            Vector3 dir = (targetPos - firePoint.position).normalized;
            if (dir.sqrMagnitude < 0.0001f)
                dir = firePoint.forward;

            GameObject bullet = Instantiate(
                bulletPrefab,
                firePoint.position,
                Quaternion.LookRotation(dir)
            );

            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.startColor = laserLine != null ? laserLine.startColor : Color.red;
                Color c = laserLine != null ? laserLine.startColor : Color.red;
                trail.endColor = new Color(c.r, c.g, c.b, 0f);
            }
        }

        StopAiming();

        yield return new WaitForSeconds(fireAnimationTime);

        isFiring = false;
    }

    void SpawnMuzzleFX()
    {
        if (muzzleFXPrefab == null || firePoint == null) return;

        GameObject fx = Instantiate(muzzleFXPrefab, firePoint.position, firePoint.rotation);

        // Nếu prefab chưa có script tự hủy thì destroy thủ công
        Destroy(fx, muzzleFXLifetime);
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        StopAiming();
        isFiring = false;
        isRetreating = false;
        retreatTimer = 0f;

        return base.TakeDamage(info);
    }
}