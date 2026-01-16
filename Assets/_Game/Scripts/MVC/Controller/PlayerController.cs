using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("MVC Components")]
    [SerializeField] private PlayerModel model;
    [SerializeField] private PlayerView view;

    // --- CÁC BIẾN NỘI BỘ ---
    private CharacterController _characterController;
    private Transform _cameraTransform;
    private Coroutine _currentActionCoroutine;
    private Coroutine _counterWindowCoroutine;
    
    // Skill R Targeting
    private GameObject _currentIndicator;
    private Transform _currentTarget; 
    
    // Tương tác Môi trường
    private GameObject _currentInteractableObject; // Cửa/Đe rèn
    private BoatController _nearbyBoat;            // Thuyền
    
    // [MỚI] Trạng thái đi tàu (Travel Mode)
    private bool _isTraveling = false;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        
        // Tự động tìm View/Model nếu chưa gán
        if (view == null) view = GetComponent<PlayerView>();
        if (model == null) model = new PlayerModel();
        
        if (Camera.main != null) _cameraTransform = Camera.main.transform;
        
        // Khởi tạo stack tối đa cho Skill R
        model.currentRStacks = model.skillRMaxStacks;
    }

    void Start() 
    { 
        if (view != null) view.OnAttackImpact += HandleImpact; 
    }
    
    void OnDestroy() 
    { 
        if (view != null) view.OnAttackImpact -= HandleImpact; 
    }

    // --- [MỚI] CÁC HÀM PUBLIC HỖ TRỢ MAP MANAGER ---
    
    // 1. Bật lại Camera chính sau khi tàu đến nơi
    public void EnableMainCamera()
    {
        if (_cameraTransform != null) _cameraTransform.gameObject.SetActive(true);
    }

    // 2. Lấy View để MapManager điều khiển Animation (Leo lên, Lái tàu...)
    public PlayerView GetView() 
    { 
        return view; 
    }

    // 3. Chuyển đổi chế độ "Đi tàu" (Khóa/Mở khóa nhân vật)
    public void SetTravelMode(bool isTraveling)
    {
        _isTraveling = isTraveling;
        
        // Tắt CharacterController để tránh xung đột vật lý khi bị MapManager di chuyển
        _characterController.enabled = !isTraveling; 
        
        // Ẩn/Hiện UI Skill (Thanh máu, cooldown...)
        view.ToggleCombatUI(!isTraveling);

        // Nếu bắt đầu đi tàu, reset các trạng thái chiến đấu để tránh kẹt
        if (isTraveling)
        {
            model.isAttacking = false;
            model.isBlocking = false;
            model.isAimingR = false;
            view.ResumeAnimator();
            view.UpdateMovementAnimation(0, 0); // Đưa animation về Idle
            
            // Tắt indicator nhắm skill R nếu đang bật
            if (_currentIndicator != null) _currentIndicator.SetActive(false);
        }
    }

    // --- VÒNG LẶP CHÍNH ---
    void Update()
    {
        // [QUAN TRỌNG] Nếu đang đi tàu -> Dừng mọi logic Update
        if (_isTraveling) return;

        // 1. Cập nhật Cooldown và UI
        HandleSkillRCooldown();
        HandleUIUpdates(); 

        // 2. Kiểm tra tương tác (Thuyền & Rèn)
        HandleInteractionLogic();

        // 3. Nếu đang Rèn (Minigame) -> Dừng logic chiến đấu/di chuyển
        if (model.isSmithing) return;

        // 4. Logic Nhắm Skill R
        if (model.isAimingR) HandleTargeting();
        else {
            if (_currentIndicator != null) _currentIndicator.SetActive(false);
            _currentTarget = null;
        }

        // 5. Debug Keys
        if (Input.GetKeyDown(KeyCode.K)) DealDamage(5f, DamageType.Physical);
        if (Input.GetKeyDown(KeyCode.H) && model.isBlocking) OnBlockSuccess();
        
        // 6. Reset Combo
        if (Time.time > model.lastAttackTime + model.comboResetTime && model.currentComboStep > 0) 
            model.currentComboStep = 0;

        // 7. Xử lý Input Chiến đấu
        HandleInputPriority();

        // [AN TOÀN] Nếu Controller bị tắt (do MapManager chưa kịp bật lại), thoát ngay
        if (!_characterController.enabled) return;

        // 8. Xử lý Di chuyển Vật lý
        HandleMovementLogic();
    }

    // ===================================================================================
    // PHẦN 1: LOGIC TƯƠNG TÁC (Tách biệt để gọn code)
    // ===================================================================================

    void HandleInteractionLogic()
    {
        // A. QUÉT TÌM THUYỀN
        Collider[] hits = Physics.OverlapSphere(transform.position, 6.0f); 
        BoatController foundBoat = null;
        foreach (var hit in hits) {
            BoatController boat = hit.GetComponent<BoatController>();
            if (boat == null) boat = hit.GetComponentInParent<BoatController>();
            if (boat != null) { foundBoat = boat; break; }
        }

        // Cập nhật trạng thái Thuyền (Bật/Tắt Prompt)
        if (foundBoat != _nearbyBoat)
        {
            if (_nearbyBoat != null) _nearbyBoat.TogglePrompt(false); 
            if (foundBoat != null) foundBoat.TogglePrompt(true);     
            _nearbyBoat = foundBoat;
        }

        // B. QUÉT TÌM RÈN (Chỉ khi không đứng gần thuyền)
        if (_nearbyBoat == null)
        {
            CheckForSmithing();
        }
        else
        {
            // Nếu đang có thuyền, đảm bảo tắt UI rèn cũ đi
            if (_currentInteractableObject != null) ToggleObjectUI(_currentInteractableObject, false);
            _currentInteractableObject = null;
        }

        // C. XỬ LÝ INPUT "F" (Ưu tiên: Thuyền -> Rèn)
        if (Input.GetKeyDown(KeyCode.F))
        {
            // 1. Mở Map
            if (_nearbyBoat != null)
            {
                if (MapManager.Instance != null) MapManager.Instance.ToggleMap();
                return; 
            }

            // 2. Vào Rèn
            if (_currentInteractableObject != null && !model.isSmithing)
            {
                EnterSmithingMode();
            }
            // 3. Thoát Rèn
            else if (model.isSmithing)
            {
                ExitSmithingMode();
            }
        }

        // Thoát Rèn bằng ESC
        if (model.isSmithing && Input.GetKeyDown(KeyCode.Escape)) ExitSmithingMode();
    }

    void CheckForSmithing()
    {
        if (model.isSmithing) return;

        GameObject found = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, model.interactionRange, model.interactionLayer);
        if (hits.Length > 0) found = hits[0].gameObject;

        if (found != _currentInteractableObject)
        {
            if (_currentInteractableObject != null) ToggleObjectUI(_currentInteractableObject, false);
            if (found != null) ToggleObjectUI(found, true);
            _currentInteractableObject = found;
        }
    }

    void ToggleObjectUI(GameObject rootObj, bool isActive)
    {
        if (rootObj == null) return;
        Transform uiTransform = rootObj.transform.Find(model.interactionUIName); 
        if (uiTransform != null) uiTransform.gameObject.SetActive(isActive);
    }

    void EnterSmithingMode()
    {
        model.isSmithing = true;
        model.isAttacking = false;
        model.isBlocking = false;
        model.isAimingR = false;
        view.SetBlocking(false);
        view.ResumeAnimator(); 
        model.currentVelocity = Vector3.zero;
        view.ToggleSmithingUI(true, model.smithingMinigamePrefab);
    }

    public void ExitSmithingMode()
    {
        model.isSmithing = false;
        view.ToggleSmithingUI(false, null);
    }

    // ===================================================================================
    // PHẦN 2: LOGIC DI CHUYỂN
    // ===================================================================================

    void HandleMovementLogic()
    {
        if (model.isDashing || model.isAttacking || model.isBlocking || model.isAimingR || model.isSmithing)
        {
            // Dừng di chuyển ngang, nhưng vẫn giữ trọng lực rơi
            model.currentVelocity = Vector3.zero;
            model.smoothDampVelocity = Vector3.zero;
            view.UpdateMovementAnimation(0, 0);
            
            if (!_characterController.isGrounded && !model.isDashing)
                _characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
        else
        {
            // Di chuyển bình thường
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 moveDir = Vector3.zero;
            
            if (_cameraTransform != null)
            {
                Vector3 camRight = _cameraTransform.right; camRight.y = 0;
                Vector3 camForward = _cameraTransform.forward; camForward.y = 0;
                moveDir = (camRight * h + camForward * v).normalized;
            }
            
            HandleRotation(moveDir);
            HandleMovement(moveDir, h, v);
        }
    }

    void HandleRotation(Vector3 moveDir) 
    { 
        if (model.isAttacking || model.isDashing || model.isBlocking) return; 
        if (_cameraTransform == null) return; 
        if (moveDir != Vector3.zero) { 
            Quaternion targetRotation = Quaternion.LookRotation(moveDir); 
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, model.rotationSpeed * Time.deltaTime); 
        } 
    }

    void HandleMovement(Vector3 moveDir, float h, float v) 
    { 
        if (model.isAttacking || model.isDashing || model.isBlocking) return; 
        
        Vector3 targetVelocity = moveDir * model.moveSpeed; 
        float smoothTime = (moveDir.magnitude > 0) ? model.accelerationTime : model.decelerationTime; 
        
        model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, targetVelocity, ref model.smoothDampVelocity, smoothTime); 
        
        _characterController.Move(model.currentVelocity * Time.deltaTime); 
        if (!_characterController.isGrounded) _characterController.Move(Vector3.down * 9.81f * Time.deltaTime); 
        
        float speedPercent = model.currentVelocity.magnitude / model.moveSpeed; 
        if (speedPercent > 1) speedPercent = 1; 
        view.UpdateMovementAnimation(0, speedPercent); 
    }

    // ===================================================================================
    // PHẦN 3: LOGIC CHIẾN ĐẤU (GIỮ NGUYÊN)
    // ===================================================================================

    void HandleUIUpdates()
    {
        if (view == null) return;
        float dashTimeLeft = Mathf.Max(0, (model.lastDashTime + model.dashCooldown) - Time.time);
        float eTimeLeft = Mathf.Max(0, (model.lastSkillETime + model.skillECooldown) - Time.time);
        float rTimeLeft = 0;
        if (model.currentRStacks < model.skillRMaxStacks) rTimeLeft = Mathf.Max(0, model.nextRStackTime - Time.time);
        
        view.UpdateCooldowns(dashTimeLeft, model.dashCooldown, eTimeLeft, model.skillECooldown, rTimeLeft, model.skillRCooldown, model.currentRStacks);
    }

    void HandleInputPriority() 
    {
        // 1. Skill R
        if (Input.GetKeyDown(KeyCode.R)) { 
            if (model.currentRStacks > 0) { 
                model.isAimingR = true; 
                if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine); 
                if (model.isBlocking) EndBlock(); 
                view.TriggerSkillR_Prep(); 
            } else Debug.Log("Hết stack Skill R!"); 
            return; 
        }
        if (Input.GetKeyUp(KeyCode.R)) { 
            if (model.isAimingR) { 
                model.isAimingR = false; 
                view.ResumeAnimator(); 
                view.TriggerSkillR_Cancel(); 
                if (_currentIndicator != null) _currentIndicator.SetActive(false); 
            } 
        }
        if (model.isAimingR && Input.GetMouseButtonDown(0)) { 
            if (_currentTarget != null) { 
                model.isAimingR = false; 
                if (model.currentRStacks == model.skillRMaxStacks) model.nextRStackTime = Time.time + model.skillRCooldown; 
                model.currentRStacks--; 
                view.ResumeAnimator(); 
                if (_currentIndicator != null) _currentIndicator.SetActive(false); 
                StartCoroutine(PerformSkillR_Logic(_currentTarget.position)); 
            } 
            return; 
        }

        // 2. Skill E
        if (Input.GetKeyDown(KeyCode.E) && Time.time > model.lastSkillETime + model.skillECooldown) { 
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine); 
            if (model.isBlocking) EndBlock(); 
            StartCoroutine(PerformSkillE()); 
            return; 
        }

        // 3. Dash
        if (Input.GetKeyDown(KeyCode.Space) && Time.time > model.lastDashTime + model.dashCooldown) { 
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine); 
            if (model.isBlocking) EndBlock(); 
            StartCoroutine(PerformJumpSmash()); 
            return; 
        }
        if (model.isDashing) return;

        // 4. Block
        bool isHoldingBlock = Input.GetMouseButton(1);
        if (isHoldingBlock && Time.time >= model.nextBlockTime) { 
            if (!model.isBlocking) { 
                model.isBlocking = true; 
                model.blockStartTime = Time.time; 
                view.SetBlocking(true); 
                model.currentComboStep = 0; 
            } 
            if (Time.time > model.blockStartTime + model.maxBlockDuration) EndBlock(); 
        } else if (model.isBlocking) EndBlock();

        // 5. Attack
        if (Input.GetMouseButtonDown(0) && !model.isAimingR) { 
            if (model.isCounterReady) { 
                EndBlock(); 
                PerformCounterAttack(); 
                return; 
            } 
            if (!model.isBlocking) { 
                if (model.currentComboStep > 0 && Time.time < model.lastAttackTime + model.minComboDelay) return; 
                PerformComboAttack(); 
            } 
        }
    }

    // --- CÁC COROUTINE CHIẾN ĐẤU ---

    IEnumerator PerformSkillR_Logic(Vector3 targetPos) { 
        model.isAttacking = true; 
        Vector3 dirToTarget = (targetPos - transform.position).normalized; dirToTarget.y = 0; 
        if (dirToTarget != Vector3.zero) transform.rotation = Quaternion.LookRotation(dirToTarget); 
        yield return new WaitForSeconds(0.4f); 
        SpawnVFX(model.vfxSkillR_Explosion, targetPos, Quaternion.identity); 
        Collider[] hits = Physics.OverlapSphere(targetPos, model.skillRRadius, model.enemyLayer); 
        foreach (var hit in hits) { 
            IDamageable target = hit.GetComponent<IDamageable>(); 
            if (target == null) target = hit.GetComponentInParent<IDamageable>(); 
            if (target != null) { 
                float distanceToCenter = Vector3.Distance(hit.transform.position, targetPos); 
                bool isMainTarget = distanceToCenter < 1.0f; 
                float duration = isMainTarget ? model.skillRStunTimeMain : model.skillRStunTimeArea; 
                float knockForce = isMainTarget ? model.skillRKnockupForce : (model.skillRKnockupForce * 0.5f); 
                DamageInfo info = new DamageInfo { amount = model.skillRDamage, attacker = gameObject, hitPoint = hit.ClosestPoint(targetPos), hitDirection = Vector3.up, knockbackForce = knockForce, type = DamageType.UltimateR, duration = duration }; 
                target.TakeDamage(info); 
            } 
        } 
        yield return new WaitForSeconds(0.4f); 
        view.TriggerSkillR_Cancel(); 
        model.isAttacking = false; 
        _currentActionCoroutine = null; 
    }

    void HandleSkillRCooldown() { 
        if (model.currentRStacks < model.skillRMaxStacks) { 
            if (Time.time >= model.nextRStackTime) { 
                model.currentRStacks++; 
                if (model.currentRStacks < model.skillRMaxStacks) model.nextRStackTime = Time.time + model.skillRCooldown; 
            } 
        } 
    }

    void HandleTargeting() { 
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, model.enemyLayer)) { 
            _currentTarget = hit.transform; 
            if (_currentIndicator == null && model.vfxSkillR_Target != null) _currentIndicator = Instantiate(model.vfxSkillR_Target); 
            if (_currentIndicator != null) { 
                _currentIndicator.SetActive(true); 
                _currentIndicator.transform.position = _currentTarget.position + Vector3.up * 0.05f; 
                _currentIndicator.transform.rotation = Quaternion.identity; 
                float diameter = model.skillRRadius * 2.0f; 
                _currentIndicator.transform.localScale = new Vector3(diameter, 0.05f, diameter); 
            } 
        } else { 
            if (_currentIndicator != null) _currentIndicator.SetActive(false); 
            _currentTarget = null; 
        } 
    }

    IEnumerator PerformSkillE() { 
        model.isAttacking = true; 
        model.lastSkillETime = Time.time; 
        RotateToInputDirection(); 
        view.TriggerSkillE(); 
        yield return new WaitForSeconds(0.3f); 
        Quaternion vfxRotation = transform.rotation * Quaternion.Euler(model.skillEVfxRotation); 
        SpawnVFX(model.vfxSkillE, transform.position, vfxRotation); 
        Collider[] hits = Physics.OverlapSphere(transform.position, model.skillERange, model.enemyLayer); 
        foreach(var hit in hits) { 
            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized; 
            directionToTarget.y = 0; 
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget); 
            if (angleToTarget < model.skillEAngle/2) { 
                IDamageable t = hit.GetComponent<IDamageable>(); 
                if(t==null) t = hit.GetComponentInParent<IDamageable>(); 
                if(t!=null) t.TakeDamage(new DamageInfo{amount=model.damageAmount*1.5f, attacker=gameObject, hitPoint=hit.ClosestPoint(transform.position), hitDirection=Vector3.up, knockbackForce=model.skillEKnockupForce, type=DamageType.EarthUp, duration=model.skillEStunTime});
            }
        } 
        yield return new WaitForSeconds(model.skillEDuration - 0.3f); 
        model.isAttacking = false; 
        _currentActionCoroutine = null; 
    }

    void SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation) { 
        if (vfxPrefab != null) { 
            GameObject vfxObj = Instantiate(vfxPrefab, position, rotation); 
            Destroy(vfxObj, 3.0f); 
        } 
    }

    void RotateToInputDirection() { 
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical"); 
        Vector3 inputDir = Vector3.zero; 
        if (_cameraTransform != null) { 
            Vector3 camRight = _cameraTransform.right; camRight.y = 0; 
            Vector3 camForward = _cameraTransform.forward; camForward.y = 0; 
            inputDir = (camRight * h + camForward * v).normalized; 
        } 
        if (inputDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(inputDir); 
    }

    IEnumerator PerformJumpSmash() { 
        model.isDashing = true; 
        model.lastDashTime = Time.time; 
        model.currentComboStep = 0; 
        model.isAttacking = false; 
        float h = Input.GetAxisRaw("Horizontal"); 
        float v = Input.GetAxisRaw("Vertical"); 
        Vector3 inputDir = Vector3.zero; 
        if (_cameraTransform != null) { 
            Vector3 camRight = _cameraTransform.right; camRight.y = 0; 
            Vector3 camForward = _cameraTransform.forward; camForward.y = 0; 
            inputDir = (camRight * h + camForward * v).normalized; 
        } 
        if (inputDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(inputDir); 
        view.TriggerDash(); 
        yield return new WaitForSeconds(model.dashWindupTime); 
        float timer = 0; 
        Vector3 dashDirection = transform.forward; 
        while (timer < model.dashAirTime) { 
            float percent = timer / model.dashAirTime; 
            Vector3 forwardMove = dashDirection * model.dashMoveSpeed * Time.deltaTime; 
            float deltaHeight = (model.jumpCurve.Evaluate((timer + Time.deltaTime)/model.dashAirTime) - model.jumpCurve.Evaluate(timer/model.dashAirTime)) * model.jumpHeightMultiplier; 
            _characterController.Move(forwardMove + Vector3.up * deltaHeight); 
            timer += Time.deltaTime; 
            yield return null; 
        } 
        _characterController.Move(Vector3.down * 1.0f); 
        SpawnVFX(model.vfxJumpSmash, transform.position + Vector3.up * 0.1f, Quaternion.identity); 
        DealDamage(model.knockbackForces[2], DamageType.Heavy, Vector3.up); 
        yield return new WaitForSeconds(model.dashRecoveryTime); 
        model.isDashing = false; 
    }

    void PerformCounterAttack() { 
        model.isCounterReady = false; 
        model.currentComboStep = 0; 
        RotateToCameraImmediate(); 
        view.TriggerCounterAttack(); 
        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine); 
        _currentActionCoroutine = StartCoroutine(CounterAttackRoutine()); 
    }

    IEnumerator CounterAttackRoutine() { 
        model.isAttacking = true; 
        yield return new WaitForSeconds(model.counterDuration); 
        model.isAttacking = false; 
        _currentActionCoroutine = null; 
    }

    void PerformComboAttack() { 
        model.currentComboStep++; 
        if (model.currentComboStep > 3) model.currentComboStep = 1; 
        model.lastAttackTime = Time.time; 
        RotateToCameraImmediate(); 
        view.TriggerAttack(); 
        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine); 
        _currentActionCoroutine = StartCoroutine(AttackRoutine(model.currentComboStep)); 
    }

    IEnumerator AttackRoutine(int step) { 
        model.isAttacking = true; 
        float duration = 0.5f; 
        if (model.attackDurations != null && model.attackDurations.Length >= step) duration = model.attackDurations[step - 1]; 
        yield return new WaitForSeconds(duration); 
        model.isAttacking = false; 
        _currentActionCoroutine = null; 
    }

    void HandleImpact(int type) { 
        Vector3 hitPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.0f; 
        if (type == 2) { 
            SpawnVFX(model.vfxCounter, hitPos, Quaternion.LookRotation(transform.forward)); 
            DealDamage(model.knockbackForces[3], DamageType.Physical, transform.forward); 
            return; 
        } 
        if (model.currentComboStep == 3) { 
            SpawnVFX(model.vfxCombo3, hitPos, Quaternion.LookRotation(transform.forward)); 
            DealDamage(model.knockbackForces[1], DamageType.Stun); 
        } else {
            DealDamage(model.knockbackForces[0], DamageType.Physical); 
        }
    }

    void DealDamage(float force, DamageType dmgType, Vector3 directionOverride = default) { 
        Vector3 hitPos = transform.position + transform.forward * 1.0f; 
        if (model.enemyLayer.value == 0) return; 
        Collider[] hits = Physics.OverlapSphere(hitPos, model.attackRange, model.enemyLayer); 
        foreach (var hit in hits) { 
            IDamageable target = hit.GetComponent<IDamageable>(); 
            if (target == null) target = hit.GetComponentInParent<IDamageable>(); 
            if (target != null) { 
                Vector3 pushDir = (directionOverride == default) ? transform.forward : directionOverride; 
                DamageInfo info = new DamageInfo { amount = model.damageAmount, attacker = gameObject, hitPoint = hit.ClosestPoint(hitPos), hitDirection = pushDir, knockbackForce = force, type = dmgType, duration = 0.5f }; 
                target.TakeDamage(info); 
            } 
        } 
    }

    void EndBlock() { 
        model.isBlocking = false; 
        view.SetBlocking(false); 
        model.nextBlockTime = Time.time + model.blockCooldown; 
    }

    public void OnBlockSuccess() { 
        if (_counterWindowCoroutine != null) StopCoroutine(_currentActionCoroutine); 
        _counterWindowCoroutine = StartCoroutine(CounterWindowRoutine()); 
    }

    IEnumerator CounterWindowRoutine() { 
        model.isCounterReady = true; 
        yield return new WaitForSeconds(model.counterWindow); 
        model.isCounterReady = false; 
    }

    void RotateToCameraImmediate() { 
        if (_cameraTransform == null) return; 
        Vector3 camForward = _cameraTransform.forward; camForward.y = 0; 
        if (camForward != Vector3.zero) transform.rotation = Quaternion.LookRotation(camForward); 
    }
    
    private void OnDrawGizmosSelected() { 
        if (model == null) return; 
        Gizmos.color = Color.red; 
        Vector3 hitPos = transform.position + transform.forward * 1.0f; 
        Gizmos.DrawWireSphere(hitPos, model.attackRange); 
        
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(transform.position, model.interactionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 6.0f); // Tầm phát hiện thuyền
    }
}