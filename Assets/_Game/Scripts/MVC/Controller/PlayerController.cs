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

    // --- BIẾN NỘI BỘ ---
    private CharacterController _cc;
    private Transform _camTransform;
    private float _turnSmoothVelocity;
    private float _buoyancyVelocityY; 
    
    // --- TRẠNG THÁI ---
    private bool _isTraveling = false;             
    private GameObject _currentInteractableObject; 
    private BoatController _nearbyBoat;            
    private bool _canControl = true; 

    public bool IsTraveling => _isTraveling; 

    // --- KỊCH BẢN INTRO ---
    IEnumerator Start()
    {
        // 1. Reset
        model.hasSword = false;
        model.hasBow = false;
        model.currentWeapon = WeaponType.Sword; 

        if (view) view.UpdateWeaponVisuals(false, false);

        // Khóa điều khiển
        _canControl = false;
        model.currentVelocity = Vector3.zero;
        if (view) view.ToggleCombatUI(false); 

        // 2. Animation Dậy
        if (view) view.TriggerWakeUp();

        // 3. Chờ
        yield return new WaitForSeconds(4.0f);

        // 4. Mở khóa
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

        HandleInteraction();
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

    // --- MOVEMENT ---
    void HandleMovement() {
        if (model.currentState == PlayerState.Dashing || model.currentState == PlayerState.Parrying) return;
        
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical"); 
        Vector3 direction = new Vector3(h, 0f, v).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift) && model.currentStamina > 0;
        float targetSpeed = model.walkSpeed;
        
        if (model.currentState == PlayerState.Aiming) targetSpeed = model.aimMoveSpeed;
        else if (isRunning) targetSpeed = model.runSpeed;

        if (direction.magnitude >= 0.1f) {
            if (model.currentState != PlayerState.Aiming) {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; 
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f); 
                transform.rotation = Quaternion.Euler(0f, angle, 0f); 
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; 
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, moveDir * targetSpeed, ref model.smoothDampVelocity, model.accelerationTime);
                model.currentState = PlayerState.Moving;
            } else { 
                Vector3 moveDir = _camTransform.right * h + _camTransform.forward * v; moveDir.y = 0; 
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, moveDir.normalized * targetSpeed, ref model.smoothDampVelocity, model.accelerationTime);
            }
        } else { 
            model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, Vector3.zero, ref model.smoothDampVelocity, model.decelerationTime);
            if (model.currentVelocity.magnitude < 0.1f) { 
                model.currentVelocity = Vector3.zero; 
                model.currentState = PlayerState.Idle; 
            } 
        }
        
        _cc.Move(model.currentVelocity * Time.deltaTime);
        
        if (isRunning && direction.magnitude >= 0.1f && model.currentState != PlayerState.Aiming)
        {
            model.currentStamina -= 10f * Time.deltaTime; 
            model.lastActionTime = Time.time;
        }

        float currentSpeedPercent = model.currentVelocity.magnitude / model.runSpeed;
        if (!isRunning && direction.magnitude >= 0.1f) currentSpeedPercent = 0.5f;
        else if (isRunning && direction.magnitude >= 0.1f) currentSpeedPercent = 1.0f;

        Vector3 localVelocity = transform.InverseTransformDirection(model.currentVelocity); 
        
        if(view) view.UpdateMovementAnimation(currentSpeedPercent, localVelocity.x, localVelocity.z, model.currentState == PlayerState.Aiming, model.currentWeapon == WeaponType.Bow);
        
        if (!_cc.isGrounded) _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }

    // --- SWORD COMBAT (ĐÃ KHÔI PHỤC LOGIC CŨ) ---
    void HandleSwordCombat() { 
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        // Tấn công
        if (Input.GetMouseButtonDown(0) && model.currentState != PlayerState.Parrying) { 
            if (Time.time - model.lastActionTime > model.comboResetTime) model.currentComboStep = 0; 
            if (model.currentComboStep < 3) StartCoroutine(PerformAttack(model.currentComboStep + 1)); 
        } 
        
        // Đỡ đòn
        if (Input.GetMouseButtonDown(1) && model.currentState != PlayerState.Attacking) StartCoroutine(PerformParry()); 
        
        // Lướt
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash()); 
    }

    // [QUAN TRỌNG] Đã trả lại Switch Case để chỉnh Timing cho từng đòn như cũ
    IEnumerator PerformAttack(int step) { 
        model.currentState = PlayerState.Attacking; 
        model.currentComboStep = step; 
        model.lastActionTime = Time.time; 
        
        if(view) view.TriggerAttack(step); 
        RotateToCamera(); // Xoay người về hướng Camera khi đánh

        // --- KHÔI PHỤC THÔNG SỐ CŨ CỦA BẠN ---
        float windUpTime = 0.1f, activeTime = 0.3f, recoveryTime = 0.1f;
        switch (step) { 
            case 1: windUpTime = 0.45f; activeTime = 0.18f; break; 
            case 2: windUpTime = 0.24f; activeTime = 0.27f; break; 
            case 3: windUpTime = 0.9f; activeTime = 0.47f; break; 
        }
        // --------------------------------------

        // Chờ vung tay (Wind up)
        yield return new WaitForSeconds(windUpTime);
        
        // BẬT TRAIL & COLLIDER (Gây damage)
        if (swordScript != null) swordScript.StartAttack(); 
        
        // Thời gian gây damage (Active)
        yield return new WaitForSeconds(activeTime); 
        
        // TẮT TRAIL & COLLIDER
        if (swordScript != null) swordScript.StopAttack(); 
        
        // Thời gian hồi (Recovery)
        yield return new WaitForSeconds(recoveryTime);

        if (model.currentState == PlayerState.Attacking) model.currentState = PlayerState.Idle; 
    }

    // [KHÔI PHỤC] Hàm xoay người khi đánh (Bị thiếu ở bản trước)
    void RotateToCamera() { 
        if(_camTransform == null) return; 
        Vector3 camDir = _camTransform.forward; camDir.y = 0; 
        if (camDir != Vector3.zero) 
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(camDir), Time.deltaTime * model.rotationSpeed * 5f); 
    }

    IEnumerator PerformParry() { 
        model.currentState = PlayerState.Parrying; 
        if(view) view.TriggerParry(); 
        
        // VFX Parry
        if (model.vfxParrySparks != null) { 
            Vector3 spawnPos = transform.position + transform.forward * 1.0f + Vector3.up * 1.2f; 
            GameObject vfx = Instantiate(model.vfxParrySparks, spawnPos, transform.rotation); 
            Destroy(vfx, 1.0f); 
        }

        yield return new WaitForSeconds(model.parryWindow); 
        if (model.currentState == PlayerState.Parrying) model.currentState = PlayerState.Idle; 
    }

    // --- BOW COMBAT ---
    void HandleBowCombat() {
        if (model.currentState != PlayerState.Aiming && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        bool isHoldingAim = Input.GetMouseButton(1); 
        if (isHoldingAim) {
            model.currentState = PlayerState.Aiming; 
            if(view) {
                view.ToggleCrosshair(true); view.SetCameraZoom(true, model.zoomFOV, model.normalFOV); 
                view.SetAiming(true); view.SetArrowVisual(Time.time - model.lastActionTime >= model.reloadTime);
            }
            if (tpsCamera != null) tpsCamera.SetAiming(true); 

            if (Input.GetMouseButtonDown(0)) { 
                if (Time.time - model.lastActionTime > model.reloadTime && model.currentArrows > 0) ShootArrow(); 
            }
        } else {
            if (model.currentState == PlayerState.Aiming) model.currentState = PlayerState.Idle;
            if(view) { view.ToggleCrosshair(false); view.SetCameraZoom(false, model.zoomFOV, model.normalFOV); view.SetAiming(false); view.SetArrowVisual(false); }
            if (tpsCamera != null) tpsCamera.SetAiming(false); 
        }
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash());
    }

    void ShootArrow() { 
        model.lastActionTime = Time.time; model.currentArrows--; 
        if(view) view.TriggerShoot(); 
        if(view) view.SetArrowVisual(false);
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
            if (arrow.TryGetComponent<Collider>(out var arrowCol)) { 
                Collider[] playerColliders = GetComponentsInChildren<Collider>(); 
                foreach (var col in playerColliders) Physics.IgnoreCollision(arrowCol, col); 
            } 
            if (arrow.TryGetComponent<Rigidbody>(out var rb)) { 
                rb.linearVelocity = dir * model.arrowShootSpeed; 
            } 
        } 
    }

    IEnumerator PerformDash() { 
        model.currentState = PlayerState.Dashing; model.currentStamina -= model.dashCost; 
        model.lastActionTime = Time.time; model.isInvincible = true; 
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
        while (timer < model.dashDuration) { _cc.Move(dashDir * model.dashForce * Time.deltaTime); timer += Time.deltaTime; yield return null; } 
        model.isInvincible = false; model.currentState = PlayerState.Idle; 
    }

    // --- WEAPON SWITCH ---
    void HandleWeaponSwitch() { 
        float scroll = Input.GetAxis("Mouse ScrollWheel"); 
        if (scroll == 0) return;
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

    // --- OTHER LOGIC ---
    void HandleWaterCheck()
    {
        float immersionDepth = model.waterLevelY - transform.position.y;
        if (model.currentState != PlayerState.Swimming) {
            if (immersionDepth > model.swimThreshold) StartSwimming();
        } else {
            if (immersionDepth < model.swimThreshold - 0.3f) StopSwimming();
        }
    }

    void StartSwimming() {
        model.currentState = PlayerState.Swimming;
        model.currentComboStep = 0; 
        if (view) view.SetSwimming(true); 
    }

    void StopSwimming() {
        model.currentState = PlayerState.Idle; 
        model.currentVelocity = Vector3.zero;  
        if (view) view.SetSwimming(false); 
        if (view) view.SwitchWeaponVisuals(model.currentWeapon);
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

        float targetY = model.waterLevelY - model.surfaceBuoyancy;
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref _buoyancyVelocityY, 0.2f);
        _cc.Move(new Vector3(0, newY - transform.position.y, 0));
    }

    public HitResult TakeDamage(DamageInfo info) {
        if (model.isInvincible || model.currentState == PlayerState.Dashing) return HitResult.Miss;
        if (model.currentHealth <= 0) return HitResult.Ignored;
        if (model.currentState == PlayerState.Parrying) return HitResult.Parried;

        model.currentHealth -= info.amount;
        if(view) view.UpdateStatsUI(model.currentHealth, model.maxHealth, model.currentStamina, model.maxStamina, model.currentArrows);

        if (model.currentHealth <= 0) Die();
        else {
            model.currentState = PlayerState.Stunned;
            if (view) view.TriggerStun();
            Invoke(nameof(RecoverFromStun), 0.5f);
        }
        return HitResult.Hit;
    }
    void RecoverFromStun() { if (model.currentState == PlayerState.Stunned) model.currentState = PlayerState.Idle; }
    void Die() { model.currentState = PlayerState.Dead; }

    void HandleInteraction() { 
        CheckForBoat(); CheckForSmithingInteractable();
        if (Input.GetKeyDown(KeyCode.F)) {
            if (_nearbyBoat != null) { 
                if (MapManager.Instance != null) MapManager.Instance.ToggleMap(); 
                return; 
            }
            if (_currentInteractableObject != null) EnterSmithingMode();
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
            _currentInteractableObject = null; return; 
        } 
        GameObject found = null; 
        Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer); 
        foreach(var hit in hits) { if(hit.CompareTag("Anvil")) { found = hit.gameObject; break; } } 
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
        model.isSmithing = true; model.currentState = PlayerState.Idle; 
        _canControl = false;
        if (view) view.ToggleSmithingUI(true, model.smithingMinigamePrefab); 
    }
    
    public void ExitSmithingMode() { 
        model.isSmithing = false; 
        _canControl = true;
        
        if (view) {
            view.ToggleSmithingUI(false, null); 
            view.UpdateWeaponVisuals(model.hasSword, model.hasBow); 
        }

        if (Camera.main) Camera.main.gameObject.SetActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void SetTravelMode(bool isTraveling) {
        _isTraveling = isTraveling; _cc.enabled = !isTraveling; 
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

    void HandleStaminaRegen() { 
        if (Time.time - model.lastActionTime > model.staminaRegenDelay && model.currentState != PlayerState.Dashing) 
            model.currentStamina = Mathf.MoveTowards(model.currentStamina, model.maxStamina, model.staminaRegenRate * Time.deltaTime); 
    }
}