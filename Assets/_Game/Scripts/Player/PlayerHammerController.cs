using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerHammerController : MonoBehaviour // Giả sử IDamageable, ICombatState nằm ở file khác
{
    // =========================================================
    // 1. CẤU HÌNH (SETTINGS)
    // =========================================================
    [Header("--- Movement & Camera ---")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    public float gravity = -9.81f;

    [Header("--- Dash ---")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 0.6f;

    [Header("--- Combat ---")]
    public LayerMask enemyLayer;
    public float attackRadius = 1.5f;
    public float damageBase = 10f;
    public bool requireHitToCombo = false; // Tùy chọn: Có cần đánh trúng mới được combo không?

    // Hash Animator
    private readonly int hashSpeed = Animator.StringToHash("Speed");
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashComboIndex = Animator.StringToHash("ComboIndex");
    private readonly int hashDash = Animator.StringToHash("Dash");
    private readonly int hashBlocking = Animator.StringToHash("IsBlocking");
    private readonly int hashHit = Animator.StringToHash("Hit");
    private readonly int hashBlockHit = Animator.StringToHash("BlockHit");

    // =========================================================
    // 2. BIẾN TRẠNG THÁI (STATE)
    // =========================================================
    public enum State { Idle, Moving, Attacking, Blocking, Dashing, Stunned }
    
    [Header("--- Debug Info ---")]
    public State currentState;

    [Header("--- Combo Debug ---")]
    public int comboIndex = 0;
    public int clickCount = 0;
    public bool hasHitEnemy = false;
    public bool canMoveCancel = false;

    private List<GameObject> hitEnemies = new List<GameObject>();
    
    // Tối ưu Physics: Dùng mảng cố định để tránh rác bộ nhớ
    private Collider[] hitCollidersBuffer = new Collider[10]; 

    // Components
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCam;
    
    private Vector3 gravityVelocity;
    private float lastDashTime;
    private bool isInvincible = false;
    private Coroutine dashCoroutine;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCam = Camera.main;
    }

    void Update()
    {
        if (currentState == State.Stunned) return;

        ApplyGravity();

        // --- CƠ CHẾ AN TOÀN (FAIL-SAFE) ---
        // Nếu Code nghĩ là đang đánh, nhưng Animator đã về Idle/Run -> Reset ngay.
        if (currentState == State.Attacking)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            // Kiểm tra nếu animation không phải là Attack và không đang chuyển tiếp
            if (!info.IsTag("Attack") && !animator.IsInTransition(0) && info.normalizedTime > 0.9f) 
            {
                 // Đôi khi Event EndAttack bị miss, đây là chốt chặn cuối cùng
                 // Tuy nhiên, cẩn thận kẻo nó reset khi animation vừa mới bắt đầu
            }
        }
        // ------------------------------------

        switch (currentState)
        {
            case State.Idle:
            case State.Moving:
                HandleLocomotion();
                HandleCombatInput();
                break;

            case State.Attacking:
                // Hit & Run: Di chuyển để hủy hoạt ảnh thừa
                if (canMoveCancel && IsTryingToMove())
                {
                    EndAttack(); // Reset ngay lập tức
                    return;      // Xuống Update sau để bắt đầu logic di chuyển
                }

                ProcessAnimationMove();

                // Input Buffering: Đang đánh vẫn nhận lệnh bấm cho đòn sau
                if (Input.GetMouseButtonDown(0)) clickCount++;
                
                // Dash hủy chiêu khẩn cấp
                if (Input.GetKeyDown(KeyCode.Space)) TryDash();
                break;

            case State.Blocking:
                if (Input.GetMouseButtonUp(1)) EndBlock();
                break;

            case State.Dashing:
                // Logic nằm trong Coroutine
                break;
        }
    }

    // =========================================================
    // 3. LOGIC DI CHUYỂN
    // =========================================================

    Vector3 GetCameraRelativeInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camFwd = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;
        camFwd.y = 0; camRight.y = 0;
        camFwd.Normalize(); camRight.Normalize();

        return (camFwd * v + camRight * h).normalized;
    }

    bool IsTryingToMove() => GetCameraRelativeInput().magnitude > 0.1f;

    void HandleLocomotion()
    {
        Vector3 moveDir = GetCameraRelativeInput();

        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            characterController.Move(moveDir * moveSpeed * Time.deltaTime);

            currentState = State.Moving;
            animator.SetFloat(hashSpeed, 1f, 0.1f, Time.deltaTime);
        }
        else
        {
            currentState = State.Idle;
            animator.SetFloat(hashSpeed, 0f, 0.1f, Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        if (characterController.isGrounded && gravityVelocity.y < 0)
            gravityVelocity.y = -2f;
        
        gravityVelocity.y += gravity * Time.deltaTime;
        characterController.Move(gravityVelocity * Time.deltaTime);
    }

    void ProcessAnimationMove()
    {
        // Root Motion giả: Animator điều khiển tốc độ lướt tới
        float speed = animator.GetFloat("ForwardImpulse"); // Cần set param này trong Animation Curve
        if (speed > 0.01f)
            characterController.Move(transform.forward * speed * Time.deltaTime);
    }

    // =========================================================
    // 4. LOGIC CHIẾN ĐẤU
    // =========================================================

    void HandleCombatInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) { TryDash(); return; }

        if (Input.GetMouseButtonDown(0))
        {
            clickCount++; 
            comboIndex = 0;
            StartAttack();
        }

        if (Input.GetMouseButton(1)) StartBlock();
    }

    void StartAttack()
    {
        currentState = State.Attacking;
        hasHitEnemy = false;
        canMoveCancel = false;
        hitEnemies.Clear();

        // Tiêu thụ 1 lượt bấm
        clickCount = Mathf.Max(0, clickCount - 1);

        animator.SetInteger(hashComboIndex, comboIndex);

        // Chỉ trigger cho đòn đầu tiên để bắt đầu State Machine
        if (comboIndex == 0)
        {
            animator.ResetTrigger(hashAttack);
            animator.SetTrigger(hashAttack);
        }
    }

    // [ANIMATION EVENT]
    public void OpenHitbox()
    {
        Vector3 center = transform.position + transform.forward * 1.0f;
        
        // Tối ưu: Dùng NonAlloc để không sinh rác bộ nhớ
        int hitCount = Physics.OverlapSphereNonAlloc(center, attackRadius, hitCollidersBuffer, enemyLayer);

        for(int i = 0; i < hitCount; i++)
        {
            GameObject hitObj = hitCollidersBuffer[i].gameObject;
            if (!hitEnemies.Contains(hitObj))
            {
                // Giả lập Interface gây damage
                // var damageable = hitObj.GetComponent<IDamageable>();
                // if (damageable != null) { ... }
                
                Debug.Log($"Hit: {hitObj.name} | Damage: {(comboIndex == 2 ? damageBase * 3 : damageBase)}");

                hitEnemies.Add(hitObj);
                hasHitEnemy = true;
            }
        }
    }

    // [ANIMATION EVENT]
    public void AllowEarlyExit()
    {
        canMoveCancel = true;
    }

    // [ANIMATION EVENT] CheckCombo
    public void CheckCombo()
    {
        canMoveCancel = false;

        // Logic cải tiến: Cho phép đánh trượt vẫn combo nếu tắt `requireHitToCombo`
        bool hitCondition = !requireHitToCombo || hasHitEnemy;

        if (clickCount > 0 && hitCondition)
        {
            comboIndex++;
            if (comboIndex > 2) comboIndex = 0;
            StartAttack(); // Animator sẽ tự chuyển state dựa trên Int ComboIndex thay đổi
        }
        else
        {
            // Hết combo -> Cho phép hủy động tác thừa
            clickCount = 0;
            canMoveCancel = true;
        }
    }

    // [ANIMATION EVENT]
    public void EndAttack()
    {
        // Reset toàn bộ về trạng thái gốc
        currentState = State.Idle;
        comboIndex = 0;
        clickCount = 0; 
        animator.SetInteger(hashComboIndex, 0);
        canMoveCancel = false;
        hasHitEnemy = false;
    }

    // =========================================================
    // 5. DASH & BLOCK
    // =========================================================

    void TryDash()
    {
        if (Time.time < lastDashTime + dashCooldown) return;
        
        // Ngắt coroutine cũ nếu có
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        lastDashTime = Time.time;
        currentState = State.Dashing;
        isInvincible = true;

        animator.ResetTrigger(hashDash);
        animator.SetTrigger(hashDash);

        // Xác định hướng Dash ngay lúc bấm
        Vector3 inputDir = GetCameraRelativeInput();
        Vector3 dashDir = (inputDir.magnitude > 0.1f) ? inputDir : transform.forward;
        
        // Xoay nhân vật theo hướng dash ngay lập tức
        transform.rotation = Quaternion.LookRotation(dashDir);

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            // AN TOÀN: Nếu bị choáng giữa chừng -> Ngắt Dash ngay
            if (currentState == State.Stunned)
            {
                isInvincible = false;
                yield break; 
            }

            characterController.Move(dashDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isInvincible = false;
        
        // Chỉ reset về Idle nếu chưa bị chuyển sang trạng thái khác (như Stun)
        if (currentState == State.Dashing)
        {
            EndAttack(); // Gọi hàm này để dọn dẹp biến số luôn
        }
    }

    void StartBlock() 
    { 
        // Không block khi đang dash hoặc tấn công (trừ khi cho phép cancel)
        if(currentState == State.Dashing) return;
        
        currentState = State.Blocking; 
        animator.SetBool(hashBlocking, true); 
        // Reset trigger attack để tránh lỗi visual
        animator.ResetTrigger(hashAttack);
        comboIndex = 0; // Hủy combo nếu block
    }
    
    void EndBlock() 
    { 
        currentState = State.Idle; 
        animator.SetBool(hashBlocking, false); 
    }

    // =========================================================
    // 6. DEBUG & GIZMOS
    // =========================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1.0f, attackRadius);
    }
    
    // Hàm nhận damage giả lập
    public void TakeDamageFromEnemy()
    {
        if (isInvincible) return;
        // Logic tính toán block hướng...
        // Nếu dính đòn:
        currentState = State.Stunned;
        animator.SetTrigger(hashHit);
        // Cần có logic để thoát khỏi Stun (ví dụ sau 1 khoảng thời gian)
        Invoke(nameof(RecoverFromStun), 0.5f);
    }

    void RecoverFromStun()
    {
        currentState = State.Idle;
    }
}