using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("MVC Components")]
    [SerializeField] private PlayerModel model;
    [SerializeField] private PlayerView view;

    // --- CÁC BIẾN NỘI BỘ ---
    private CharacterController _cc;
    private Transform _camTransform;
    private float _turnSmoothVelocity; 
    
    // TRẠNG THÁI HỆ THỐNG
    private bool _isTraveling = false;             // Đang đi tàu
    private GameObject _currentInteractableObject; // Bàn rèn đang đứng gần
    private BoatController _nearbyBoat;            // [QUAN TRỌNG] Tàu đang đứng gần (Biến bị thiếu trước đó)

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (Camera.main) _camTransform = Camera.main.transform;
        
        if (view == null) view = GetComponent<PlayerView>();
        if (model == null) model = new PlayerModel();

        model.currentHealth = model.maxHealth;
        model.currentStamina = model.maxStamina;
        model.currentState = PlayerState.Idle;
        model.isSmithing = false;
        
        if(view) view.SwitchWeaponVisuals(model.currentWeapon);
    }

    // ========================================================================
    // PHẦN 1: HỖ TRỢ MAP MANAGER 
    // ========================================================================

    public PlayerView GetView() { return view; }

    public void SetTravelMode(bool isTraveling)
    {
        _isTraveling = isTraveling;
        _cc.enabled = !isTraveling; 

        if (isTraveling)
        {
            model.currentState = PlayerState.Idle;
            if (view) view.ToggleCombatUI(false); 
        }
        else
        {
            if (view) view.ToggleCombatUI(true);  
            if (view) view.SwitchWeaponVisuals(model.currentWeapon);
        }
    }

    public void EnableMainCamera()
    {
        if (_camTransform != null) _camTransform.gameObject.SetActive(true);
    }

    // ========================================================================
    // PHẦN 2: VÒNG LẶP CHÍNH (UPDATE)
    // ========================================================================

    void Update()
    {
        // 1. Chặn logic nếu đang đi tàu, chết hoặc bị choáng
        if (_isTraveling || model.currentHealth <= 0 || model.currentState == PlayerState.Stunned) return;

        // 2. Xử lý Tương tác (Tàu, Rèn, Nhặt đồ)
        HandleInteraction();

        // Nếu đang ngồi Rèn -> Dừng mọi việc khác
        if (model.isSmithing) return;

        // 3. Logic khác (Stamina, Vũ khí, Di chuyển)
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

    // ========================================================================
    // PHẦN 3: LOGIC TƯƠNG TÁC (TÀU + RÈN + NHẶT ĐỒ)
    // ========================================================================

    void HandleInteraction()
    {
        // A. QUÉT TÌM TÀU & RÈN (Liên tục kiểm tra xung quanh)
        if (!model.isSmithing) 
        {
            CheckForBoat();                 // [MỚI] Tìm tàu
            CheckForSmithingInteractable(); // Tìm bàn rèn
        }

        // B. XỬ LÝ PHÍM 'F' (Ưu tiên: Tàu -> Rèn)
        if (Input.GetKeyDown(KeyCode.F))
        {
            // Ưu tiên 1: Nếu đang đứng gần Tàu -> Mở Map
            if (_nearbyBoat != null)
            {
                if (MapManager.Instance != null) MapManager.Instance.ToggleMap();
                return; // Thoát ngay, không check rèn nữa
            }

            // Ưu tiên 2: Vào Rèn
            if (_currentInteractableObject != null && !model.isSmithing) 
            {
                EnterSmithingMode();
            }
            // Ưu tiên 3: Thoát Rèn
            else if (model.isSmithing) 
            {
                ExitSmithingMode();
            }
        }

        // C. PHÍM 'ESC' (Thoát Rèn)
        if (model.isSmithing && Input.GetKeyDown(KeyCode.Escape)) ExitSmithingMode();

        // D. PHÍM 'E' (Nhặt cành cây)
        if (Input.GetKeyDown(KeyCode.E) && !model.isSmithing)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Branch")) 
                { 
                    Destroy(hit.gameObject); 
                    model.currentArrows++; 
                    return; 
                }
            }
        }
    }

    // --- [MỚI] HÀM TÌM TÀU ---
    void CheckForBoat()
    {
        // Quét bán kính lớn hơn (6m) để dễ tìm tàu
        Collider[] hits = Physics.OverlapSphere(transform.position, 6.0f); 
        BoatController found = null;
        
        foreach(var hit in hits) {
            // Tìm script BoatController trên vật thể hoặc cha của nó
            found = hit.GetComponent<BoatController>();
            if (found == null) found = hit.GetComponentInParent<BoatController>();
            
            if (found != null) break; // Thấy tàu rồi!
        }

        // Logic hiển thị UI "Bấm F lái tàu"
        if (found != _nearbyBoat)
        {
            if (_nearbyBoat != null) _nearbyBoat.TogglePrompt(false); // Tắt UI tàu cũ
            if (found != null) found.TogglePrompt(true);              // Bật UI tàu mới
            _nearbyBoat = found;
        }
    }

    // --- HÀM TÌM BÀN RÈN ---
    void CheckForSmithingInteractable()
    {
        // Nếu đang đứng gần tàu, thì tạm thời bỏ qua bàn rèn để tránh lẫn lộn UI
        if (_nearbyBoat != null) 
        {
            if (_currentInteractableObject != null) ToggleObjectPrompt(_currentInteractableObject, false);
            _currentInteractableObject = null;
            return;
        }

        GameObject found = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer);
        
        foreach(var hit in hits) {
            if(hit.CompareTag("Anvil")) { found = hit.gameObject; break; }
        }

        if (found != _currentInteractableObject)
        {
            if (_currentInteractableObject != null) ToggleObjectPrompt(_currentInteractableObject, false);
            if (found != null) ToggleObjectPrompt(found, true);
            _currentInteractableObject = found;
        }
    }

    void ToggleObjectPrompt(GameObject rootObj, bool isActive)
    {
        if (rootObj == null) return;
        Transform uiTransform = rootObj.transform.Find(model.interactionUIName); 
        if (uiTransform != null) uiTransform.gameObject.SetActive(isActive);
    }

    void EnterSmithingMode()
    {
        model.isSmithing = true;
        model.currentState = PlayerState.Idle; 
        if (view) view.ToggleSmithingUI(true, model.smithingMinigamePrefab);
    }

    public void ExitSmithingMode()
    {
        model.isSmithing = false;
        if (view) view.ToggleSmithingUI(false, null);
    }

    // ========================================================================
    // PHẦN 4: CHIẾN ĐẤU & DI CHUYỂN (GIỮ NGUYÊN)
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
        if (Input.GetMouseButtonDown(0) && model.currentState != PlayerState.Parrying)
        {
            if (Time.time - model.lastActionTime > model.comboResetTime) model.currentComboStep = 0;
            if (model.currentComboStep < 3) StartCoroutine(PerformAttack(model.currentComboStep + 1));
        }
        if (Input.GetMouseButtonDown(1) && model.currentState != PlayerState.Attacking)
        {
            StartCoroutine(PerformParry());
        }
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
                if (Time.time - model.lastActionTime > model.reloadTime && model.currentArrows > 0) ShootArrow();
            }
        }
        else
        {
            if (model.currentState == PlayerState.Aiming) model.currentState = PlayerState.Idle;
            view.ToggleCrosshair(false);
            view.SetCameraZoom(false, model.zoomFOV, model.normalFOV);
            view.SetAiming(false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && model.currentStamina >= model.dashCost) StartCoroutine(PerformDash());
    }

    IEnumerator PerformAttack(int step)
    {
        model.currentState = PlayerState.Attacking;
        model.currentComboStep = step;
        model.lastActionTime = Time.time;
        view.TriggerAttack(step);
        RotateToCamera();
        yield return new WaitForSeconds(0.4f); 
        if (model.currentState == PlayerState.Attacking) model.currentState = PlayerState.Idle;
    }

    IEnumerator PerformParry()
    {
        model.currentState = PlayerState.Parrying;
        view.TriggerParry();
        yield return new WaitForSeconds(model.parryWindow); 
        if (model.currentState == PlayerState.Parrying) model.currentState = PlayerState.Idle;
    }

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
        Vector3 dashDir;

        if (inputDir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            dashDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        else dashDir = -transform.forward; 

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

    void ShootArrow()
    {
        model.lastActionTime = Time.time;
        model.currentArrows--;
        view.TriggerShoot();
        Vector3 aimCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 100f));
        Vector3 spawnPos = model.arrowSpawnPoint ? model.arrowSpawnPoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 dir = (aimCenter - spawnPos).normalized;
        if (model.arrowPrefab) {
            GameObject arrow = Instantiate(model.arrowPrefab, spawnPos, Quaternion.LookRotation(dir));
            if (arrow.TryGetComponent<Rigidbody>(out var rb)) rb.linearVelocity = dir * 40f; 
        }
    }
    
    void RotateToCamera()
    {
        if(_camTransform == null) return;
        Vector3 camDir = _camTransform.forward; camDir.y = 0;
        if (camDir != Vector3.zero) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(camDir), Time.deltaTime * model.rotationSpeed);
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
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, 0.1f);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                _cc.Move(Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * targetSpeed * Time.deltaTime);
                model.currentState = PlayerState.Moving;
            }
            else
            {
                Vector3 moveDir = _camTransform.right * h + _camTransform.forward * v; moveDir.y = 0;
                _cc.Move(moveDir.normalized * targetSpeed * Time.deltaTime);
            }
        }
        else if (model.currentState == PlayerState.Moving) model.currentState = PlayerState.Idle;

        view.UpdateMovementAnim(new Vector3(_cc.velocity.x, 0, _cc.velocity.z).magnitude / model.walkSpeed, model.currentState == PlayerState.Aiming);
        if (!_cc.isGrounded) _cc.Move(Vector3.down * 9.8f * Time.deltaTime);
    }
}