using UnityEngine;

[RequireComponent(typeof(LineRenderer))] 
public class RangedMonster : MonsterController
{
    [Header("Ranged")]
    public GameObject bulletPrefab; 
    public Transform firePoint;     
    public float aimDuration = 0.5f; 
    
    private bool isAiming = false;
    private float aimTimer = 0f;
    private LineRenderer laserLine;

    protected override void Start()
    {
        base.Start();
        laserLine = GetComponent<LineRenderer>();
        laserLine.positionCount = 2; 
        laserLine.startWidth = 0.05f; laserLine.endWidth = 0.05f;
        laserLine.material = new Material(Shader.Find("Sprites/Default")); 
        laserLine.startColor = Color.red;
        laserLine.endColor = new Color(1, 0, 0, 0);
        laserLine.enabled = false; 
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (isHit || isDead) { StopAiming(); return; }

        float distance = Vector3.Distance(transform.position, player.position);

        // --- DEBUG LOGIC (Kiểm tra xem nó đang chui vào nhánh nào) ---
        
        // 1. Check xem có bị kẹt Aiming không
        if (isAiming)
        {
            // Debug.Log("<color=yellow>[RANGE] Đang bận ngắm bắn...</color>");
            HandleAiming(player);
            return;
        }

        // 2. LOGIC DI CHUYỂN
        // Ưu tiên số 1: Xa quá thì phải chạy
        if (distance > data.attackRange)
        {
            // Debug.Log($"<color=green>[RANGE] Xa quá ({distance:F1} > {data.attackRange}) -> Đang lệnh CHẠY!</color>");
            MoveToPosition(player.position, true); // true = Ép chạy
        }
        else if (distance < data.attackRange * 0.4f)
        {
            // Debug.Log("<color=cyan>[RANGE] Gần quá -> Đang KITING lùi!</color>");
            Vector3 dir = (transform.position - player.position).normalized;
            MoveToPosition(transform.position + dir * 3f, true);
        }
        else
        {
            // Trong tầm bắn
            if (MonsterManager.Instance.CanSeePlayer(this))
            {
                StopMoving();
                RotateTowards(player.position);
                
                // Debug.Log("<color=red>[RANGE] Đã vào tầm -> Chuẩn bị bắn!</color>");
                
                if (CanAttack()) StartAiming();
            }
            else
            {
                // Debug.Log("<color=orange>[RANGE] Bị tường che -> Đang di chuyển tìm góc!</color>");
                MoveToPosition(player.position, true);
            }
        }
    }

    void HandleAiming(Transform player)
    {
        StopMoving();
        RotateTowards(player.position);
        
        if (firePoint != null) {
            laserLine.SetPosition(0, firePoint.position);
            laserLine.SetPosition(1, player.position + Vector3.up);
        }

        if (!MonsterManager.Instance.CanSeePlayer(this)) {
            StopAiming();
            return;
        }

        aimTimer += Time.deltaTime;
        if (aimTimer >= aimDuration) Fire(player.position + Vector3.up);
    }

    void StartAiming()
    {
        isAiming = true;
        aimTimer = 0f;
        laserLine.enabled = true;
        if (anim != null) anim.SetTrigger("attack");
    }

    void StopAiming()
    {
        isAiming = false;
        if (laserLine != null) laserLine.enabled = false;
    }

    void Fire(Vector3 targetPos)
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Vector3 dir = (targetPos - firePoint.position).normalized;
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail != null) {
                 trail.startColor = laserLine.startColor;
                 trail.endColor = new Color(laserLine.startColor.r, laserLine.startColor.g, laserLine.startColor.b, 0f);
            }
        }
        StopAiming();
    }
    
    public override HitResult TakeDamage(DamageInfo info)
    {
        StopAiming(); 
        return base.TakeDamage(info);
    }
}