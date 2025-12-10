using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerHammerController : MonoBehaviour, IDamageable, ICombatState
{
    #region 1. CONFIGURATION (Cấu hình chỉ số)
    [Header("Movement Stats")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    [Tooltip("Thời gian để đạt tốc độ tối đa (tạo cảm giác nặng)")]
    public float accelerationTime = 0.2f; 

    [Header("Dash / Dodge Stats")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.4f;
    public float dashCooldown = 0.8f;
    
    [Header("Combat Stats")]
    public string impulseCurveName = "ForwardImpulse"; // Tên Curve trong Animation

    // Các biến nội bộ
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCam;
    
    private Vector3 currentVelocity; // Dùng cho SmoothDamp di chuyển
    private Vector3 gravityVelocity;
    private float smoothVelocityX, smoothVelocityZ; // Biến phụ cho SmoothDamp
    #endregion

    #region 2. STATE MANAGEMENT (Quản lý trạng thái)
    public enum State { Idle, Moving, Attacking, Blocking, Dashing, Stunned }
    [Header("Debug Info")]
    public State currentState;
    
    // Trạng thái chiến đấu
    private bool isInvincible = false; // Bất tử khi Dash
    private bool isBlocking = false;
    private float lastDashTime;
    #endregion

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCam = Camera.main;
        
        // Khóa con trỏ chuột
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (currentState == State.Stunned) return;

        // Xử lý trọng lực luôn luôn chạy
        HandleGravity();

        // Máy trạng thái (State Machine)
        switch (currentState)
        {
            case State.Idle:
            case State.Moving:
                HandleLocomotion(); // Đi lại bình thường
                HandleActionInput(); // Nghe lệnh Đánh/Dash/Đỡ
                break;

            case State.Attacking:
                ProcessAnimationMovement(); // Di chuyển theo Curve của Animation
                // Không cho phép di chuyển WASD, nhưng có thể Dash để hủy đòn
                if (Input.GetKeyDown(KeyCode.Space)) TryDash(); 
                break;

            case State.Blocking:
                HandleBlockingMovement(); // Di chuyển chậm kiểu Strafe
                HandleActionInput();
                break;

            case State.Dashing:
                // Logic Dash được xử lý trong Coroutine, ở đây không làm gì
                break;
        }
    }

    // ----------------------------------------------------------------------
    // PHẦN 3: LOCOMOTION (Di chuyển & Vật lý)
    // ----------------------------------------------------------------------
    
    void HandleLocomotion()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // 1. Xoay nhân vật theo hướng Camera
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + mainCam.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSpeed, 0.1f);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            // 2. Tính hướng di chuyển
            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            // 3. Di chuyển có gia tốc (SmoothDamp) -> Tạo cảm giác nặng
            // Thay vì gán thẳng velocity, ta dùng SmoothDamp để tăng tốc từ từ
            float targetSpeed = moveSpeed;
            // (Bạn có thể thêm logic nhấn Shift để chạy nhanh ở đây)

            characterController.Move(moveDir * targetSpeed * Time.deltaTime);

            currentState = State.Moving;
            animator.SetFloat("Speed", 1f, 0.1f, Time.deltaTime); // Blend animation mượt
        }
        else
        {
            currentState = State.Idle;
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        }
    }

    void HandleGravity()
    {
        if (characterController.isGrounded && gravityVelocity.y < 0)
        {
            gravityVelocity.y = -2f; // Giữ nhân vật dính đất
        }
        gravityVelocity.y += gravity * Time.deltaTime;
        characterController.Move(gravityVelocity * Time.deltaTime);
    }

    // Hàm quan trọng: Đẩy nhân vật đi dựa trên Animation Curve
    void ProcessAnimationMovement()
    {
        float speedFromCurve = animator.GetFloat(impulseCurveName);
        if (speedFromCurve > 0.1f)
        {
            characterController.Move(transform.forward * speedFromCurve * Time.deltaTime);
        }
    }

    void HandleBlockingMovement()
    {
        // Khi đỡ, nhân vật luôn hướng về phía trước camera (Strafe)
        // Code xử lý di chuyển chậm sẽ thêm sau...
        if (Input.GetMouseButtonUp(1)) 
        {
            currentState = State.Idle;
            animator.SetBool("IsBlocking", false);
        }
    }

    // ----------------------------------------------------------------------
    // PHẦN 4: ACTION INPUT (Tấn công & Dash)
    // ----------------------------------------------------------------------

    void HandleActionInput()
    {
        // --- DASH (Ưu tiên cao nhất) ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryDash();
            return;
        }

        // --- ATTACK ---
        if (Input.GetMouseButtonDown(0))
        {
            StartAttack();
        }

        // --- BLOCK ---
        if (Input.GetMouseButton(1))
        {
            currentState = State.Blocking;
            animator.SetBool("IsBlocking", true);
        }
    }

    void TryDash()
    {
        if (Time.time < lastDashTime + dashCooldown) return;

        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        lastDashTime = Time.time;
        currentState = State.Dashing;
        isInvincible = true; // BẬT BẤT TỬ (I-FRAME)
        
        animator.SetTrigger("Dash");

        // Hướng Dash: Theo hướng đang di chuyển hoặc lùi lại nếu đứng yên
        Vector3 dashDir = transform.forward;
        if (Input.GetAxisRaw("Vertical") < -0.1f) dashDir = -transform.forward;
        // (Có thể mở rộng dash trái phải sau)

        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            // Di chuyển Dash (Trượt nhanh)
            characterController.Move(dashDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isInvincible = false; // TẮT BẤT TỬ
        currentState = State.Idle;
    }

    void StartAttack()
    {
        currentState = State.Attacking;
        animator.SetTrigger("Attack");
        // Logic Combo chi tiết sẽ thêm vào ở bước sau
    }

    // ----------------------------------------------------------------------
    // PHẦN 5: INTERFACE IMPLEMENTATION (Hợp đồng với Boss)
    // ----------------------------------------------------------------------
    
    public HitResult TakeDamage(DamageInfo info)
    {
        // 1. Check I-Frame (Dash)
        if (isInvincible) return HitResult.Miss;

        // 2. Check Block
        if (currentState == State.Blocking)
        {
            // Kiểm tra góc đỡ (ví dụ 120 độ phía trước)
            Vector3 dirToAttacker = (info.attacker.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToAttacker) < 60f)
            {
                // Hiệu ứng đẩy lùi khi đỡ
                animator.SetTrigger("BlockHit");
                return HitResult.Blocked;
            }
        }

        // 3. Dính đòn
        Debug.Log("Player hộc máu!");
        animator.SetTrigger("Hit"); // Animation bị đánh
        return HitResult.Hit;
    }

    // Interface ICombatState
    public bool IsInvincible() => isInvincible;
    public bool IsBlocking() => currentState == State.Blocking;
    public bool IsStunned() => currentState == State.Stunned;
}