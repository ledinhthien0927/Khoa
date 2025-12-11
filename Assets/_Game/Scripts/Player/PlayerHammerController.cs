using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Đảm bảo có CharacterController và Animator
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerHammerController : MonoBehaviour, IDamageable, ICombatState
{
    // =========================================================
    // 1. CẤU HÌNH (SETTINGS)
    // =========================================================
    [Header("--- Movement ---")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f; 
    public float gravity = -9.81f;

    [Header("--- Dash ---")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.4f;
    public float dashCooldown = 0.8f;

    [Header("--- Combat ---")]
    public LayerMask enemyLayer;      // NHỚ CHỌN LAYER 'Enemy' Ở INSPECTOR
    public float attackRadius = 1.5f; 
    public float damageBase = 10f;
    
    // Tên các Parameter trong Animator (Phải trùng khớp 100%)
    private readonly int hashSpeed = Animator.StringToHash("Speed");
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashComboIndex = Animator.StringToHash("ComboIndex");
    private readonly int hashDash = Animator.StringToHash("Dash");
    private readonly int hashIsBlocking = Animator.StringToHash("IsBlocking");
    private readonly int hashBlockHit = Animator.StringToHash("BlockHit");
    private readonly int hashHit = Animator.StringToHash("Hit");

    // =========================================================
    // 2. BIẾN TRẠNG THÁI (STATE VARIABLES)
    // =========================================================
    public enum State { Idle, Moving, Attacking, Blocking, Dashing, Stunned }
    [Header("--- Debug Info ---")]
    public State currentState;

    // Logic Combo & Hit-Run
    public int comboIndex = 0;
    private bool inputBuffered = false; 
    private bool canMoveCancel = false; // Cho phép Hit & Run
    private bool hasHitEnemy = false;
    private List<GameObject> hitEnemies = new List<GameObject>();

    // Logic Vật lý & Dash
    private Vector3 gravityVelocity;
    private float lastDashTime;
    private bool isInvincible = false;

    // Components
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCam;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCam = Camera.main; // Cần MainCamera trong Scene
        
        // Ẩn chuột đi cho giống game hành động
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void Update()
    {
        if (currentState == State.Stunned) return;

        HandleGravity();

        switch (currentState)
        {
            case State.Idle:
            case State.Moving:
                HandleLocomotion(); // Xử lý di chuyển thường
                HandleInput();      // Xử lý bấm nút Đánh/Dash/Đỡ
                break;

            case State.Attacking:
                // [HIT AND RUN]: Nếu animation cho phép hủy (canMoveCancel) VÀ người chơi bấm đi
                if (canMoveCancel && IsTryingToMove())
                {
                    EndCombo(); // Ngắt chiêu ngay lập tức
                    return;
                }
                
                ProcessAnimationMovement(); // Lao tới theo lực Animation Curve

                if (Input.GetMouseButtonDown(0)) inputBuffered = true; // Lưu lệnh combo
                if (Input.GetKeyDown(KeyCode.Space)) TryDash(); // Cho phép Dash hủy chiêu
                break;

            case State.Blocking:
                if (Input.GetMouseButtonUp(1)) EndBlock();
                break;

            case State.Dashing:
                // Dash chạy trong Coroutine riêng nên Update không cần làm gì
                break;
        }
    }

    // =========================================================
    // 3. LOGIC DI CHUYỂN (CAMERA RELATIVE)
    // =========================================================
    
    // Kiểm tra xem người chơi có đang bấm WASD không
    bool IsTryingToMove()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return new Vector3(h, 0, v).magnitude > 0.1f;
    }

    void HandleLocomotion()
    {
        if (IsTryingToMove())
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Tính hướng đi dựa trên Camera (để không bị ngược hướng khi lùi)
            Vector3 camForward = mainCam.transform.forward;
            Vector3 camRight = mainCam.transform.right;
            camForward.y = 0; // Giữ nhân vật trên mặt đất
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * v + camRight * h).normalized;

            if (moveDir.magnitude > 0.1f)
            {
                // Xoay nhân vật
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Di chuyển
                characterController.Move(moveDir * moveSpeed * Time.deltaTime);
            }

            currentState = State.Moving;
            animator.SetFloat(hashSpeed, 1f, 0.1f, Time.deltaTime);
        }
        else
        {
            currentState = State.Idle;
            animator.SetFloat(hashSpeed, 0f, 0.1f, Time.deltaTime);
        }
    }

    void HandleGravity()
    {
        if (characterController.isGrounded && gravityVelocity.y < 0) gravityVelocity.y = -2f;
        gravityVelocity.y += gravity * Time.deltaTime;
        characterController.Move(gravityVelocity * Time.deltaTime);
    }

    // Đọc Curve "ForwardImpulse" từ Animation để đẩy nhân vật tới
    void ProcessAnimationMovement()
    {
        float speed = animator.GetFloat("ForwardImpulse");
        if (speed > 0.01f)
        {
            characterController.Move(transform.forward * speed * Time.deltaTime);
        }
    }

    // =========================================================
    // 4. LOGIC CHIẾN ĐẤU (COMBAT)
    // =========================================================

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) { TryDash(); return; }
        if (Input.GetMouseButtonDown(0)) StartAttack();
        if (Input.GetMouseButton(1)) StartBlock();
    }

    void StartAttack()
    {
        currentState = State.Attacking;
        inputBuffered = false;
        canMoveCancel = false; // Mới vào đánh thì khóa di chuyển
        hitEnemies.Clear();

        // Reset trigger Attack cũ để tránh lỗi spam
        animator.ResetTrigger(hashAttack);
        
        animator.SetInteger(hashComboIndex, comboIndex);
        animator.SetTrigger(hashAttack);
    }

    // --- CÁC HÀM SỰ KIỆN (ANIMATION EVENTS) ---

    // [EVENT 1] OpenHitbox: Gọi tại frame bắt đầu vung búa
    public void OpenHitbox()
    {
        CheckWeaponHit();
    }

    // [EVENT 2] OnRecoveryStart: Gọi ngay sau khi búa đi qua mục tiêu (Hit & Run)
    public void OnRecoveryStart()
    {
        canMoveCancel = true; 
    }

    // [EVENT 3] CheckComboWindow: Gọi gần cuối animation
    public void CheckComboWindow()
    {
        canMoveCancel = false;

        // IN RA LOG ĐỂ KIỂM TRA GIÁ TRỊ TẠI THỜI ĐIỂM CHECK
        Debug.Log($"CHECK COMBO: Đã bấm nút = {inputBuffered} | Đã trúng địch = {hasHitEnemy}");

        if (inputBuffered && hasHitEnemy) 
        {
            Debug.Log("--> ĐIỀU KIỆN THỎA MÃN! TĂNG COMBO.");
            comboIndex++;
            if (comboIndex > 2) comboIndex = 0;
            StartAttack();
        }
        else
        {
            Debug.Log("--> KHÔNG ĐỦ ĐIỀU KIỆN. VỀ IDLE.");
            // Cho phép Hit & Run lần cuối
            canMoveCancel = true;
        }
    }

    // [EVENT 4] EndCombo: Gọi ở Frame cuối cùng (SỬA LỖI MẤT RECEIVER)
    public void EndCombo()
    {
        currentState = State.Idle;
        comboIndex = 0;
        animator.SetInteger(hashComboIndex, 0);
        canMoveCancel = false;
        inputBuffered = false;
        hasHitEnemy = false;
        
        // Cưỡng chế về Idle animation để tránh kẹt
        // animator.Play("Idle", 0, 0.2f); 
    }

    // --- XỬ LÝ VA CHẠM VŨ KHÍ ---
    void CheckWeaponHit()
    {
        Vector3 center = transform.position + transform.forward * 1.0f;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius, enemyLayer);

        foreach (var hit in hits)
        {
            if (!hitEnemies.Contains(hit.gameObject))
            {
                IDamageable damageable = hit.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Đòn 3 dam to hơn
                    float dmg = (comboIndex == 2) ? damageBase * 3f : damageBase;
                    DamageType type = (comboIndex == 2) ? DamageType.Heavy : DamageType.Physical;

                    // Gửi thông tin dam
                    DamageInfo info = new DamageInfo { 
                        amount = dmg, 
                        attacker = gameObject, 
                        hitPoint = hit.ClosestPoint(transform.position),
                        hitDirection = transform.forward,
                        knockbackForce = 5f,
                        type = type
                    };
                    damageable.TakeDamage(info);
                }
                hitEnemies.Add(hit.gameObject);
            }
        }
    }

    // =========================================================
    // 5. DASH & BLOCK
    // =========================================================
    
    void TryDash()
    {
        if (Time.time < lastDashTime + dashCooldown) return;
        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        lastDashTime = Time.time;
        currentState = State.Dashing;
        isInvincible = true;
        animator.SetTrigger(hashDash);

        // Hướng Dash (theo hướng di chuyển hoặc lùi lại)
        Vector3 dashDir = transform.forward;
        if (Input.GetAxisRaw("Vertical") < -0.1f) dashDir = -transform.forward;

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            characterController.Move(dashDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isInvincible = false;
        currentState = State.Idle;
    }

    void StartBlock()
    {
        currentState = State.Blocking;
        animator.SetBool(hashIsBlocking, true);
    }

    void EndBlock()
    {
        currentState = State.Idle;
        animator.SetBool(hashIsBlocking, false);
    }

    // =========================================================
    // 6. INTERFACE & GIZMOS
    // =========================================================
    
    // Nhận sát thương (Interface)
    public HitResult TakeDamage(DamageInfo info)
    {
        if (isInvincible) return HitResult.Miss;

        if (currentState == State.Blocking)
        {
            Vector3 dirToAttacker = (info.attacker.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToAttacker) < 60f)
            {
                animator.SetTrigger(hashBlockHit);
                return HitResult.Blocked;
            }
        }

        animator.SetTrigger(hashHit);
        return HitResult.Hit;
    }

    public bool IsInvincible() => isInvincible;
    public bool IsBlocking() => currentState == State.Blocking;
    public bool IsStunned() => currentState == State.Stunned;

    // Vẽ vòng tròn tầm đánh
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1.0f, attackRadius);
    }
}