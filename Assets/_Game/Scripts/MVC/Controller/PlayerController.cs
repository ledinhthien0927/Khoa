using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems; // Dùng để check chuột trên UI

// Kế thừa IDamageable để nhận sát thương từ quái/cá mập
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("MVC Components")]
    [SerializeField] private PlayerModel model;
    [SerializeField] private PlayerView view;
    [SerializeField] private ThirdPersonCamera tpsCamera; 

    [Header("Combat References")]
    [SerializeField] private SwordWeapon swordScript; // Script gắn trên kiếm để gây damage

    // --- BIẾN NỘI BỘ ---
    private CharacterController _cc;
    private Transform _camTransform;
    private float _turnSmoothVelocity;
    
    // Biến cho SmoothDamp lực nổi (Chống giật khi bơi)
    private float _buoyancyVelocityY; 
    
    // --- TRẠNG THÁI TƯƠNG TÁC ---
    private bool _isTraveling = false;             
    private GameObject _currentInteractableObject; 
    private BoatController _nearbyBoat;            

    // [QUAN TRỌNG] Property này để SharkManager đọc trạng thái an toàn
    public bool IsTraveling => _isTraveling; 

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        
        // Setup Camera
        if (Camera.main) 
        {
            _camTransform = Camera.main.transform;
            if (tpsCamera == null) tpsCamera = Camera.main.GetComponent<ThirdPersonCamera>();
            if (tpsCamera == null) tpsCamera = Camera.main.GetComponentInParent<ThirdPersonCamera>();
        }
        
        // Setup MVC
        if (view == null) view = GetComponent<PlayerView>();
        if (model == null) model = new PlayerModel();

        // Init Stats (Khởi tạo chỉ số)
        model.currentHealth = model.maxHealth;
        model.currentStamina = model.maxStamina;
        model.currentState = PlayerState.Idle;
        model.isSmithing = false;
        model.currentVelocity = Vector3.zero;
        
        // Cập nhật hiển thị vũ khí ban đầu
        if(view) view.SwitchWeaponVisuals(model.currentWeapon);

        // Đồng bộ damage kiếm
        if (swordScript != null) swordScript.damage = model.damageSwordBase;
    }

    // ========================================================================
    // LOGIC CHÍNH (UPDATE LOOP)
    // ========================================================================
    void Update()
    {
        // 1. Kiểm tra các điều kiện dừng (Cutscene, Chết, Choáng)
        if (_isTraveling || model.currentHealth <= 0 || model.currentState == PlayerState.Stunned) return;

        // 2. [MỚI] Kiểm tra mực nước liên tục
        HandleWaterCheck();

        if (model.isSmithing) return; // Đang rèn thì không làm gì khác

        // 3. [MỚI] Logic Bơi Lội (Nếu đang bơi thì xử lý riêng và return ngay)
        if (model.currentState == PlayerState.Swimming)
        {
            HandleSwimmingMovement();
            
            // Vẫn cập nhật UI khi bơi
            if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);
            return; 
        }

        // 4. Logic Trên Cạn (Chỉ chạy khi KHÔNG bơi)
        HandleInteraction();
        HandleStaminaRegen();
        HandleWeaponSwitch();

        // Xử lý Combat (nếu không lướt/parry)
        if (model.currentState != PlayerState.Dashing && model.currentState != PlayerState.ParryingRecovery)
        {
            if (model.currentWeapon == WeaponType.Sword) HandleSwordCombat();
            else if (model.currentWeapon == WeaponType.Bow) HandleBowCombat();
        }

        // Xử lý Di chuyển trên cạn
        HandleMovement();
        
        // Cập nhật UI
        if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);
    }

    // ========================================================================
    // PHẦN 1: HỆ THỐNG BƠI LỘI (SWIMMING SYSTEM)
    // ========================================================================
    void HandleWaterCheck()
    {
        // 1. Tính độ ngập hiện tại
        float immersionDepth = model.waterLevelY - transform.position.y;

        // 2. Logic Vùng Đệm (Hysteresis)
        if (model.currentState != PlayerState.Swimming)
        {
            // --- ĐIỀU KIỆN VÀO: Cần ngập sâu (Vẫn giữ Threshold cũ) ---
            // Ví dụ: Threshold = 1.3m (Ngập ngang ngực mới bắt đầu bơi)
            if (immersionDepth > model.swimThreshold)
            {
                StartSwimming();
            }
        }
        else
        {
            // --- ĐIỀU KIỆN RA: Phải rút cạn hẳn mới cho thoát ---
            // Ta cho phép nhân vật nổi cao hơn Threshold mà vẫn giữ trạng thái bơi.
            // exitThreshold = Threshold - 0.3m. 
            // Ví dụ: 1.3 - 0.3 = 1.0m. (Nước rút xuống thắt lưng mới đứng dậy)
            float exitThreshold = model.swimThreshold - 0.3f; 

            if (immersionDepth < exitThreshold)
            {
                StopSwimming();
            }
        }
    }

    void StartSwimming()
    {
        model.currentState = PlayerState.Swimming;
        model.currentComboStep = 0; // Reset combo nếu đang đánh dở
        if (view) view.SetSwimming(true); // Bật Animation Bơi
    }

    void StopSwimming()
    {
        model.currentState = PlayerState.Idle; // Về trạng thái đứng
        model.currentVelocity = Vector3.zero;  // Reset quán tính
        if (view) view.SetSwimming(false); // Tắt Animation Bơi
        
        // Hiện lại vũ khí khi lên bờ
        if (view) view.SwitchWeaponVisuals(model.currentWeapon);
    }

    void HandleSwimmingMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(h, 0f, v).normalized;

        // [QUAN TRỌNG] Luôn báo cho View biết đang bơi mỗi khung hình
        // Điều này ngăn Animator chuyển sang trạng thái Fall khi chân không chạm đất
        if (view) view.SetSwimming(true);

        // A. Xử lý Di chuyển ngang (XZ)
        if (direction.magnitude >= 0.1f)
        {
            // 1. Xoay nhân vật theo Camera
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // 2. Di chuyển tới
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _cc.Move(moveDir * model.swimSpeed * Time.deltaTime);
            
            // 3. Animation: Gửi Speed = 1 để Blend Tree chạy "Swim Forward"
            if (view) view.UpdateMovementAnimation(1f, 0, 0, false, false);
        }
        else
        {
            // Animation: Gửi Speed = 0 để Blend Tree chạy "Swim Idle" (Đạp nước)
            if (view) view.UpdateMovementAnimation(0f, 0, 0, false, false);
        }

        // B. Xử lý Lực nổi (Buoyancy) - [FIX JITTER: DÙNG SMOOTHDAMP]
        // Mục tiêu Y = Mặt nước - (Khoảng cách từ đỉnh đầu đến mắt)
        float targetY = model.waterLevelY - model.surfaceBuoyancy;
        Vector3 currentPos = transform.position;
        
        // Dùng SmoothDamp để loại bỏ rung lắc thay vì Lerp
        float smoothTime = 0.2f; // Thời gian để đạt độ cao mong muốn
        float newY = Mathf.SmoothDamp(currentPos.y, targetY, ref _buoyancyVelocityY, smoothTime);
        
        // Áp dụng di chuyển dọc
        Vector3 verticalMove = new Vector3(0, newY - currentPos.y, 0);
        _cc.Move(verticalMove);
    }

    // ========================================================================
    // PHẦN 2: CHIẾN ĐẤU & TƯƠNG TÁC (COMBAT & INTERACTION)
    // ========================================================================
    
    // --- Xử lý nhận sát thương (IDamageable) ---
    public HitResult TakeDamage(DamageInfo info)
    {
        // Né đòn (Dash/Invincible)
        if (model.isInvincible || model.currentState == PlayerState.Dashing) 
            return HitResult.Miss;

        // Đã chết thì thôi
        if (model.currentHealth <= 0) return HitResult.Ignored;

        // Đỡ đòn (Parry)
        if (model.currentState == PlayerState.Parrying) return HitResult.Parried;

        // Nhận damage
        model.currentHealth -= info.amount;
        
        string attackerName = (info.attacker != null) ? info.attacker.name : "Unknown Source";
        Debug.Log($"Player bị đánh bởi {attackerName}! Máu còn: {model.currentHealth}");

        // Cập nhật UI ngay lập tức
        if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);

        if (model.currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Bị choáng nhẹ
            model.currentState = PlayerState.Stunned;
            if (view) view.TriggerStun();
            Invoke(nameof(RecoverFromStun), 0.5f);
        }

        return HitResult.Hit;
    }

    void RecoverFromStun() { if (model.currentState == PlayerState.Stunned) model.currentState = PlayerState.Idle; }
    void Die() { model.currentState = PlayerState.Dead; Debug.Log("Game Over!"); }
    
    // --- Tương tác (Thuyền, Rèn, Nhặt đồ) ---
    void HandleInteraction() 
    { 
        if (!model.isSmithing) { CheckForBoat(); CheckForSmithingInteractable(); }
        
        // Phím F: Tương tác chính
        if (Input.GetKeyDown(KeyCode.F)) {
            if (_nearbyBoat != null) { 
                if (MapManager.Instance != null) MapManager.Instance.ToggleMap(); 
                return; 
            }
            if (_currentInteractableObject != null && !model.isSmithing) EnterSmithingMode();
            else if (model.isSmithing) ExitSmithingMode();
        }
        
        // Thoát chế độ rèn
        if (model.isSmithing && Input.GetKeyDown(KeyCode.Escape)) ExitSmithingMode();
        
        // Phím E: Nhặt cành cây (ví dụ)
        if (Input.GetKeyDown(KeyCode.E) && !model.isSmithing) {
            Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer);
            foreach (var hit in hits) { 
                if (hit.CompareTag("Branch")) { 
                    Destroy(hit.gameObject); 
                    model.currentArrows++; 
                    return; 
                } 
            }
        }
    }

    void CheckForBoat() { 
        Collider[] hits = Physics.OverlapSphere(transform.position, 6.0f); 
        BoatController found = null; 
        foreach(var hit in hits) { 
            found = hit.GetComponent<BoatController>(); 
            if (found == null) found = hit.GetComponentInParent<BoatController>(); 
            if (found != null) break; 
        } 
        if (found != _nearbyBoat) { 
            if (_nearbyBoat != null) _nearbyBoat.TogglePrompt(false); 
            if (found != null) found.TogglePrompt(true); 
            _nearbyBoat = found; 
        } 
    }

    void CheckForSmithingInteractable() { 
        if (_nearbyBoat != null) { 
            if (_currentInteractableObject != null) ToggleObjectPrompt(_currentInteractableObject, false); 
            _currentInteractableObject = null; 
            return; 
        } 
        GameObject found = null; 
        Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer); 
        foreach(var hit in hits) { 
            if(hit.CompareTag("Anvil")) { found = hit.gameObject; break; } 
        } 
        if (found != _currentInteractableObject) { 
            if (_currentInteractableObject != null) ToggleObjectPrompt(_currentInteractableObject, false); 
            if (found != null) ToggleObjectPrompt(found, true); 
            _currentInteractableObject = found; 
        } 
    }
    
    void ToggleObjectPrompt(GameObject rootObj, bool isActive) { 
        if (rootObj == null) return; 
        Transform uiTransform = rootObj.transform.Find(model.interactionUIName); 
        if (uiTransform != null) uiTransform.gameObject.SetActive(isActive); 
    }
    
    void EnterSmithingMode() { 
        model.isSmithing = true; 
        model.currentState = PlayerState.Idle; 
        if (view) view.ToggleSmithingUI(true, model.smithingMinigamePrefab); 
    }
    
    public void ExitSmithingMode() { 
        model.isSmithing = false; 
        if (view) view.ToggleSmithingUI(false, null); 
    }

    // --- Hồi phục Stamina ---
    void HandleStaminaRegen() { 
        if (Time.time - model.lastActionTime > model.staminaRegenDelay && model.currentState != PlayerState.Dashing) 
            model.currentStamina = Mathf.MoveTowards(model.currentStamina, model.maxStamina, model.staminaRegenRate * Time.deltaTime); 
    }

    // --- Đổi vũ khí ---
    void HandleWeaponSwitch() { 
        float scroll = Input.GetAxis("Mouse ScrollWheel"); 
        if (scroll != 0 && (model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving)) { 
            model.currentWeapon = (model.currentWeapon == WeaponType.Sword) ? WeaponType.Bow : WeaponType.Sword; 
            view.SwitchWeaponVisuals(model.currentWeapon); 
        } 
    }

    // --- KIẾM (SWORD COMBAT) ---
    void HandleSwordCombat() { 
        // Nếu chuột đè lên UI thì không đánh
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Chuột trái: Tấn công
        if (Input.GetMouseButtonDown(0) && model.currentState != PlayerState.Parrying) { 
            if (Time.time - model.lastActionTime > model.comboResetTime) model.currentComboStep = 0; 
            if (model.currentComboStep < 3) StartCoroutine(PerformAttack(model.currentComboStep + 1)); 
        } 
        
        // Chuột phải: Đỡ đòn (Parry)
        if (Input.GetMouseButtonDown(1) && model.currentState != PlayerState.Attacking) StartCoroutine(PerformParry()); 
        
        // Space: Lướt (Dash)
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash()); 
    }

    IEnumerator PerformAttack(int step) { 
        model.currentState = PlayerState.Attacking; 
        model.currentComboStep = step; 
        model.lastActionTime = Time.time; 
        
        view.TriggerAttack(step); 
        RotateToCamera(); 

        // Timing cho animation (cần chỉnh theo thực tế Animation của bạn)
        float windUpTime = 0.1f, activeTime = 0.3f, recoveryTime = 0.1f;
        switch (step) { 
            case 1: windUpTime = 0.45f; activeTime = 0.18f; break; 
            case 2: windUpTime = 0.24f; activeTime = 0.27f; break; 
            case 3: windUpTime = 0.9f; activeTime = 0.47f; break; 
        }

        yield return new WaitForSeconds(windUpTime);
        
        if (swordScript != null) swordScript.StartAttack(); // Bật Collider kiếm
        yield return new WaitForSeconds(activeTime); 
        if (swordScript != null) swordScript.StopAttack(); // Tắt Collider kiếm
        
        yield return new WaitForSeconds(recoveryTime);

        if (model.currentState == PlayerState.Attacking) model.currentState = PlayerState.Idle; 
    }

    IEnumerator PerformParry() { 
        model.currentState = PlayerState.Parrying; 
        view.TriggerParry(); 
        // Hiệu ứng tia lửa
        if (model.vfxParrySparks != null) { 
            Vector3 spawnPos = transform.position + transform.forward * 1.0f + Vector3.up * 1.2f; 
            GameObject vfx = Instantiate(model.vfxParrySparks, spawnPos, transform.rotation); 
            Destroy(vfx, 1.0f); 
        }
        yield return new WaitForSeconds(model.parryWindow); 
        if (model.currentState == PlayerState.Parrying) model.currentState = PlayerState.Idle; 
    }

    // --- CUNG (BOW COMBAT) ---
    void HandleBowCombat() {
        // Chặn bắn nếu chuột trên UI (trừ khi đang ngắm)
        if (model.currentState != PlayerState.Aiming && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool isHoldingAim = Input.GetMouseButton(1); 

        if (isHoldingAim) {
            model.currentState = PlayerState.Aiming; 
            view.ToggleCrosshair(true); 
            view.SetCameraZoom(true, model.zoomFOV, model.normalFOV); 
            view.SetAiming(true);
            
            bool isReloading = (Time.time - model.lastActionTime < model.reloadTime);
            if (!isReloading) view.SetArrowVisual(true); // Hiện mũi tên giả

            RotateToCrosshair(); 
            if (tpsCamera != null) tpsCamera.SetAiming(true); 

            // Bắn tên
            if (Input.GetMouseButtonDown(0)) { 
                if (Time.time - model.lastActionTime > model.reloadTime && model.currentArrows > 0) { 
                    ShootArrow(); 
                    view.SetArrowVisual(false); // Tắt tên giả
                } 
            }
        } else {
            // Thả chuột phải
            if (model.currentState == PlayerState.Aiming) model.currentState = PlayerState.Idle;
            view.ToggleCrosshair(false); 
            view.SetCameraZoom(false, model.zoomFOV, model.normalFOV); 
            view.SetAiming(false); 
            if (tpsCamera != null) tpsCamera.SetAiming(false); 
            view.SetArrowVisual(false);
        }

        // Lướt khi cầm cung
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash());
    }

    void ShootArrow() { 
        model.lastActionTime = Time.time; 
        model.currentArrows--; 
        view.TriggerShoot(); 
        
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
        
        // Spawn mũi tên
        Vector3 spawnPos = model.arrowSpawnPoint ? model.arrowSpawnPoint.position : transform.position + Vector3.up * 1.5f; 
        Vector3 dir = (targetPoint - spawnPos).normalized; 
        
        if (model.arrowPrefab) { 
            GameObject arrow = Instantiate(model.arrowPrefab, spawnPos, Quaternion.LookRotation(dir)); 
            
            // Ignore collision với Player
            if (arrow.TryGetComponent<Collider>(out var arrowCol)) { 
                Collider[] playerColliders = GetComponentsInChildren<Collider>(); 
                foreach (var col in playerColliders) Physics.IgnoreCollision(arrowCol, col); 
            } 
            // Bắn đi
            if (arrow.TryGetComponent<Rigidbody>(out var rb)) { 
                rb.linearVelocity = dir * model.arrowShootSpeed; 
            } 
        } 
    }

    // ========================================================================
    // PHẦN 3: DI CHUYỂN TRÊN CẠN & HELPER FUNCTIONS
    // ========================================================================

    void RotateToCrosshair() {
        if (Camera.main == null) return; 
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        Vector3 targetPoint = ray.GetPoint(100f); 
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, model.interactionLayer | model.enemyLayer | 1); 
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance)); 
        foreach (var hit in hits) { 
            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform)) { 
                targetPoint = hit.point; break; 
            } 
        }
        Vector3 lookDir = targetPoint - transform.position; lookDir.y = 0; 
        if (lookDir != Vector3.zero && lookDir.sqrMagnitude > 0.1f) { 
            Quaternion targetRotation = Quaternion.LookRotation(lookDir); 
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, model.rotationSpeed * 5f * Time.deltaTime); 
        }
    }

    void RotateToCamera() { 
        if(_camTransform == null) return; 
        Vector3 camDir = _camTransform.forward; camDir.y = 0; 
        if (camDir != Vector3.zero) 
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(camDir), Time.deltaTime * model.rotationSpeed); 
    }

    IEnumerator PerformDash() { 
        model.currentState = PlayerState.Dashing; 
        model.currentStamina -= model.dashCost; 
        model.lastActionTime = Time.time; 
        model.isInvincible = true; 
        view.TriggerDash(); 
        
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical"); 
        Vector3 inputDir = new Vector3(h, 0, v).normalized; 
        Vector3 dashDir; 
        
        if (inputDir.magnitude > 0.1f) { 
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; 
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f); 
            dashDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; 
        } else {
            dashDir = -transform.forward; // Lướt lùi nếu không bấm nút
        }
        
        float timer = 0; 
        while (timer < model.dashDuration) { 
            _cc.Move(dashDir * model.dashForce * Time.deltaTime); 
            timer += Time.deltaTime; 
            if (timer > model.dashIFrameDuration) model.isInvincible = false; 
            yield return null; 
        } 
        model.isInvincible = false; 
        model.currentState = PlayerState.Idle; 
    }
    
    void HandleMovement() {
        if (model.currentState == PlayerState.Dashing || model.currentState == PlayerState.Parrying) return;
        
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical"); 
        Vector3 direction = new Vector3(h, 0f, v).normalized;
        float targetSpeed = (model.currentState == PlayerState.Aiming) ? model.aimMoveSpeed : model.walkSpeed;
        
        // Di chuyển
        if (direction.magnitude >= 0.1f) {
            if (model.currentState != PlayerState.Aiming) {
                // Xoay theo hướng đi
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; 
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f); 
                transform.rotation = Quaternion.Euler(0f, angle, 0f); 
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; 
                Vector3 targetVelocity = moveDir * targetSpeed; 
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, targetVelocity, ref model.smoothDampVelocity, model.accelerationTime); 
                model.currentState = PlayerState.Moving;
            } else { 
                // Xoay theo Camera (Strafe) khi ngắm
                Vector3 moveDir = _camTransform.right * h + _camTransform.forward * v; moveDir.y = 0; 
                Vector3 targetVelocity = moveDir.normalized * targetSpeed; 
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, targetVelocity, ref model.smoothDampVelocity, model.accelerationTime); 
            }
        } else { 
            // Dừng lại
            model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, Vector3.zero, ref model.smoothDampVelocity, model.decelerationTime); 
            if (model.currentVelocity.magnitude < 0.1f) { 
                model.currentVelocity = Vector3.zero; 
                model.smoothDampVelocity = Vector3.zero; 
                if (model.currentState == PlayerState.Moving) model.currentState = PlayerState.Idle; 
            } 
        }
        
        _cc.Move(model.currentVelocity * Time.deltaTime);
        
        // Tính animation blend tree (cho đi bộ)
        float currentSpeed = new Vector3(model.currentVelocity.x, 0, model.currentVelocity.z).magnitude / model.walkSpeed; 
        if (currentSpeed < 0.05f) currentSpeed = 0f; 
        
        Vector3 localVelocity = transform.InverseTransformDirection(model.currentVelocity); 
        float localX = localVelocity.x / model.aimMoveSpeed; 
        float localZ = localVelocity.z / model.aimMoveSpeed; 
        
        bool isBow = (model.currentWeapon == WeaponType.Bow); 
        bool isAiming = (model.currentState == PlayerState.Aiming); 
        
        view.UpdateMovementAnimation(currentSpeed, localX, localZ, isAiming, isBow);
        
        // Trọng lực (chỉ khi không bơi)
        if (!_cc.isGrounded) _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }

    // --- Helpers cho bên ngoài gọi ---
    public PlayerView GetView() { return view; }
    
    public void SetTravelMode(bool isTraveling) 
    {
        _isTraveling = isTraveling; 
        _cc.enabled = !isTraveling; // Tắt Physics khi đi tàu
        if (isTraveling) { 
            model.currentState = PlayerState.Idle; 
            if (view) view.ToggleCombatUI(false); 
        } else { 
            if (view) view.ToggleCombatUI(true); 
            if (view) view.SwitchWeaponVisuals(model.currentWeapon); 
        }
    }
    
    public void EnableMainCamera() { if (_camTransform != null) _camTransform.gameObject.SetActive(true); }
}