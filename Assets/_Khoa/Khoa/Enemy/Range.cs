using UnityEngine;

[RequireComponent(typeof(LineRenderer))] // Tự động thêm component vẽ tia laser
public class RangedMonster : MonsterController
{
    [Header("Ranged Settings")]
    public GameObject bulletPrefab; 
    public Transform firePoint;     

    [Header("Aim Settings (Ngắm bắn)")]
    [Tooltip("Thời gian ngắm trước khi bắn (giây)")]
    public float aimDuration = 0.5f; 
    public bool useLaserSight = true; // Có bật tia laser không?

    // Biến nội bộ quản lý trạng thái
    private bool isAiming = false;
    private float currentAimTime = 0f;
    private LineRenderer laserLine;

    protected override void Start()
    {
        base.Start();
        
        // Setup Laser đơn giản bằng code
        laserLine = GetComponent<LineRenderer>();
        laserLine.positionCount = 2; // Điểm đầu và cuối
        laserLine.startWidth = 0.02f; // Tia nhỏ
        laserLine.endWidth = 0.02f;
        laserLine.material = new Material(Shader.Find("Sprites/Default")); // Material mặc định
        laserLine.startColor = Color.red;
        laserLine.endColor = new Color(1, 0, 0, 0); // Đỏ mờ dần ở đuôi
        laserLine.enabled = false; // Mặc định tắt
    }

    public override void OnCombatBehavior(Transform player)
    {
        // --- 1. ƯU TIÊN CAO NHẤT: ĐANG NGẮM THÌ CHỈ XOAY VÀ CHỜ ---
        if (isAiming)
        {
            HandleAimingState(player);
            return; // Dừng hàm tại đây, không chạy code di chuyển bên dưới
        }

        // --- 2. LOGIC DI CHUYỂN BÌNH THƯỜNG ---
        float distance = Vector3.Distance(transform.position, player.position);
        float safeDistance = data.attackRange * 0.5f;

        // Xa quá -> Lại gần
        if (distance > data.attackRange)
        {
            MoveToPosition(player.position, false);
        }
        // Gần quá -> Lùi lại (Kiting)
        else if (distance < safeDistance)
        {
            Vector3 dirAway = (transform.position - player.position).normalized;
            MoveToPosition(transform.position + dirAway * 5f, true);
        }
        // Trong tầm bắn
        else
        {
            if (MonsterManager.Instance.CanSeePlayer(this))
            {
                StopMoving();
                RotateTowards(player.position);

                // Kiểm tra Cooldown
                if (CanAttack())
                {
                    StartAiming(); // Bắt đầu quy trình ngắm
                }
            }
            else
            {
                // Bị tường che -> Di chuyển tìm góc
                MoveToPosition(player.position, false);
            }
        }
    }

    // --- CÁC HÀM XỬ LÝ AIMING ---

    void StartAiming()
    {
        isAiming = true;
        currentAimTime = 0f;

        // Chạy animation giơ súng
        if (anim != null) anim.SetTrigger("attack");

        // Bật Laser
        if (useLaserSight) laserLine.enabled = true;

        Debug.Log($"<color=orange>{gameObject.name} ĐANG NGẮM...</color>");
    }

    void HandleAimingState(Transform player)
    {
        // 1. Đứng yên và luôn xoay mặt về phía Player (Tracking)
        StopMoving();
        RotateTowards(player.position);

        // 2. Vẽ tia Laser cập nhật liên tục
        if (useLaserSight)
        {
            laserLine.SetPosition(0, firePoint.position);
            laserLine.SetPosition(1, player.position + Vector3.up * 1.0f); // Nhắm vào ngực
        }

        // 3. Kiểm tra: Nếu Player chạy khuất tường -> Hủy bắn
        if (!MonsterManager.Instance.CanSeePlayer(this))
        {
            CancelShot();
            return;
        }

        // 4. Đếm thời gian
        currentAimTime += Time.deltaTime;
        if (currentAimTime >= aimDuration)
        {
            FireBullet();
        }
    }

    void FireBullet()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            // Bắn đạn theo hướng xoay của firePoint (lúc này đã chuẩn hướng Player)
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Debug.Log($"<color=cyan>[Ranged] {gameObject.name} BẮN ĐẠN!</color>");
        }
        else
        {
            Debug.LogError($"[Lỗi] {gameObject.name} thiếu Bullet Prefab hoặc Fire Point!");
        }

        // Kết thúc ngắm -> Reset timer Cooldown
        StopAiming();
        
        // Lưu ý: CanAttack() trong MonsterController dùng Time.time để check, 
        // nên việc gọi FireBullet xong ta để timer tự trôi là ổn, hoặc nếu cần reset thủ công:
        // ResetAttackTimer(); 
    }

    void CancelShot()
    {
        Debug.Log("Mất dấu mục tiêu! Hủy bắn.");
        StopAiming();
    }

    void StopAiming()
    {
        isAiming = false;
        if (laserLine != null) laserLine.enabled = false;
        currentAimTime = 0f;
    }
}