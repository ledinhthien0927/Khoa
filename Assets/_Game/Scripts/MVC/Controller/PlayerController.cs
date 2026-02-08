using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("MVC Components")]
    public PlayerModel model; // Dữ liệu chỉ số (Máu, Stamina, Tốc độ...)
    
    [SerializeField] private PlayerView view;
    [SerializeField] private ThirdPersonCamera tpsCamera; 
    [SerializeField] private SwordWeapon swordScript; // Script kích hoạt Trail/Damage

    private bool _wasArmedBeforeSwim = false;
    private int _savedStateBeforeSwim = 0;

    // --- BIẾN NỘI BỘ ---
    private CharacterController _cc;
    private Transform _camTransform;
    private float _turnSmoothVelocity;
    private float _buoyancyVelocityY; // Biến lực nổi khi bơi
    
    // --- TRẠNG TƯƠNG TÁC (Tối ưu hóa Trigger) ---
    private GameObject _activeInteractable; // Vật thể đang đứng gần (Được set bởi Trigger)
    private bool _isTraveling = false;             
    private bool _canControl = true; // Biến khóa input (cho Intro/Rèn)

    // --- [MỚI] BIẾN HỖ TRỢ COMBO NARAKA-STYLE ---
    private bool _nextAttackQueued = false; // Đã bấm chuột trái chờ combo tiếp theo chưa?
    private bool _canChainCombo = false;    // Đang ở trong giai đoạn cho phép nối combo?
    private Coroutine _combatCoroutine;     // Lưu coroutine để có thể Stop (Hủy chiêu)

    public bool IsTraveling => _isTraveling; 

    // =========================================================
    //                KỊCH BẢN KHỞI ĐẦU (INTRO)
    // =========================================================
    IEnumerator Start()
    {
        // 1. SETUP: Reset vũ khí về tay không
        model.hasSword = false;
        model.hasBow = false;
        model.currentWeapon = WeaponType.Sword; 

        // Update View: Ẩn vũ khí
        if (view) view.UpdateWeaponVisuals(false, false);

        // 2. KHÓA: Không cho di chuyển, Ẩn UI
        _canControl = false;
        model.currentVelocity = Vector3.zero;
        if (view) view.ToggleCombatUI(false); 

        // 3. ANIMATION: Nhân vật tỉnh dậy
        if (view) view.TriggerWakeUp();

        // 4. CHỜ: Animation chạy xong (Khoảng 4 giây)
        yield return new WaitForSeconds(4.0f);

        // 5. BẮT ĐẦU: Mở khóa điều khiển
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
        if (model == null) model = new PlayerModel(); // Tạo mới nếu thiếu

        // Reset các chỉ số runtime
        model.currentHealth = model.maxHealth;
        model.currentStamina = model.maxStamina;
        model.currentState = PlayerState.Idle;
        model.isSmithing = false;
        
        if (swordScript != null) swordScript.damage = model.damageSwordBase;
    }

    void Update()
    {
        // Nếu đang đối thoại -> Dừng mọi hoạt động
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing) {
            model.currentVelocity = Vector3.zero;
            if(view) view.UpdateMovementAnimation(0, 0, 0, false, false);
            return; 
        }
    
        // Kiểm tra điều kiện dừng: Chết, Choáng, Lái tàu, Đang Intro, Đang Rèn
        if (!_canControl || _isTraveling || model.isSmithing || model.currentHealth <= 0 || model.currentState == PlayerState.Stunned) return;

        // 1. Kiểm tra môi trường Nước
        HandleWaterCheck();

        if (model.currentState == PlayerState.Swimming)
        {
            HandleSwimmingMovement();
            if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);
            return; // Nếu đang bơi thì bỏ qua các hành động trên cạn
        }

        // 2. Xử lý Input Tương tác (F) - Dựa trên Trigger tối ưu
        if (Input.GetKeyDown(KeyCode.F)) HandleInteractionInput();

        // 3. Hồi phục Stamina & Đổi vũ khí
        HandleStaminaRegen();
        HandleWeaponSwitch(); 

        // 4. Xử lý Chiến đấu (Nếu không đang Lướt/Đỡ hồi phục)
        if (model.currentState != PlayerState.Dashing && model.currentState != PlayerState.ParryingRecovery)
        {
            if (model.currentWeapon == WeaponType.Sword) HandleSwordCombat();
            else if (model.currentWeapon == WeaponType.Bow) HandleBowCombat();
        }

        // 5. Xử lý Di chuyển
        HandleMovement(); 
        
        // 6. Update UI
        if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);
    }

    // =========================================================
    //                  HỆ THỐNG DI CHUYỂN
    // =========================================================
    void HandleMovement() {
        if (model.currentState == PlayerState.Dashing || model.currentState == PlayerState.Parrying || model.currentState == PlayerState.Attacking) return;
        
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical"); 
        Vector3 direction = new Vector3(h, 0f, v).normalized;

        // Logic Chạy (Shift)
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && model.currentStamina > 0;
        float targetSpeed = model.walkSpeed;
        
        if (model.currentState == PlayerState.Aiming) targetSpeed = model.aimMoveSpeed;
        else if (isRunning) targetSpeed = model.runSpeed;

        if (direction.magnitude >= 0.1f) {
            if (model.currentState != PlayerState.Aiming) {
                // Xoay người theo hướng Camera
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; 
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f); 
                transform.rotation = Quaternion.Euler(0f, angle, 0f); 
                
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; 
                // Smooth movement
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, moveDir * targetSpeed, ref model.smoothDampVelocity, model.accelerationTime);
                model.currentState = PlayerState.Moving;
            } else { 
                // Di chuyển kiểu Strafing khi ngắm
                Vector3 moveDir = _camTransform.right * h + _camTransform.forward * v; moveDir.y = 0; 
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, moveDir.normalized * targetSpeed, ref model.smoothDampVelocity, model.accelerationTime);
            }
        } else { 
            // Dừng lại từ từ
            model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, Vector3.zero, ref model.smoothDampVelocity, model.decelerationTime);
            if (model.currentVelocity.magnitude < 0.1f) { 
                model.currentVelocity = Vector3.zero; 
                model.currentState = PlayerState.Idle; 
            } 
        }
        
        _cc.Move(model.currentVelocity * Time.deltaTime);
        
        // Trừ Stamina khi chạy
        if (isRunning && direction.magnitude >= 0.1f && model.currentState != PlayerState.Aiming)
        {
            model.currentStamina -= 10f * Time.deltaTime; 
            model.lastActionTime = Time.time;
        }

        // Tính toán Animation Blend Tree
        float currentSpeedPercent = model.currentVelocity.magnitude / model.runSpeed;
        if (!isRunning && direction.magnitude >= 0.1f) currentSpeedPercent = 0.5f; // Walk
        else if (isRunning && direction.magnitude >= 0.1f) currentSpeedPercent = 1.0f; // Run

        Vector3 localVelocity = transform.InverseTransformDirection(model.currentVelocity); 
        if(view) view.UpdateMovementAnimation(currentSpeedPercent, localVelocity.x, localVelocity.z, model.currentState == PlayerState.Aiming, model.currentWeapon == WeaponType.Bow);
        
        // Trọng lực giả lập
        if (!_cc.isGrounded) _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }

    // =========================================================
    //              HỆ THỐNG CHIẾN ĐẤU (KIẾM) - [ĐÃ SỬA] NARAKA STYLE
    // =========================================================
    void HandleSwordCombat() { 
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        // --- 1. XỬ LÝ PARRY (Cancel Attack) ---
        // Cho phép bấm Chuột Phải để hủy đòn đánh thường ngay lập tức và đỡ đòn
        if (Input.GetMouseButtonDown(1)) {
            // [QUAN TRỌNG] Phải có kiếm mới được đỡ
            if (!model.hasSword) return;

            if (model.currentState == PlayerState.Attacking || model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving) {
                // Hủy Coroutine tấn công cũ nếu đang chạy (Cancel Animation)
                if (_combatCoroutine != null) StopCoroutine(_combatCoroutine);
                
                // Tắt hitbox kiếm ngay lập tức
                if (swordScript != null) swordScript.StopAttack(); 
                
                // Chuyển sang Parry ngay
                _combatCoroutine = StartCoroutine(PerformParry());
                return;
            }
        }

        // --- 2. XỬ LÝ TẤN CÔNG (COMBO BUFFERING) ---
        if (Input.GetMouseButtonDown(0)) { 
            // [QUAN TRỌNG] Phải có kiếm mới được đánh
            if (!model.hasSword) return;

            // Trường hợp A: Đang đứng yên hoặc chạy -> Đánh luôn đòn 1
            if (model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving) {
                model.currentComboStep = 1;
                _combatCoroutine = StartCoroutine(PerformAttack(1));
            }
            // Trường hợp B: Đang đánh và trong thời điểm cho phép nối chiêu -> Lưu lệnh (Queue)
            else if (model.currentState == PlayerState.Attacking && _canChainCombo) {
                _nextAttackQueued = true; // Game sẽ tự đánh đòn tiếp theo khi xong đòn này
            }
        }
        
        // Lướt (Space) - Cũng cho phép Cancel đòn đánh để né
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) {
            if (_combatCoroutine != null) StopCoroutine(_combatCoroutine);
            if (swordScript != null) swordScript.StopAttack();
            StartCoroutine(PerformDash()); 
        }
    }

    IEnumerator PerformAttack(int step) { 
        model.currentState = PlayerState.Attacking; 
        model.lastActionTime = Time.time; 
        
        // Reset trạng thái queue cho đòn mới
        _nextAttackQueued = false;
        _canChainCombo = false; 

        if(view) view.TriggerAttack(step); 
        RotateToCamera(); // Xoay người về phía tâm ngắm

        // --- CẤU HÌNH TIMING (Đã tinh chỉnh cho cảm giác nhanh hơn) ---
        // WindUp: Thời gian vung tay
        // Active: Thời gian gây damage
        // Recovery: Thời gian nghỉ (Cho phép bấm sẵn đòn tiếp theo ở đây)
        float windUpTime = 0.1f, activeTime = 0.2f, recoveryTime = 0.1f;
        
        switch (step) { 
            case 1: windUpTime = 0.2f; activeTime = 0.2f; recoveryTime = 0.2f; break; 
            case 2: windUpTime = 0.15f; activeTime = 0.25f; recoveryTime = 0.2f; break; 
            case 3: windUpTime = 0.4f; activeTime = 0.4f; recoveryTime = 0.3f; break; 
        }

        // Giai đoạn 1: Wind Up (Chuẩn bị)
        yield return new WaitForSeconds(windUpTime); 
        
        // Giai đoạn 2: Gây Damage
        if (swordScript != null) swordScript.StartAttack();
        
        // [QUAN TRỌNG] Cho phép nhận lệnh combo sớm một chút trước khi hết Active
        yield return new WaitForSeconds(activeTime * 0.5f);
        _canChainCombo = true; 
        yield return new WaitForSeconds(activeTime * 0.5f);

        if (swordScript != null) swordScript.StopAttack();
        
        // Giai đoạn 3: Recovery (Hồi phục & Kiểm tra Queue)
        // Đây là "Cửa sổ vàng" để nối combo
        float timer = 0;
        while (timer < recoveryTime)
        {
            timer += Time.deltaTime;
            
            // Nếu người chơi ĐÃ bấm chuột trước đó (Queue) -> Chuyển đòn luôn
            if (_nextAttackQueued && step < 3) {
                model.currentComboStep = step + 1;
                _combatCoroutine = StartCoroutine(PerformAttack(model.currentComboStep)); // Đệ quy sang đòn sau
                yield break; // Kết thúc coroutine hiện tại ngay lập tức
            }
            yield return null;
        }

        // Nếu hết thời gian mà không bấm gì -> Về Idle
        model.currentState = PlayerState.Idle; 
        model.currentComboStep = 0; // Reset combo
        _combatCoroutine = null;
    }

    void RotateToCamera() { 
        if(_camTransform == null) return; 
        Vector3 camDir = _camTransform.forward; camDir.y = 0; 
        if (camDir != Vector3.zero) 
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(camDir), Time.deltaTime * model.rotationSpeed * 5f); 
    }

    IEnumerator PerformParry() { 
        model.currentState = PlayerState.Parrying; 
        
        // Reset tất cả trạng thái combo
        _nextAttackQueued = false;
        _canChainCombo = false;
        model.currentComboStep = 0;

        if(view) view.TriggerParry(); 
        
        // Hiệu ứng Parry nếu có
        if (model.vfxParrySparks != null) { 
            GameObject vfx = Instantiate(model.vfxParrySparks, transform.position + transform.forward + Vector3.up, transform.rotation); 
            Destroy(vfx, 1.0f); 
        }

        yield return new WaitForSeconds(model.parryWindow); 
        if (model.currentState == PlayerState.Parrying) model.currentState = PlayerState.Idle; 
        _combatCoroutine = null;
    }

    // =========================================================
    //              HỆ THỐNG CHIẾN ĐẤU (CUNG)
    // =========================================================
    void HandleBowCombat() {
        if (model.currentState != PlayerState.Aiming && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        bool isHoldingAim = Input.GetMouseButton(1); 
        if (isHoldingAim) {
            model.currentState = PlayerState.Aiming; 
            if(view) {
                view.ToggleCrosshair(true); 
                view.SetCameraZoom(true, model.zoomFOV, model.normalFOV); 
                view.SetAiming(true); 
                view.SetArrowVisual(Time.time - model.lastActionTime >= model.reloadTime);
            }
            if (tpsCamera != null) tpsCamera.SetAiming(true); 

            if (Input.GetMouseButtonDown(0)) { 
                if (Time.time - model.lastActionTime > model.reloadTime && model.currentArrows > 0) ShootArrow(); 
            }
        } else {
            if (model.currentState == PlayerState.Aiming) model.currentState = PlayerState.Idle;
            if(view) { 
                view.ToggleCrosshair(false); 
                view.SetCameraZoom(false, model.zoomFOV, model.normalFOV); 
                view.SetAiming(false); 
                view.SetArrowVisual(false); 
            }
            if (tpsCamera != null) tpsCamera.SetAiming(false); 
        }
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash());
    }

    void ShootArrow() { 
        model.lastActionTime = Time.time; model.currentArrows--; 
        if(view) view.TriggerShoot(); 
        if(view) view.SetArrowVisual(false);
        
        // Raycast tìm điểm bắn
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        Vector3 targetPoint = ray.GetPoint(100f); 
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, model.interactionLayer | model.enemyLayer | 1); 
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        foreach (var hit in hits) { 
            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform)) { 
                targetPoint = hit.point; break; 
            } 
        }
        
        Vector3 spawnPos = model.arrowSpawnPoint ? model.arrowSpawnPoint.position : transform.position + Vector3.up * 1.5f; 
        Vector3 dir = (targetPoint - spawnPos).normalized; 
        
        if (model.arrowPrefab) { 
            GameObject arrow = Instantiate(model.arrowPrefab, spawnPos, Quaternion.LookRotation(dir)); 
            if (arrow.TryGetComponent<Rigidbody>(out var rb)) { 
                rb.linearVelocity = dir * model.arrowShootSpeed; 
            } 
        } 
    }

    IEnumerator PerformDash() { 
        model.currentState = PlayerState.Dashing; 
        model.currentStamina -= model.dashCost; 
        model.lastActionTime = Time.time; 
        model.isInvincible = true; 
        
        if(view) view.TriggerDash(); 
        
        Vector3 dashDir = transform.forward;
        float h = Input.GetAxisRaw("Horizontal"); float v = Input.GetAxisRaw("Vertical"); 
        Vector3 inputDir = new Vector3(h, 0, v).normalized; 
        if (inputDir.magnitude > 0.1f) { 
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; 
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f); 
            dashDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; 
        }
        
        float timer = 0; 
        while (timer < model.dashDuration) { 
            _cc.Move(dashDir * model.dashForce * Time.deltaTime); 
            timer += Time.deltaTime; 
            yield return null; 
        } 
        model.isInvincible = false; model.currentState = PlayerState.Idle; 
    }

    // =========================================================
    //              HỆ THỐNG BƠI LỘI & NƯỚC
    // =========================================================
    void HandleWaterCheck()
    {
        float immersionDepth = model.waterLevelY - transform.position.y;
        
        if (model.currentState != PlayerState.Swimming) 
        {
            // Chỉ bắt đầu bơi khi ngập sâu hơn Threshold
            if (immersionDepth > model.swimThreshold) 
                StartSwimming();
        } 
        else 
        {
            // Chỉ thoát bơi khi đã nổi lên cao (có vùng đệm 0.3f để chống nháy trạng thái)
            if (immersionDepth < model.swimThreshold - 0.3f) 
                StopSwimming();
        }
    }

    void StartSwimming() {
        // [QUAN TRỌNG] Chụp ảnh trạng thái hiện tại trước khi bơi
        if (view) 
        {
            _savedStateBeforeSwim = view.GetCurrentVisualState();
        }
        else 
        {
            _savedStateBeforeSwim = 0;
        }

        model.currentState = PlayerState.Swimming;
        model.currentComboStep = 0; 
        if (view) view.SetSwimming(true); 
    }

    void StopSwimming() {
        // 1. Reset trạng thái logic
        model.currentState = PlayerState.Idle; 
        model.currentVelocity = Vector3.zero;  
        
        if (view) 
        {
            // [QUAN TRỌNG] Set lại trạng thái Blend Tree TRƯỚC khi tắt Animation bơi
            // Logic an toàn: Nếu lỡ mất kiếm lúc bơi thì về 0
            if (_savedStateBeforeSwim == 1 && !model.hasSword) _savedStateBeforeSwim = 0;
            if (_savedStateBeforeSwim == 2 && !model.hasBow) _savedStateBeforeSwim = 0;

            // Khôi phục trạng thái cũ (Ví dụ: 0 - Unarmed)
            view.RestoreVisualState(_savedStateBeforeSwim);

            // [SAU CÙNG] Mới tắt trạng thái bơi để Animator bắt đầu chuyển đổi (Transition)
            view.SetSwimming(false); 
        }
    }

    void HandleSwimmingMovement() {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(h, 0f, v).normalized;
        
        if (view) view.SetSwimming(true);

        if (direction.magnitude >= 0.1f) {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _cc.Move(moveDir * model.swimSpeed * Time.deltaTime);
            if (view) view.UpdateMovementAnimation(1f, 0, 0, false, false);
        } else {
            if (view) view.UpdateMovementAnimation(0f, 0, 0, false, false);
        }

        // Lực nổi (Buoyancy) - Giúp nhân vật dập dềnh
        float targetY = model.waterLevelY - model.surfaceBuoyancy;
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref _buoyancyVelocityY, 0.2f);
        _cc.Move(new Vector3(0, newY - transform.position.y, 0));
    }

    // =========================================================
    //              TƯƠNG TÁC (OPTIMIZED TRIGGER)
    // =========================================================
    
    // Hàm này được gọi từ script InteractableTrigger (gắn trên object)
    public void SetCurrentInteractable(GameObject obj)
    {
        _activeInteractable = obj;
    }

    public void ClearInteractable()
    {
        _activeInteractable = null;
    }

    void HandleInteractionInput()
    {
        if (_activeInteractable == null) return;

        // Kiểm tra loại object
        if (_activeInteractable.GetComponent<BoatController>() != null) 
        {
            if (MapManager.Instance != null) MapManager.Instance.ToggleMap(); 
        }
        else if (_activeInteractable.CompareTag("Anvil") || _activeInteractable.GetComponent<InteractableTrigger>() != null)
        {
            EnterSmithingMode();
        }
    }

    void EnterSmithingMode() { 
        model.isSmithing = true; 
        model.currentState = PlayerState.Idle; 
        _canControl = false;
        if (view) view.ToggleSmithingUI(true, model.smithingMinigamePrefab); 
    }
    
    public void ExitSmithingMode() { 
        model.isSmithing = false; 
        _canControl = true;
        
        if (view) {
            view.ToggleSmithingUI(false, null); 
            // Cập nhật lại vũ khí sau khi rèn xong
            view.UpdateWeaponVisuals(model.hasSword, model.hasBow); 
        }

        if (Camera.main) Camera.main.gameObject.SetActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // =========================================================
    //                      CÁC LOGIC KHÁC
    // =========================================================
    
    void HandleWeaponSwitch() { 
        float scroll = Input.GetAxis("Mouse ScrollWheel"); 
        if (scroll == 0) return;
        
        // Chặn nếu chưa có đồ
        if (!model.hasSword && !model.hasBow) return;

        if (model.hasSword && !model.hasBow) {
            if (model.currentWeapon != WeaponType.Sword) {
                model.currentWeapon = WeaponType.Sword;
                if(view) view.SwitchWeaponVisuals(model.currentWeapon);
            }
            return;
        }

        if (model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving) { 
            model.currentWeapon = (model.currentWeapon == WeaponType.Sword) ? WeaponType.Bow : WeaponType.Sword; 
            if(view) view.SwitchWeaponVisuals(model.currentWeapon); 
        } 
    }

    void HandleStaminaRegen() { 
        if (Time.time - model.lastActionTime > model.staminaRegenDelay && model.currentState != PlayerState.Dashing) 
            model.currentStamina = Mathf.MoveTowards(model.currentStamina, model.maxStamina, model.staminaRegenRate * Time.deltaTime); 
    }

    // IDamageable
    public HitResult TakeDamage(DamageInfo info) { 
        if (model.isInvincible || model.currentState == PlayerState.Dashing) return HitResult.Miss;
        if (model.currentHealth <= 0) return HitResult.Ignored;
        if (model.currentState == PlayerState.Parrying) return HitResult.Parried;

        model.currentHealth -= info.amount;
        if (model.currentHealth <= 0) model.currentState = PlayerState.Dead; 
        else {
            model.currentState = PlayerState.Stunned;
            if (view) view.TriggerStun();
            Invoke(nameof(RecoverFromStun), 0.5f);
        }
        return HitResult.Hit;
    }
    void RecoverFromStun() { if (model.currentState == PlayerState.Stunned) model.currentState = PlayerState.Idle; }

    // Helpers
    public void SetTravelMode(bool isTraveling) {
        _isTraveling = isTraveling; 
        _cc.enabled = !isTraveling; 
        if (isTraveling) { 
            model.currentState = PlayerState.Idle; 
            if (view) view.ToggleCombatUI(false); 
        } else { 
            if (view) view.ToggleCombatUI(true); 
            if (view) view.SwitchWeaponVisuals(model.currentWeapon); 
        }
    }
    public PlayerView GetView() { return view; }
    public void EnableMainCamera() { if (_camTransform != null) _camTransform.gameObject.SetActive(true); }
}