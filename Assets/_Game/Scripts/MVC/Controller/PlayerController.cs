using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("MVC Components")]
    public PlayerModel model; 
    
    [SerializeField] private PlayerView view;
    [SerializeField] private ThirdPersonCamera tpsCamera; 
    [SerializeField] private SwordWeapon swordScript; 

    private bool _wasArmedBeforeSwim = false;
    private int _savedStateBeforeSwim = 0;

    // --- BIẾN NỘI BỘ ---
    private CharacterController _cc;
    private Transform _camTransform;
    private float _turnSmoothVelocity;
    private float _buoyancyVelocityY; 
    
    // --- TRẠNG THÁI TƯƠNG TÁC ---
    private GameObject _activeInteractable; 
    private bool _isTraveling = false;             
    private bool _canControl = true; 

    // --- BIẾN HỖ TRỢ COMBO NARAKA-STYLE ---
    private bool _nextAttackQueued = false; 
    private bool _canChainCombo = false;    
    private Coroutine _combatCoroutine;     

    public bool IsTraveling => _isTraveling; 

    // =========================================================
    //                KỊCH BẢN KHỞI ĐẦU (INTRO)
    // =========================================================
    IEnumerator Start()
    {
        // 1. Dùng mẹo đợi 1 frame để đảm bảo GameSaveManager đã LoadGame() xong
        yield return null; 

        // 2. Kiểm tra: Nếu nhân vật đã có vũ khí (do SaveGame cấp lại), bỏ qua Intro
        if (model.hasSword || model.hasBow)
        {
            _canControl = true;
            if (view) view.ToggleCombatUI(true);
            
            // Ép model vũ khí hiện ra tay
            if (view) view.SwitchWeaponVisuals(model.currentWeapon);
            yield break; // Thoát hàm lập tức, không chạy Intro "tỉnh dậy" nữa
        }

        // 3. Nếu không có vũ khí (New Game thật sự), mới chạy Intro
        if (view) view.UpdateWeaponVisuals(false, false);

        _canControl = false;
        model.currentVelocity = Vector3.zero;
        if (view) view.ToggleCombatUI(false); 

        // Chạy animation thức dậy
        if (view) view.TriggerWakeUp();

        // Đợi animation chạy xong (bạn có thể điều chỉnh thời gian này nếu cần)
        yield return new WaitForSeconds(0.2f);

        _canControl = true;
        if (view) view.ToggleCombatUI(true); 
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (Camera.main) 
        {
            _camTransform = Camera.main.transform;
            if (tpsCamera == null) tpsCamera = Camera.main.GetComponent<ThirdPersonCamera>();
            if (tpsCamera == null) tpsCamera = Camera.main.GetComponentInParent<ThirdPersonCamera>();
        }
        
        if (view == null) view = GetComponent<PlayerView>();
        if (model == null) model = new PlayerModel(); 

        model.currentHealth = model.maxHealth;
        model.currentStamina = model.maxStamina;
        model.currentState = PlayerState.Idle;
        model.isSmithing = false;
        
        if (swordScript != null) swordScript.damage = model.damageSwordBase;
    }

    void Update()
    {
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing) {
            model.currentVelocity = Vector3.zero;
            if(view) view.UpdateMovementAnimation(0, 0, 0, false, false);
            return; 
        }
    
        if (!_canControl || _isTraveling || model.isSmithing || model.currentHealth <= 0 || model.currentState == PlayerState.Stunned) return;

        HandleWaterCheck();

        if (model.currentState == PlayerState.Swimming)
        {
            HandleSwimmingMovement();
            if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);
            return; 
        }

        if (Input.GetKeyDown(KeyCode.F)) HandleInteractionInput();

        HandleStaminaRegen();
        HandleWeaponSwitch(); 

        if (model.currentState != PlayerState.Dashing && model.currentState != PlayerState.ParryingRecovery)
        {
            if (model.currentWeapon == WeaponType.Sword) HandleSwordCombat();
            else if (model.currentWeapon == WeaponType.Bow) HandleBowCombat();
        }

        HandleMovement(); 
        
        if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);
    }

    // =========================================================
    //              HỆ THỐNG CHIẾN ĐẤU (KIẾM)
    // =========================================================
    void HandleSwordCombat() { 
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        // 1. PARRY
        if (Input.GetMouseButtonDown(1)) {
            if (!model.hasSword) return; 

            if (model.currentState == PlayerState.Attacking || model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving) {
                if (_combatCoroutine != null) StopCoroutine(_combatCoroutine);
                if (swordScript != null) swordScript.StopAttack(); 
                _combatCoroutine = StartCoroutine(PerformParry());
                return;
            }
        }

        // 2. ATTACK
        if (Input.GetMouseButtonDown(0)) { 
            if (!model.hasSword) return; 

            if (model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving) {
                model.currentComboStep = 1;
                _combatCoroutine = StartCoroutine(PerformAttack(1));
            }
            else if (model.currentState == PlayerState.Attacking && _canChainCombo) {
                _nextAttackQueued = true; 
            }
        }
        
        // 3. DASH (Animation Cancel - Hủy đòn chém để lướt ngay lập tức)
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) {
            if (_combatCoroutine != null) {
                StopCoroutine(_combatCoroutine);
                _combatCoroutine = null;
            }
            if (swordScript != null) swordScript.StopAttack();
            
            // Xóa sạch bộ nhớ combo hiện tại
            _nextAttackQueued = false;
            _canChainCombo = false;
            model.currentComboStep = 0;

            StartCoroutine(PerformDash()); 
        }
    }

    IEnumerator PerformAttack(int step) { 
        model.currentState = PlayerState.Attacking; 
        model.lastActionTime = Time.time; 
        
        _nextAttackQueued = false;
        _canChainCombo = false; 

        if(view) view.TriggerAttack(step); 
        RotateToCamera(); 

        float windUpTime = 0.3f, activeTime = 0.2f, recoveryTime = 0.2f;
        
        switch (step) { 
            case 1: windUpTime = 0.3f; activeTime = 0.2f; recoveryTime = 0.2f; break; 
            case 2: windUpTime = 0.3f; activeTime = 0.25f; recoveryTime = 0.2f; break; 
            case 3: windUpTime = 0.5f; activeTime = 0.4f; recoveryTime = 0.4f; break; 
        }

        yield return new WaitForSeconds(windUpTime); 
        
        yield return new WaitForSeconds(activeTime * 0.5f);
        _canChainCombo = true; 
        yield return new WaitForSeconds(activeTime * 0.5f);

        if (swordScript != null) swordScript.StopAttack();
        
        float timer = 0;
        while (timer < recoveryTime)
        {
            timer += Time.deltaTime;
            if (_nextAttackQueued && step < 3) {
                model.currentComboStep = step + 1;
                _combatCoroutine = StartCoroutine(PerformAttack(model.currentComboStep)); 
                yield break; 
            }
            yield return null;
        }

        model.currentState = PlayerState.Idle; 
        model.currentComboStep = 0; 
        _combatCoroutine = null;
    }

    // [GỌI TỪ ANIMATION EVENT] - Bật hitbox và xuất hiện VFX chém
    // [GỌI TỪ ANIMATION EVENT] - Bật hitbox và xuất hiện VFX chém
    public void TriggerSwordAttackFromAnim(int step)
    {
        if (swordScript != null)
        {
            swordScript.StartAttack(step);
        }

        GameObject vfxPrefab = null;
        switch (step)
        {
            case 1: vfxPrefab = model.vfxSlash1; break;
            case 2: vfxPrefab = model.vfxSlash2; break;
            case 3: vfxPrefab = model.vfxSlash3; break;
        }

        if (vfxPrefab != null)
        {
            // Xác định vị trí sinh ra (nếu có điểm gắn thì lấy, không thì lấy vị trí ngực nhân vật)
            Vector3 spawnPos = model.slashVfxSpawnPoint != null ? model.slashVfxSpawnPoint.position : transform.position + (Vector3.up * 1.2f);
            
            // Sinh ra VFX tại vị trí và góc quay HIỆN TẠI của nhân vật
            GameObject vfx = Instantiate(vfxPrefab, spawnPos, transform.rotation);
            
            // [QUAN TRỌNG NHẤT] Ép VFX tách ra, không làm con của bất kỳ ai
            vfx.transform.SetParent(null); 
            
            // Hủy VFX sau 1.5s để dọn rác
            Destroy(vfx, 1.5f);
        }
    }

    void RotateToCamera() { 
        if(_camTransform == null) return; 
        Vector3 camDir = _camTransform.forward; camDir.y = 0; 
        if (camDir != Vector3.zero) 
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(camDir), Time.deltaTime * model.rotationSpeed * 5f); 
    }

    IEnumerator PerformParry() { 
        model.currentState = PlayerState.Parrying; 
        
        _nextAttackQueued = false;
        _canChainCombo = false;
        model.currentComboStep = 0;

        if(view) view.TriggerParry(); 
        
        if (model.vfxParrySparks != null) { 
            GameObject vfx = Instantiate(model.vfxParrySparks, transform.position + transform.forward + Vector3.up, transform.rotation); 
            Destroy(vfx, 1.0f); 
        }

        yield return new WaitForSeconds(model.parryWindow); 
        if (model.currentState == PlayerState.Parrying) model.currentState = PlayerState.Idle; 
        _combatCoroutine = null;
    }

    // =========================================================
    //              CÁC HỆ THỐNG PHỤ
    // =========================================================
    void HandleMovement() {
        if (model.currentState == PlayerState.Dashing || model.currentState == PlayerState.Parrying || model.currentState == PlayerState.Attacking) return;
        float h = Input.GetAxisRaw("Horizontal"); float v = Input.GetAxisRaw("Vertical"); 
        Vector3 direction = new Vector3(h, 0f, v).normalized;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && model.currentStamina > 0;
        float targetSpeed = model.currentState == PlayerState.Aiming ? model.aimMoveSpeed : (isRunning ? model.runSpeed : model.walkSpeed);
        if (direction.magnitude >= 0.1f) {
            if (model.currentState != PlayerState.Aiming) {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; 
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f); 
                transform.rotation = Quaternion.Euler(0f, angle, 0f); 
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * targetSpeed, ref model.smoothDampVelocity, model.accelerationTime);
                model.currentState = PlayerState.Moving;
            } else { 
                Vector3 moveDir = _camTransform.right * h + _camTransform.forward * v; moveDir.y = 0; 
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, moveDir.normalized * targetSpeed, ref model.smoothDampVelocity, model.accelerationTime);
            }
        } else { 
            model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, Vector3.zero, ref model.smoothDampVelocity, model.decelerationTime);
            if (model.currentVelocity.magnitude < 0.1f) { model.currentVelocity = Vector3.zero; model.currentState = PlayerState.Idle; } 
        }
        _cc.Move(model.currentVelocity * Time.deltaTime);
        if (isRunning && direction.magnitude >= 0.1f && model.currentState != PlayerState.Aiming) { model.currentStamina -= 10f * Time.deltaTime; model.lastActionTime = Time.time; }
        float currentSpeedPercent = model.currentVelocity.magnitude / model.runSpeed;
        if (!isRunning && direction.magnitude >= 0.1f) currentSpeedPercent = 0.5f; else if (isRunning && direction.magnitude >= 0.1f) currentSpeedPercent = 1.0f; 
        Vector3 localVelocity = transform.InverseTransformDirection(model.currentVelocity); 
        if(view) view.UpdateMovementAnimation(currentSpeedPercent, localVelocity.x, localVelocity.z, model.currentState == PlayerState.Aiming, model.currentWeapon == WeaponType.Bow);
        if (!_cc.isGrounded) _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }

    void HandleBowCombat() {
        if (model.currentState != PlayerState.Aiming && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        bool isHoldingAim = Input.GetMouseButton(1); 
        if (isHoldingAim) {
            model.currentState = PlayerState.Aiming; 
            if(view) { view.ToggleCrosshair(true); view.SetCameraZoom(true, model.zoomFOV, model.normalFOV); view.SetAiming(true); view.SetArrowVisual(Time.time - model.lastActionTime >= model.reloadTime); }
            if (tpsCamera != null) tpsCamera.SetAiming(true); 
            if (Input.GetMouseButtonDown(0)) { if (Time.time - model.lastActionTime > model.reloadTime && model.currentArrows > 0) ShootArrow(); }
        } else {
            if (model.currentState == PlayerState.Aiming) model.currentState = PlayerState.Idle;
            if(view) { view.ToggleCrosshair(false); view.SetCameraZoom(false, model.zoomFOV, model.normalFOV); view.SetAiming(false); view.SetArrowVisual(false); }
            if (tpsCamera != null) tpsCamera.SetAiming(false); 
        }
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash());
    }

    void ShootArrow() { 
        model.lastActionTime = Time.time; model.currentArrows--; 
        if(view) view.TriggerShoot(); if(view) view.SetArrowVisual(false);
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); Vector3 targetPoint = ray.GetPoint(100f); 
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, model.interactionLayer | model.enemyLayer | 1); 
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        foreach (var hit in hits) { if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform)) { targetPoint = hit.point; break; } }
        Vector3 spawnPos = model.arrowSpawnPoint ? model.arrowSpawnPoint.position : transform.position + Vector3.up * 1.5f; 
        Vector3 dir = (targetPoint - spawnPos).normalized; 
        if (model.arrowPrefab) { GameObject arrow = Instantiate(model.arrowPrefab, spawnPos, Quaternion.LookRotation(dir)); if (arrow.TryGetComponent<Rigidbody>(out var rb)) rb.linearVelocity = dir * model.arrowShootSpeed; } 
    }

    IEnumerator PerformDash() { 
        model.currentState = PlayerState.Dashing; model.currentStamina -= model.dashCost; model.lastActionTime = Time.time; model.isInvincible = true; 
        if(view) view.TriggerDash(); 
        Vector3 dashDir = transform.forward; float h = Input.GetAxisRaw("Horizontal"); float v = Input.GetAxisRaw("Vertical"); 
        Vector3 inputDir = new Vector3(h, 0, v).normalized; 
        if (inputDir.magnitude > 0.1f) { float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; transform.rotation = Quaternion.Euler(0f, targetAngle, 0f); dashDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; }
        float timer = 0; while (timer < model.dashDuration) { _cc.Move(dashDir * model.dashForce * Time.deltaTime); timer += Time.deltaTime; yield return null; } 
        model.isInvincible = false; model.currentState = PlayerState.Idle; 
    }

    void HandleWaterCheck() { float immersionDepth = model.waterLevelY - transform.position.y; if (model.currentState != PlayerState.Swimming) { if (immersionDepth > model.swimThreshold) StartSwimming(); } else { if (immersionDepth < model.swimThreshold - 0.3f) StopSwimming(); } }
    
    void StartSwimming() 
{ 
    if (view) _savedStateBeforeSwim = view.GetCurrentVisualState(); else _savedStateBeforeSwim = 0; 
    model.currentState = PlayerState.Swimming; 
    model.currentComboStep = 0; 
    if (view) view.SetSwimming(true); 
    
    // THÊM DÒNG NÀY ĐỂ BÁO CAMERA
    if (tpsCamera != null) tpsCamera.SetSwimmingState(true);
}
    
    void StopSwimming() 
{ 
    model.currentState = PlayerState.Idle; 
    model.currentVelocity = Vector3.zero; 
    if (view) 
    { 
        if (_savedStateBeforeSwim == 1 && !model.hasSword) _savedStateBeforeSwim = 0; 
        if (_savedStateBeforeSwim == 2 && !model.hasBow) _savedStateBeforeSwim = 0; 
        view.RestoreVisualState(_savedStateBeforeSwim); 
        view.SetSwimming(false); 
    } 

    // THÊM DÒNG NÀY ĐỂ BÁO CAMERA
    if (tpsCamera != null) tpsCamera.SetSwimmingState(false);
}
    
    void HandleSwimmingMovement() { float h = Input.GetAxisRaw("Horizontal"); float v = Input.GetAxisRaw("Vertical"); Vector3 direction = new Vector3(h, 0f, v).normalized; if (view) view.SetSwimming(true); if (direction.magnitude >= 0.1f) { float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f); transform.rotation = Quaternion.Euler(0f, angle, 0f); Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; _cc.Move(moveDir * model.swimSpeed * Time.deltaTime); if (view) view.UpdateMovementAnimation(1f, 0, 0, false, false); } else { if (view) view.UpdateMovementAnimation(0f, 0, 0, false, false); } float targetY = model.waterLevelY - model.surfaceBuoyancy; float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref _buoyancyVelocityY, 0.2f); _cc.Move(new Vector3(0, newY - transform.position.y, 0)); }
    
    public void SetCurrentInteractable(GameObject obj) { _activeInteractable = obj; }
    
    public void ClearInteractable() { _activeInteractable = null; }
    
    void HandleInteractionInput() { if (_activeInteractable == null) return; if (_activeInteractable.GetComponent<BoatController>() != null) { if (MapManager.Instance != null) MapManager.Instance.ToggleMap(); } else if (_activeInteractable.CompareTag("Anvil") || _activeInteractable.GetComponent<InteractableTrigger>() != null) { EnterSmithingMode(); } }
    
    void EnterSmithingMode() { model.isSmithing = true; model.currentState = PlayerState.Idle; _canControl = false; if (view) view.ToggleSmithingUI(true, model.smithingMinigamePrefab); }
    
    public void ExitSmithingMode() { model.isSmithing = false; _canControl = true; if (view) { view.ToggleSmithingUI(false, null); view.UpdateWeaponVisuals(model.hasSword, model.hasBow); } if (Camera.main) Camera.main.gameObject.SetActive(true); Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    
    void HandleWeaponSwitch() { float scroll = Input.GetAxis("Mouse ScrollWheel"); if (scroll == 0) return; if (!model.hasSword && !model.hasBow) return; if (model.hasSword && !model.hasBow) { if (model.currentWeapon != WeaponType.Sword) { model.currentWeapon = WeaponType.Sword; if(view) view.SwitchWeaponVisuals(model.currentWeapon); } return; } if (model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving) { model.currentWeapon = (model.currentWeapon == WeaponType.Sword) ? WeaponType.Bow : WeaponType.Sword; if(view) view.SwitchWeaponVisuals(model.currentWeapon); } }
    
    void HandleStaminaRegen() { if (Time.time - model.lastActionTime > model.staminaRegenDelay && model.currentState != PlayerState.Dashing) model.currentStamina = Mathf.MoveTowards(model.currentStamina, model.maxStamina, model.staminaRegenRate * Time.deltaTime); }
    
    public HitResult TakeDamage(DamageInfo info) { if (model.isInvincible || model.currentState == PlayerState.Dashing) return HitResult.Miss; if (model.currentHealth <= 0) return HitResult.Ignored; if (model.currentState == PlayerState.Parrying) return HitResult.Parried; model.currentHealth -= info.amount; if (model.currentHealth <= 0) model.currentState = PlayerState.Dead; else { model.currentState = PlayerState.Stunned; if (view) view.TriggerStun(); Invoke(nameof(RecoverFromStun), 0.5f); } return HitResult.Hit; }
    
    void RecoverFromStun() { if (model.currentState == PlayerState.Stunned) model.currentState = PlayerState.Idle; }
    
    public void SetTravelMode(bool isTraveling) { _isTraveling = isTraveling; _cc.enabled = !isTraveling; if (isTraveling) { model.currentState = PlayerState.Idle; if (view) view.ToggleCombatUI(false); } else { if (view) view.ToggleCombatUI(true); if (view) view.SwitchWeaponVisuals(model.currentWeapon); } }
    
    public PlayerView GetView() { return view; }
    
    public void EnableMainCamera() { if (_camTransform != null) _camTransform.gameObject.SetActive(true); }
}