using UnityEngine;
using System.Collections; // Thêm thư viện này để dùng IEnumerator (Coroutine)

[RequireComponent(typeof(LineRenderer))] 
public class RangedMonster : MonsterController
{
    [Header("Ranged Settings")]
    public GameObject bulletPrefab; 
    public Transform firePoint;     
    public float aimDuration = 0.5f; // Sniper thì chỉnh cái này cao lên (vd: 2.5)
    
    [Header("Animation Settings")]
    [Tooltip("Tên trigger tấn công. Quái thường điền 'attack', Sniper điền 'Fire'")]
    public string attackAnimTrigger = "attack"; 
    
    [Tooltip("Thời gian đứng yên sau khi bắn để diễn Animation giật súng (Ví dụ: 0.5s)")]
    public float fireAnimationTime = 0.5f; 
    
    private bool isAiming = false;
    private bool isFiring = false; // Biến khóa AI trong lúc đang diễn hoạt hình bắn
    private float aimTimer = 0f;
    private LineRenderer laserLine;

    protected override void Start()
    {
        base.Start();
        
        // Setup Laser ngắm bắn
        laserLine = GetComponent<LineRenderer>();
        laserLine.positionCount = 2; 
        laserLine.startWidth = 0.05f; laserLine.endWidth = 0.05f;
        // Tạo material đỏ cho laser nếu chưa có
        if (laserLine.material == null)
             laserLine.material = new Material(Shader.Find("Sprites/Default")); 
        
        laserLine.startColor = Color.red;
        laserLine.endColor = new Color(1, 0, 0, 0);
        laserLine.enabled = false; 
    }

    public override void OnCombatBehavior(Transform player)
    {
        // Nếu bị đánh hoặc chết thì hủy ngắm ngay
        if (isHit || isDead) { StopAiming(); return; }

        // [QUAN TRỌNG] Nếu đang bóp cò/giật súng -> Khóa AI, không cho chạy hay ngắm tiếp
        if (isFiring) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // --- 1. ƯU TIÊN XỬ LÝ NGẮM BẮN ---
        // Nếu đang ngắm dở -> Phải đứng im ngắm cho xong (bất kể xa gần)
        if (isAiming)
        {
            HandleAiming(player);
            return;
        }

        // --- 2. LOGIC DI CHUYỂN ---
        // Xa quá -> Chạy lại gần
        if (distance > data.attackRange)
        {
            MoveToPosition(player.position, true); // true = Bỏ qua stopping distance để tự xử lý
        }
        // Gần quá -> Lùi lại (Thả diều)
        else if (distance < data.attackRange * 0.4f)
        {
            Vector3 dir = (transform.position - player.position).normalized;
            MoveToPosition(transform.position + dir * 3f, true);
        }
        // Vừa tầm -> Kiểm tra tầm nhìn
        else
        {
            if (MonsterManager.Instance.CanSeePlayer(this))
            {
                // Thấy Player -> Đứng lại & Bắn
                StopMoving();
                RotateTowards(player.position);
                
                if (CanAttack()) StartAiming();
            }
            else
            {
                // Bị tường che -> Di chuyển tìm góc bắn
                MoveToPosition(player.position, true);
            }
        }
    }

    // Hàm xử lý trong lúc đang ngắm (được gọi liên tục mỗi khung hình)
    void HandleAiming(Transform player)
    {
        // Ép đứng im tuyệt đối khi ngắm
        StopMoving();
        RotateTowards(player.position);
        
        // Vẽ tia Laser
        if (firePoint != null) {
            laserLine.SetPosition(0, firePoint.position);
            laserLine.SetPosition(1, player.position + Vector3.up);
        }

        // Nếu Player chạy khuất tầm nhìn -> Hủy ngắm
        if (!MonsterManager.Instance.CanSeePlayer(this)) {
            StopAiming();
            return;
        }

        // Đếm giờ ngắm
        aimTimer += Time.deltaTime;
        
        // Đủ giờ -> BẮN (Gọi Coroutine thay vì gọi hàm thường)
        if (aimTimer >= aimDuration && !isFiring) 
        {
            StartCoroutine(FireRoutine(player.position + Vector3.up));
        }
    }

    void StartAiming()
    {
        isAiming = true;
        aimTimer = 0f;
        laserLine.enabled = true;
        
        if (anim != null) anim.SetBool("isAiming", true);
    }

    void StopAiming()
    {
        isAiming = false;
        if (laserLine != null) laserLine.enabled = false;
        
        if (anim != null) anim.SetBool("isAiming", false);
        
        // Mở lại khả năng di chuyển cho NavMesh
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
    }

    // [CẬP NHẬT] Chuyển thành Coroutine để đợi Animation chạy xong
    IEnumerator FireRoutine(Vector3 targetPos)
    {
        isFiring = true; // Khóa AI

        // Tắt ngắm và Kích hoạt Animation Bắn (Lấy tên Trigger từ Inspector)
        if (anim != null)
        {
            anim.SetBool("isAiming", false); 
            anim.SetTrigger(attackAnimTrigger);         
        }

        // Sinh ra viên đạn
        if (bulletPrefab != null && firePoint != null)
        {
            Vector3 dir = (targetPos - firePoint.position).normalized;
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
            
            // Xử lý màu Trail cho đẹp (nếu có)
            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail != null) {
                 trail.startColor = laserLine.startColor;
                 trail.endColor = new Color(laserLine.startColor.r, laserLine.startColor.g, laserLine.startColor.b, 0f);
            }
        }
        
        // Tắt tia Laser
        StopAiming();

        // Chờ một nhịp cho quái diễn xong hành động giật súng / quăng đạn
        yield return new WaitForSeconds(fireAnimationTime);

        isFiring = false; // Mở khóa AI để nó tiếp tục chạy hoặc bắn phát tiếp theo
    }
    
    // Khi bị đánh trúng -> Hủy ngắm ngay để hiện animation bị thương (Hurt)
    public override HitResult TakeDamage(DamageInfo info)
    {
        StopAiming(); 
        return base.TakeDamage(info);
    }
}