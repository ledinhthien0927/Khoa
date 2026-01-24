using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("MVC Components")]
    [SerializeField] private PlayerModel model;
    [SerializeField] private PlayerView view;
    [SerializeField] private ThirdPersonCamera tpsCamera; 

    private CharacterController _cc;
    private Transform _camTransform;
    private float _turnSmoothVelocity;
    
    private bool _isTraveling = false;             
    private GameObject _currentInteractableObject; 
    private BoatController _nearbyBoat;            

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
        model.currentVelocity = Vector3.zero;
        
        if(view) view.SwitchWeaponVisuals(model.currentWeapon);
    }

    public PlayerView GetView() { return view; }
    
    public void SetTravelMode(bool isTraveling) 
    {
        _isTraveling = isTraveling; _cc.enabled = !isTraveling;
        if (isTraveling) { model.currentState = PlayerState.Idle; if (view) view.ToggleCombatUI(false); }
        else { if (view) view.ToggleCombatUI(true); if (view) view.SwitchWeaponVisuals(model.currentWeapon); }
    }
    
    public void EnableMainCamera() { if (_camTransform != null) _camTransform.gameObject.SetActive(true); }

    void Update()
    {
        if (_isTraveling || model.currentHealth <= 0 || model.currentState == PlayerState.Stunned) return;

        HandleInteraction();

        if (model.isSmithing) return;

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

    // --- INTERACTION ---
    void HandleInteraction() { 
        if (!model.isSmithing) { CheckForBoat(); CheckForSmithingInteractable(); }
        if (Input.GetKeyDown(KeyCode.F)) {
            if (_nearbyBoat != null) { if (MapManager.Instance != null) MapManager.Instance.ToggleMap(); return; }
            if (_currentInteractableObject != null && !model.isSmithing) EnterSmithingMode();
            else if (model.isSmithing) ExitSmithingMode();
        }
        if (model.isSmithing && Input.GetKeyDown(KeyCode.Escape)) ExitSmithingMode();
        if (Input.GetKeyDown(KeyCode.E) && !model.isSmithing) {
            Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer);
            foreach (var hit in hits) { if (hit.CompareTag("Branch")) { Destroy(hit.gameObject); model.currentArrows++; return; } }
        }
    }
    
    void CheckForBoat() { Collider[] hits = Physics.OverlapSphere(transform.position, 6.0f); BoatController found = null; foreach(var hit in hits) { found = hit.GetComponent<BoatController>(); if (found == null) found = hit.GetComponentInParent<BoatController>(); if (found != null) break; } if (found != _nearbyBoat) { if (_nearbyBoat != null) _nearbyBoat.TogglePrompt(false); if (found != null) found.TogglePrompt(true); _nearbyBoat = found; } }
    void CheckForSmithingInteractable() { if (_nearbyBoat != null) { if (_currentInteractableObject != null) ToggleObjectPrompt(_currentInteractableObject, false); _currentInteractableObject = null; return; } GameObject found = null; Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer); foreach(var hit in hits) { if(hit.CompareTag("Anvil")) { found = hit.gameObject; break; } } if (found != _currentInteractableObject) { if (_currentInteractableObject != null) ToggleObjectPrompt(_currentInteractableObject, false); if (found != null) ToggleObjectPrompt(found, true); _currentInteractableObject = found; } }
    void ToggleObjectPrompt(GameObject rootObj, bool isActive) { if (rootObj == null) return; Transform uiTransform = rootObj.transform.Find(model.interactionUIName); if (uiTransform != null) uiTransform.gameObject.SetActive(isActive); }
    void EnterSmithingMode() { model.isSmithing = true; model.currentState = PlayerState.Idle; if (view) view.ToggleSmithingUI(true, model.smithingMinigamePrefab); }
    public void ExitSmithingMode() { model.isSmithing = false; if (view) view.ToggleSmithingUI(false, null); }
    
    // --- COMBAT & MOVEMENT ---
    void HandleStaminaRegen() { if (Time.time - model.lastActionTime > model.staminaRegenDelay && model.currentState != PlayerState.Dashing) model.currentStamina = Mathf.MoveTowards(model.currentStamina, model.maxStamina, model.staminaRegenRate * Time.deltaTime); }
    
    void HandleWeaponSwitch() { 
        float scroll = Input.GetAxis("Mouse ScrollWheel"); 
        if (scroll != 0 && (model.currentState == PlayerState.Idle || model.currentState == PlayerState.Moving)) { 
            model.currentWeapon = (model.currentWeapon == WeaponType.Sword) ? WeaponType.Bow : WeaponType.Sword; 
            view.SwitchWeaponVisuals(model.currentWeapon); 
        } 
    }

    void HandleSwordCombat() { 
        if (Input.GetMouseButtonDown(0) && model.currentState != PlayerState.Parrying) { 
            if (Time.time - model.lastActionTime > model.comboResetTime) model.currentComboStep = 0; 
            if (model.currentComboStep < 3) StartCoroutine(PerformAttack(model.currentComboStep + 1)); 
        } 
        if (Input.GetMouseButtonDown(1) && model.currentState != PlayerState.Attacking) StartCoroutine(PerformParry()); 
        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash()); 
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
            RotateToCrosshair(); 
            if (tpsCamera != null) tpsCamera.SetAiming(true);

            if (Input.GetMouseButtonDown(0))
            {
                if (Time.time - model.lastActionTime > model.reloadTime && model.currentArrows > 0) 
                {
                    ShootArrow();
                }
            }
        }
        else
        {
            if (model.currentState == PlayerState.Aiming) model.currentState = PlayerState.Idle;
            view.ToggleCrosshair(false);
            view.SetCameraZoom(false, model.zoomFOV, model.normalFOV);
            view.SetAiming(false);
            if (tpsCamera != null) tpsCamera.SetAiming(false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash());
    }

    void RotateToCrosshair()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.GetPoint(100f); 
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, model.interactionLayer | model.enemyLayer | 1);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            {
                targetPoint = hit.point;
                break; 
            }
        }
        Vector3 lookDir = targetPoint - transform.position;
        lookDir.y = 0; 
        if (lookDir != Vector3.zero && lookDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, model.rotationSpeed * 5f * Time.deltaTime);
        }
    }

    IEnumerator PerformAttack(int step) { model.currentState = PlayerState.Attacking; model.currentComboStep = step; model.lastActionTime = Time.time; view.TriggerAttack(step); RotateToCamera(); yield return new WaitForSeconds(0.4f); if (model.currentState == PlayerState.Attacking) model.currentState = PlayerState.Idle; }
    IEnumerator PerformParry() { model.currentState = PlayerState.Parrying; view.TriggerParry(); yield return new WaitForSeconds(model.parryWindow); if (model.currentState == PlayerState.Parrying) model.currentState = PlayerState.Idle; }
    IEnumerator PerformDash() { model.currentState = PlayerState.Dashing; model.currentStamina -= model.dashCost; model.lastActionTime = Time.time; model.isInvincible = true; view.TriggerDash(); float h = Input.GetAxisRaw("Horizontal"); float v = Input.GetAxisRaw("Vertical"); Vector3 inputDir = new Vector3(h, 0, v).normalized; Vector3 dashDir; if (inputDir.magnitude > 0.1f) { float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y; transform.rotation = Quaternion.Euler(0f, targetAngle, 0f); dashDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward; } else dashDir = -transform.forward; float timer = 0; while (timer < model.dashDuration) { _cc.Move(dashDir * model.dashForce * Time.deltaTime); timer += Time.deltaTime; if (timer > model.dashIFrameDuration) model.isInvincible = false; yield return null; } model.isInvincible = false; model.currentState = PlayerState.Idle; }

    // --- [HÀM BẮN TÊN ĐÃ FIX LỖI RỚT TẠI CHỖ] ---
    void ShootArrow() 
    { 
        model.lastActionTime = Time.time; 
        model.currentArrows--; 
        view.TriggerShoot(); 
        
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        Vector3 targetPoint = ray.GetPoint(100f); 

        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, model.interactionLayer | model.enemyLayer | 1);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            {
                targetPoint = hit.point;
                break; 
            }
        }
        
        Vector3 spawnPos = model.arrowSpawnPoint ? model.arrowSpawnPoint.position : transform.position + Vector3.up * 1.5f; 
        Vector3 dir = (targetPoint - spawnPos).normalized; 

        if (model.arrowPrefab) 
        { 
            GameObject arrow = Instantiate(model.arrowPrefab, spawnPos, Quaternion.LookRotation(dir)); 
            
            // [FIX LỖI] Tìm TẤT CẢ Collider trên người chơi (gồm cả Cung) để bỏ qua va chạm
            if (arrow.TryGetComponent<Collider>(out var arrowCol))
            {
                Collider[] playerColliders = GetComponentsInChildren<Collider>();
                foreach (var col in playerColliders)
                {
                    Physics.IgnoreCollision(arrowCol, col);
                }
            }

            if (arrow.TryGetComponent<Rigidbody>(out var rb)) 
            {
                // Dùng AddForce với mode VelocityChange để lực bắn dứt khoát hơn
                // Đảm bảo model.arrowShootSpeed trong Inspector không bị 0
                rb.AddForce(dir * model.arrowShootSpeed, ForceMode.VelocityChange);
            }
        } 
    }
    
    void RotateToCamera() { if(_camTransform == null) return; Vector3 camDir = _camTransform.forward; camDir.y = 0; if (camDir != Vector3.zero) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(camDir), Time.deltaTime * model.rotationSpeed); }

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
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                Vector3 targetVelocity = moveDir * targetSpeed;
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, targetVelocity, ref model.smoothDampVelocity, model.accelerationTime);
                model.currentState = PlayerState.Moving;
            }
            else
            {
                Vector3 moveDir = _camTransform.right * h + _camTransform.forward * v; moveDir.y = 0;
                Vector3 targetVelocity = moveDir.normalized * targetSpeed;
                model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, targetVelocity, ref model.smoothDampVelocity, model.accelerationTime);
            }
        }
        else 
        {
            model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, Vector3.zero, ref model.smoothDampVelocity, model.decelerationTime);
            if (model.currentVelocity.magnitude < 0.1f) { model.currentVelocity = Vector3.zero; model.smoothDampVelocity = Vector3.zero; if (model.currentState == PlayerState.Moving) model.currentState = PlayerState.Idle; }
        }

        _cc.Move(model.currentVelocity * Time.deltaTime);
        float currentSpeed = new Vector3(model.currentVelocity.x, 0, model.currentVelocity.z).magnitude / model.walkSpeed;
        if (currentSpeed < 0.05f) currentSpeed = 0f;
        Vector3 localVelocity = transform.InverseTransformDirection(model.currentVelocity);
        float localX = localVelocity.x / model.aimMoveSpeed; float localZ = localVelocity.z / model.aimMoveSpeed;
        bool isBow = (model.currentWeapon == WeaponType.Bow); bool isAiming = (model.currentState == PlayerState.Aiming);
        view.UpdateMovementAnimation(currentSpeed, localX, localZ, isAiming, isBow);
        if (!_cc.isGrounded) _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }
}