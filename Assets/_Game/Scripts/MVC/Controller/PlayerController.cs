using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("MVC Components")]
    [SerializeField] private PlayerModel model;
    [SerializeField] private PlayerView view;

    private CharacterController _characterController;
    private Transform _cameraTransform;
    private Coroutine _currentActionCoroutine;
    private Coroutine _counterWindowCoroutine;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (view == null) view = GetComponent<PlayerView>();
        if (model == null) model = new PlayerModel();
        if (Camera.main != null) _cameraTransform = Camera.main.transform;
    }

    void Start() { if (view != null) view.OnAttackImpact += HandleImpact; }
    void OnDestroy() { if (view != null) view.OnAttackImpact -= HandleImpact; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) DealDamage(5f, DamageType.Physical);
        if (Input.GetKeyDown(KeyCode.H) && model.isBlocking) OnBlockSuccess();

        if (Time.time > model.lastAttackTime + model.comboResetTime && model.currentComboStep > 0)
            model.currentComboStep = 0;

        HandleInputPriority();

        if (model.isDashing || model.isAttacking || model.isBlocking)
        {
            model.currentVelocity = Vector3.zero;
            model.smoothDampVelocity = Vector3.zero;
            view.UpdateMovementAnimation(0, 0);
            if (!_characterController.isGrounded && !model.isDashing)
                _characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
        else
        {
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

    void SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
    {
        if (vfxPrefab != null) { GameObject vfxObj = Instantiate(vfxPrefab, position, rotation); Destroy(vfxObj, 3.0f); }
    }

    void HandleInputPriority()
    {
        // 1. SKILL E
        if (Input.GetKeyDown(KeyCode.E) && Time.time > model.lastSkillETime + model.skillECooldown)
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            if (model.isBlocking) EndBlock();
            StartCoroutine(PerformSkillE());
            return;
        }

        // 2. DASH
        if (Input.GetKeyDown(KeyCode.Space) && Time.time > model.lastDashTime + model.dashCooldown)
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            if (model.isBlocking) EndBlock();
            StartCoroutine(PerformJumpSmash());
            return;
        }
        if (model.isDashing) return;

        // 3. BLOCK
        bool isHoldingBlock = Input.GetMouseButton(1);
        if (isHoldingBlock && Time.time >= model.nextBlockTime)
        {
            if (!model.isBlocking)
            {
                model.isBlocking = true;
                model.blockStartTime = Time.time;
                view.SetBlocking(true);
                model.currentComboStep = 0;
            }
            if (Time.time > model.blockStartTime + model.maxBlockDuration) EndBlock();
        }
        else if (model.isBlocking) EndBlock();

        // 4. ATTACK
        if (Input.GetMouseButtonDown(0))
        {
            if (model.isCounterReady)
            {
                EndBlock();
                PerformCounterAttack();
                return;
            }
            if (!model.isBlocking)
            {
                if (model.currentComboStep > 0 && Time.time < model.lastAttackTime + model.minComboDelay) return;
                PerformComboAttack();
            }
        }
    }

    // --- LOGIC SKILL E (HITBOX CHỮ V) ---
    IEnumerator PerformSkillE()
    {
        model.isAttacking = true;
        model.lastSkillETime = Time.time;
        
        // 1. [SỬA LỖI XOAY] Xoay theo hướng phím bấm (WASD) thay vì ép theo Camera
        RotateToInputDirection(); 
        
        view.TriggerSkillE(); 

        yield return new WaitForSeconds(0.3f); // Thời gian chờ vung kiếm

        // 2. [SỬA LỖI VFX] Áp dụng góc xoay bù trừ (Offset) từ Model
        // transform.rotation: Hướng nhân vật
        // Quaternion.Euler(model.skillEVfxRotation): Góc xoay sửa lỗi (ví dụ -90 độ)
        Quaternion vfxRotation = transform.rotation * Quaternion.Euler(model.skillEVfxRotation);
        
        SpawnVFX(model.vfxSkillE, transform.position, vfxRotation);

        // 3. LOGIC HITBOX (GIỮ NGUYÊN)
        Collider[] hits = Physics.OverlapSphere(transform.position, model.skillERange, model.enemyLayer);
        
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
                directionToTarget.y = 0; 
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget < model.skillEAngle / 2)
                {
                    IDamageable target = hit.GetComponent<IDamageable>();
                    if (target == null) target = hit.GetComponentInParent<IDamageable>();

                    if (target != null)
                    {
                        DamageInfo info = new DamageInfo
                        {
                            amount = model.damageAmount * 1.5f,
                            attacker = gameObject,
                            hitPoint = hit.ClosestPoint(transform.position),
                            hitDirection = Vector3.up,
                            knockbackForce = model.skillEKnockupForce,
                            type = DamageType.EarthUp,
                            duration = model.skillEStunTime
                        };
                        target.TakeDamage(info);
                    }
                }
            }
        }

        yield return new WaitForSeconds(model.skillEDuration - 0.3f);
        model.isAttacking = false;
        _currentActionCoroutine = null;
    }

    // --- [HÀM MỚI] XOAY THEO INPUT ---
    // Hàm này giúp nhân vật xoay về phía bạn đang bấm nút (WASD) thay vì Camera
    void RotateToInputDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = Vector3.zero;

        if (_cameraTransform != null)
        {
            Vector3 camRight = _cameraTransform.right; camRight.y = 0;
            Vector3 camForward = _cameraTransform.forward; camForward.y = 0;
            inputDir = (camRight * h + camForward * v).normalized;
        }

        // Chỉ xoay nếu người chơi đang bấm nút di chuyển
        if (inputDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(inputDir);
        }
        // Nếu không bấm gì, giữ nguyên hướng đang đứng (không bị reset về hướng lạ)
    }

    //

    // --- CÁC HÀM KHÁC (GIỮ NGUYÊN) ---

    void PerformComboAttack()
    {
        model.currentComboStep++;
        if (model.currentComboStep > 3) model.currentComboStep = 1;
        model.lastAttackTime = Time.time;
        RotateToCameraImmediate();
        view.TriggerAttack();
        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
        _currentActionCoroutine = StartCoroutine(AttackRoutine(model.currentComboStep));
    }

    IEnumerator PerformJumpSmash()
    {
        model.isDashing = true;
        model.lastDashTime = Time.time;
        model.currentComboStep = 0;
        model.isAttacking = false;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = Vector3.zero;
        if (_cameraTransform != null)
        {
            Vector3 camRight = _cameraTransform.right; camRight.y = 0;
            Vector3 camForward = _cameraTransform.forward; camForward.y = 0;
            inputDir = (camRight * h + camForward * v).normalized;
        }
        if (inputDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(inputDir);

        view.TriggerDash();
        yield return new WaitForSeconds(model.dashWindupTime);

        float timer = 0;
        Vector3 dashDirection = transform.forward;
        while (timer < model.dashAirTime)
        {
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

    void PerformCounterAttack()
    {
        model.isCounterReady = false; 
        model.currentComboStep = 0;
        RotateToCameraImmediate();
        view.TriggerCounterAttack(); 
        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
        _currentActionCoroutine = StartCoroutine(CounterAttackRoutine());
    }

    IEnumerator CounterAttackRoutine()
    {
        model.isAttacking = true;
        yield return new WaitForSeconds(model.counterDuration);
        model.isAttacking = false;
        _currentActionCoroutine = null;
    }

    void HandleImpact(int type)
    {
        Vector3 hitPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;
        if (type == 2)
        {
            SpawnVFX(model.vfxCounter, hitPos, Quaternion.LookRotation(transform.forward));
            DealDamage(model.knockbackForces[3], DamageType.Physical, transform.forward);
            return;
        }
        if (model.currentComboStep == 3) 
        {
            SpawnVFX(model.vfxCombo3, hitPos, Quaternion.LookRotation(transform.forward));
            DealDamage(model.knockbackForces[1], DamageType.Stun);
        }
        else DealDamage(model.knockbackForces[0], DamageType.Physical);
    }

    void DealDamage(float force, DamageType dmgType, Vector3 directionOverride = default)
    {
        Vector3 hitPos = transform.position + transform.forward * 1.0f;
        if (model.enemyLayer.value == 0) return;

        Collider[] hits = Physics.OverlapSphere(hitPos, model.attackRange, model.enemyLayer);
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null) target = hit.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                Vector3 pushDir = (directionOverride == default) ? transform.forward : directionOverride;
                DamageInfo info = new DamageInfo
                {
                    amount = model.damageAmount,
                    attacker = gameObject,
                    hitPoint = hit.ClosestPoint(hitPos),
                    hitDirection = pushDir,
                    knockbackForce = force,
                    type = dmgType,
                    duration = 0.5f 
                };
                target.TakeDamage(info);
            }
        }
    }

    void EndBlock() { model.isBlocking = false; view.SetBlocking(false); model.nextBlockTime = Time.time + model.blockCooldown; }
    public void OnBlockSuccess() { if (_counterWindowCoroutine != null) StopCoroutine(_counterWindowCoroutine); _counterWindowCoroutine = StartCoroutine(CounterWindowRoutine()); }
    IEnumerator CounterWindowRoutine() { model.isCounterReady = true; yield return new WaitForSeconds(model.counterWindow); model.isCounterReady = false; }
    IEnumerator AttackRoutine(int step)
    {
        model.isAttacking = true;
        float duration = 0.5f;
        if (model.attackDurations != null && model.attackDurations.Length >= step) duration = model.attackDurations[step - 1];
        yield return new WaitForSeconds(duration);
        model.isAttacking = false;
        _currentActionCoroutine = null;
    }

    void HandleRotation(Vector3 moveDir) { if (model.isAttacking || model.isDashing || model.isBlocking) return; if (_cameraTransform == null) return; if (moveDir != Vector3.zero) { Quaternion targetRotation = Quaternion.LookRotation(moveDir); transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, model.rotationSpeed * Time.deltaTime); } }
    void HandleMovement(Vector3 moveDir, float h, float v) { if (model.isAttacking || model.isDashing || model.isBlocking) return; Vector3 targetVelocity = moveDir * model.moveSpeed; float smoothTime = (moveDir.magnitude > 0) ? model.accelerationTime : model.decelerationTime; model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, targetVelocity, ref model.smoothDampVelocity, smoothTime); _characterController.Move(model.currentVelocity * Time.deltaTime); if (!_characterController.isGrounded) _characterController.Move(Vector3.down * 9.81f * Time.deltaTime); float speedPercent = model.currentVelocity.magnitude / model.moveSpeed; if (speedPercent > 1) speedPercent = 1; view.UpdateMovementAnimation(0, speedPercent); }
    void RotateToCameraImmediate() { if (_cameraTransform == null) return; Vector3 camForward = _cameraTransform.forward; camForward.y = 0; if (camForward != Vector3.zero) transform.rotation = Quaternion.LookRotation(camForward); }

    // --- VẼ GIZMOS ĐỂ BẠN CĂN CHỈNH ---
    private void OnDrawGizmosSelected()
    {
        if (model == null) return;

        // 1. Vẽ Hitbox đánh thường (Đỏ)
        Gizmos.color = Color.red;
        Vector3 hitPos = transform.position + transform.forward * 1.0f;
        Gizmos.DrawWireSphere(hitPos, model.attackRange);
        
        // 2. Vẽ Hitbox Skill E hình chữ V (Xanh lá)
        Gizmos.color = Color.green;
        Vector3 startPos = transform.position;
        
        // Vẽ 2 cạnh của chữ V
        Quaternion leftRayRotation = Quaternion.AngleAxis(-model.skillEAngle / 2, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(model.skillEAngle / 2, Vector3.up);
        
        Vector3 leftRayDirection = leftRayRotation * transform.forward;
        Vector3 rightRayDirection = rightRayRotation * transform.forward;

        Gizmos.DrawRay(startPos, leftRayDirection * model.skillERange);
        Gizmos.DrawRay(startPos, rightRayDirection * model.skillERange);
        
        // Vẽ cung tròn nối 2 đầu (để hình dung vùng quét)
        Gizmos.DrawLine(startPos + leftRayDirection * model.skillERange, startPos + rightRayDirection * model.skillERange);
    }
}