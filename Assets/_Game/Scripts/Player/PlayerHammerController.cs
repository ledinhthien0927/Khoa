using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerHammerController : MonoBehaviour 
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
    public bool requireHitToCombo = false; 

    // Hash Animator
    private readonly int hashSpeed = Animator.StringToHash("Speed");
    private readonly int hashHorizontal = Animator.StringToHash("Horizontal"); // Dùng cho Blend Tree 2D
    private readonly int hashVertical = Animator.StringToHash("Vertical");     // Dùng cho Blend Tree 2D
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashComboIndex = Animator.StringToHash("ComboIndex");
    private readonly int hashDash = Animator.StringToHash("Dash");
    private readonly int hashBlocking = Animator.StringToHash("IsBlocking");
    private readonly int hashHit = Animator.StringToHash("Hit");

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

        switch (currentState)
        {
            case State.Idle:
            case State.Moving:
                HandleLocomotion();
                HandleCombatInput();
                break;

            case State.Attacking:
                if (canMoveCancel && IsTryingToMove())
                {
                    EndAttack(); 
                    return;      
                }
                ProcessAnimationMove();
                if (Input.GetMouseButtonDown(0)) clickCount++;
                if (Input.GetKeyDown(KeyCode.Space)) TryDash();
                break;

            case State.Blocking:
                if (Input.GetMouseButtonUp(1)) EndBlock();
                break;

            case State.Dashing:
                break;
        }
    }

    // =========================================================
    // 3. LOGIC DI CHUYỂN (STRAFE MOVEMENT)
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

    bool IsTryingToMove() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude > 0.1f;

    void HandleLocomotion()
    {
        float h = Input.GetAxis("Horizontal"); // Giá trị mượt để Blend hoạt ảnh
        float v = Input.GetAxis("Vertical");
        Vector3 moveDir = GetCameraRelativeInput();

        // Xoay nhân vật: LUÔN HƯỚNG MẶT THEO CAMERA
        Vector3 lookDir = mainCam.transform.forward;
        lookDir.y = 0;
        if (lookDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Di chuyển thực tế
        if (moveDir.magnitude > 0.1f)
        {
            characterController.Move(moveDir * moveSpeed * Time.deltaTime);
            currentState = State.Moving;
        }
        else
        {
            currentState = State.Idle;
        }

        // Cập nhật Animator cho Blend Tree 2D
        // h, v sẽ điều khiển việc đi tiến, lùi, trái, phải trong Blend Tree
        animator.SetFloat(hashHorizontal, h, 0.1f, Time.deltaTime);
        animator.SetFloat(hashVertical, v, 0.1f, Time.deltaTime);
        animator.SetFloat(hashSpeed, new Vector2(h, v).magnitude, 0.1f, Time.deltaTime);
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
        float speed = animator.GetFloat("ForwardImpulse");
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

        clickCount = Mathf.Max(0, clickCount - 1);
        animator.SetInteger(hashComboIndex, comboIndex);

        if (comboIndex == 0)
        {
            animator.ResetTrigger(hashAttack);
            animator.SetTrigger(hashAttack);
        }
    }

    public void OpenHitbox() // Animation Event
    {
        Vector3 center = transform.position + transform.forward * 1.0f;
        int hitCount = Physics.OverlapSphereNonAlloc(center, attackRadius, hitCollidersBuffer, enemyLayer);

        for(int i = 0; i < hitCount; i++)
        {
            GameObject hitObj = hitCollidersBuffer[i].gameObject;
            if (!hitEnemies.Contains(hitObj))
            {
                Debug.Log($"Hit: {hitObj.name} | Damage: {damageBase}");
                hitEnemies.Add(hitObj);
                hasHitEnemy = true;
            }
        }
    }

    public void AllowEarlyExit() => canMoveCancel = true;

    public void CheckCombo() // Animation Event
    {
        canMoveCancel = false;
        bool hitCondition = !requireHitToCombo || hasHitEnemy;

        if (clickCount > 0 && hitCondition)
        {
            comboIndex++;
            if (comboIndex > 2) comboIndex = 0;
            StartAttack(); 
        }
        else
        {
            clickCount = 0;
            canMoveCancel = true;
        }
    }

    public void EndAttack() // Animation Event
    {
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
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        lastDashTime = Time.time;
        currentState = State.Dashing;
        isInvincible = true;

        animator.SetTrigger(hashDash);

        Vector3 inputDir = GetCameraRelativeInput();
        Vector3 dashDir = (inputDir.magnitude > 0.1f) ? inputDir : transform.forward;
        transform.rotation = Quaternion.LookRotation(dashDir);

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            if (currentState == State.Stunned) { isInvincible = false; yield break; }
            characterController.Move(dashDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isInvincible = false;
        if (currentState == State.Dashing) EndAttack();
    }

    void StartBlock() 
    { 
        if(currentState == State.Dashing) return;
        currentState = State.Blocking;
        animator.SetBool(hashBlocking, true);
        animator.ResetTrigger(hashAttack);
        comboIndex = 0;
    }
    
    void EndBlock() 
    { 
        currentState = State.Idle;
        animator.SetBool(hashBlocking, false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1.0f, attackRadius);
    }
}