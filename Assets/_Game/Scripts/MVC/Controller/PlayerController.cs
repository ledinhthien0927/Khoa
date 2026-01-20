using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("MVC")]
    [SerializeField] private PlayerModel model;
    [SerializeField] private PlayerView view;

    private CharacterController _cc;
    private Transform _camTransform;
    private float _turnSmoothVelocity;
    
    // Biến kiểm soát trạng thái đi tàu
    private bool _isTraveling = false;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (Camera.main) _camTransform = Camera.main.transform;
        
        // Tự tìm view/model nếu thiếu
        if (view == null) view = GetComponent<PlayerView>();
        if (model == null) model = new PlayerModel();

        // Init Stats
        model.currentHealth = model.maxHealth;
        model.currentStamina = model.maxStamina;
        model.currentState = PlayerState.Idle;
        
        // Init Visuals
        if(view) view.SwitchWeaponVisuals(model.currentWeapon);
    }

    // ========================================================================
    // [QUAN TRỌNG] CÁC HÀM FIX LỖI MAP MANAGER (ĐỪNG XÓA)
    // ========================================================================

    // 1. Cho phép MapManager lấy View để điều khiển Animation
    public PlayerView GetView() 
    { 
        return view; 
    }

    // 2. Chuyển đổi trạng thái "Đi tàu"
    public void SetTravelMode(bool isTraveling)
    {
        _isTraveling = isTraveling;
        _cc.enabled = !isTraveling; // Tắt Physics khi đi tàu

        if (isTraveling)
        {
            model.currentState = PlayerState.Idle;
            // Ẩn UI combat
            if (view) view.ToggleCombatUI(false);
        }
        else
        {
            // Hiện lại UI combat
            if (view) view.ToggleCombatUI(true);
            // Reset vũ khí visual
            if (view) view.SwitchWeaponVisuals(model.currentWeapon);
        }
    }

    // 3. Bật lại Camera chính (MapManager gọi)
    public void EnableMainCamera()
    {
        if (_camTransform != null) _camTransform.gameObject.SetActive(true);
    }

    // ========================================================================
    // MAIN LOOP
    // ========================================================================

    void Update()
    {
        // Nếu đang đi tàu hoặc chết -> Không làm gì cả
        if (_isTraveling || model.currentHealth <= 0 || model.currentState == PlayerState.Stunned) return;

        // 1. Hồi phục Stamina
        HandleStaminaRegen();

        // 2. Chuyển vũ khí (Chuột giữa)
        HandleWeaponSwitch();

        // 3. Xử lý Input Chiến đấu & Di chuyển
        if (model.currentState != PlayerState.Dashing && model.currentState != PlayerState.ParryingRecovery)
        {
            if (model.currentWeapon == WeaponType.Sword) HandleSwordCombat();
            else if (model.currentWeapon == WeaponType.Bow) HandleBowCombat();

            HandleInteraction();
        }

        // 4. Di chuyển
        HandleMovement();

        // 5. Cập nhật UI
        if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);
    }

    // ========================================================================
    // LOGIC CHIẾN ĐẤU
    // ========================================================================

    void HandleStaminaRegen()
    {
        if (Time.time - model.lastActionTime > model.staminaRegenDelay && model.currentState != PlayerState.Dashing)
        {
            model.currentStamina = Mathf.MoveTowards(model.currentStamina, model.maxStamina, model.staminaRegenRate * Time.deltaTime);
        }
    }

    void HandleWeaponSwitch()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && (model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving))
        {
            model.currentWeapon = (model.currentWeapon == WeaponType.Sword) ? WeaponType.Bow : WeaponType.Sword;
            view.SwitchWeaponVisuals(model.currentWeapon);
        }
    }

    void HandleSwordCombat()
    {
        // TẤN CÔNG
        if (Input.GetMouseButtonDown(0) && model.currentState != PlayerState.Parrying)
        {
            if (Time.time - model.lastActionTime > model.comboResetTime) model.currentComboStep = 0;
            if (model.currentComboStep < 3) StartCoroutine(PerformAttack(model.currentComboStep + 1));
        }
        // PARRY
        if (Input.GetMouseButtonDown(1) && model.currentState != PlayerState.Attacking)
        {
            StartCoroutine(PerformParry());
        }
        // DASH
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost)
        {
            StartCoroutine(PerformDash());
        }
    }

    void HandleBowCombat()
    {
        bool isHoldingAim = Input.GetMouseButton(1);
        
        if (isHoldingAim)
        {
            model.currentState = PlayerState.Aiming;
            view.ToggleCrosshair(true);
            view.SetCameraZoom(true, model.zoomFOV, model.normalFOV);
            view.SetAiming(true);
            RotateToCamera();

            if (Input.GetMouseButtonDown(0))
            {
                if (Time.time - model.lastActionTime > model.reloadTime && model.currentArrows > 0)
                    ShootArrow();
            }
        }
        else
        {
            if (model.currentState == PlayerState.Aiming) model.currentState = PlayerState.Idle;
            view.ToggleCrosshair(false);
            view.SetCameraZoom(false, model.zoomFOV, model.normalFOV);
            view.SetAiming(false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost)
            StartCoroutine(PerformDash());
    }

    IEnumerator PerformAttack(int step)
    {
        model.currentState = PlayerState.Attacking;
        model.currentComboStep = step;
        model.lastActionTime = Time.time;
        
        view.TriggerAttack(step);
        RotateToCamera();

        yield return new WaitForSeconds(0.4f); // Chờ animation
        
        if (model.currentState == PlayerState.Attacking) 
            model.currentState = PlayerState.Idle;
    }

    IEnumerator PerformParry()
    {
        model.currentState = PlayerState.Parrying;
        view.TriggerParry();
        yield return new WaitForSeconds(model.parryWindow); 
        // Logic thực sự của Parry (detect hit) sẽ nằm ở script nhận damage
        
        model.currentState = PlayerState.Idle;
    }

    void ShootArrow()
    {
        model.lastActionTime = Time.time;
        model.currentArrows--;
        view.TriggerShoot();

        Vector3 aimCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 100f));
        Vector3 spawnPos = model.arrowSpawnPoint ? model.arrowSpawnPoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 dir = (aimCenter - spawnPos).normalized;

        if (model.arrowPrefab)
        {
            GameObject arrow = Instantiate(model.arrowPrefab, spawnPos, Quaternion.LookRotation(dir));
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = dir * 40f; 
        }
    }

    // ========================================================================
    // MOVEMENT & INTERACTION
    // ========================================================================

    IEnumerator PerformDash()
    {
        model.currentState = PlayerState.Dashing;
        model.currentStamina -= model.dashCost;
        model.lastActionTime = Time.time;
        model.isInvincible = true;

        view.TriggerDash();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;
        
        Vector3 dashDir = transform.forward; 

        if (inputDir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            dashDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        else dashDir = -transform.forward; // Backstep

        float timer = 0;
        while (timer < model.dashDuration)
        {
            _cc.Move(dashDir * model.dashForce * Time.deltaTime);
            timer += Time.deltaTime;
            if (timer > model.dashIFrameDuration) model.isInvincible = false;
            yield return null;
        }

        model.isInvincible = false;
        model.currentState = PlayerState.Idle;
    }

    void HandleMovement()
    {
        if (model.currentState == PlayerState.Dashing || model.currentState == PlayerState.Parrying) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(h, 0f, v).normalized;

        float targetSpeed = (model.currentState == PlayerState.Aiming) ? model.aimMoveSpeed : model.walkSpeed;
        
        if (direction.magnitude >= 0.1f)
        {
            if (model.currentState != PlayerState.Aiming)
            {
                // Naraka Rotation Style
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                _cc.Move(moveDir * targetSpeed * Time.deltaTime);
                model.currentState = PlayerState.Moving;
            }
            else
            {
                // Strafe Movement when Aiming
                Vector3 moveDir = _camTransform.right * h + _camTransform.forward * v;
                moveDir.y = 0;
                _cc.Move(moveDir.normalized * targetSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (model.currentState == PlayerState.Moving) model.currentState = PlayerState.Idle;
        }

        // Animation update
        float currentVelocity = new Vector3(_cc.velocity.x, 0, _cc.velocity.z).magnitude;
        view.UpdateMovementAnim(currentVelocity / model.walkSpeed, model.currentState == PlayerState.Aiming);

        // Gravity
        if (!_cc.isGrounded) _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Branch")) { Destroy(hit.gameObject); return; }
                if (hit.CompareTag("Anvil")) { Debug.Log("Open Smithing UI"); }
            }
        }
    }
    
    void RotateToCamera()
    {
        if(_camTransform == null) return;
        Vector3 camDir = _camTransform.forward;
        camDir.y = 0;
        if (camDir != Vector3.zero)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(camDir), Time.deltaTime * model.rotationSpeed);
    }
}