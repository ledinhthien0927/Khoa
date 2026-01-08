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
        else Debug.LogError("LỖI: Không tìm thấy MainCamera!");
    }

    void Start()
    {
        if (view != null) view.OnAttackImpact += HandleImpact;
    }

    void OnDestroy()
    {
        if (view != null) view.OnAttackImpact -= HandleImpact;
    }

    void Update()
    {
        // Debug
        if (Input.GetKeyDown(KeyCode.K)) DealDamage(5f, DamageType.Physical);
        if (Input.GetKeyDown(KeyCode.H) && model.isBlocking) OnBlockSuccess();

        // 1. Reset Combo
        if (Time.time > model.lastAttackTime + model.comboResetTime && model.currentComboStep > 0)
        {
            model.currentComboStep = 0;
        }

        // 2. Input
        HandleInputPriority();

        // 3. Logic Di chuyển
        if (model.isDashing || model.isAttacking || model.isBlocking)
        {
            model.currentVelocity = Vector3.zero;
            model.smoothDampVelocity = Vector3.zero;
            view.UpdateMovementAnimation(0, 0);

            if (!_characterController.isGrounded && !model.isDashing)
            {
                _characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
            }
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

    // --- HỆ THỐNG VFX (HIỆU ỨNG) ---
    void SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
    {
        if (vfxPrefab != null)
        {
            GameObject vfxObj = Instantiate(vfxPrefab, position, rotation);
            Destroy(vfxObj, 2.0f); // Tự hủy sau 2 giây để đỡ nặng máy
        }
    }

    // --- INPUT SYSTEM ---
    void HandleInputPriority()
    {
        // DASH
        if (Input.GetKeyDown(KeyCode.Space) && Time.time > model.lastDashTime + model.dashCooldown)
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            if (model.isBlocking) EndBlock();
            StartCoroutine(PerformJumpSmash());
            return;
        }
        if (model.isDashing) return;

        // BLOCK
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

        // ATTACK
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

    // --- ACTIONS ---

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
            float currentHeight = model.jumpCurve.Evaluate(percent) * model.jumpHeightMultiplier;
            // Tính toán di chuyển đơn giản hơn cho Jump Smash
            float deltaHeight = (model.jumpCurve.Evaluate((timer + Time.deltaTime)/model.dashAirTime) - model.jumpCurve.Evaluate(timer/model.dashAirTime)) * model.jumpHeightMultiplier;
            
            _characterController.Move(forwardMove + Vector3.up * deltaHeight);

            timer += Time.deltaTime;
            yield return null;
        }

        _characterController.Move(Vector3.down * 1.0f);
        
        // --- [VFX] SPAWN HIỆU ỨNG NHẢY DẬM ---
        // Spawn ngay dưới chân (offset lên 1 chút để không bị chìm xuống đất)
        SpawnVFX(model.vfxJumpSmash, transform.position + Vector3.up * 0.1f, Quaternion.identity);

        DealDamage(model.knockbackForces[2], DamageType.Heavy, Vector3.up);

        yield return new WaitForSeconds(model.dashRecoveryTime);
        model.isDashing = false;
    }

    void PerformCounterAttack()
    {
        Debug.Log(">>> THỰC HIỆN PHẢN KÍCH <<<");
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

    // --- DAMAGE & EVENTS ---

    void HandleImpact(int type)
    {
        // Vị trí ước lượng để spawn hiệu ứng (cách người 1.5m về phía trước)
        Vector3 hitPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;

        // TYPE 2: PHẢN KÍCH
        if (type == 2)
        {
            Debug.Log("EVENT: COUNTER HIT!");
            // --- [VFX] SPAWN HIỆU ỨNG PHẢN KÍCH ---
            SpawnVFX(model.vfxCounter, hitPos, Quaternion.LookRotation(transform.forward));

            DealDamage(model.knockbackForces[3], DamageType.Physical, transform.forward);
            return;
        }

        // COMBO THƯỜNG
        if (model.currentComboStep == 3) 
        {
            // --- [VFX] SPAWN HIỆU ỨNG ĐÒN 3 ---
            SpawnVFX(model.vfxCombo3, hitPos, Quaternion.LookRotation(transform.forward));
            DealDamage(model.knockbackForces[1], DamageType.Stun);
        }
        else 
        {
            DealDamage(model.knockbackForces[0], DamageType.Physical);
        }
    }

    void DealDamage(float force, DamageType dmgType, Vector3 directionOverride = default)
    {
        Vector3 hitPos = transform.position + transform.forward * 1.0f;
        if (model.enemyLayer.value == 0) { Debug.LogError("Chưa chọn Enemy Layer!"); return; }

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
                    type = dmgType
                };
                target.TakeDamage(info);
            }
        }
    }

    // --- HELPER FUNCTIONS ---
    void EndBlock()
    {
        model.isBlocking = false;
        view.SetBlocking(false);
        model.nextBlockTime = Time.time + model.blockCooldown;
    }

    public void OnBlockSuccess()
    {
        Debug.Log("BLOCK SUCCESS!");
        if (_counterWindowCoroutine != null) StopCoroutine(_counterWindowCoroutine);
        _counterWindowCoroutine = StartCoroutine(CounterWindowRoutine());
    }

    IEnumerator CounterWindowRoutine()
    {
        model.isCounterReady = true;
        yield return new WaitForSeconds(model.counterWindow);
        model.isCounterReady = false;
    }

    IEnumerator AttackRoutine(int step)
    {
        model.isAttacking = true;
        float duration = 0.5f;
        if (model.attackDurations != null && model.attackDurations.Length >= step)
            duration = model.attackDurations[step - 1];

        yield return new WaitForSeconds(duration);
        model.isAttacking = false;
        _currentActionCoroutine = null;
    }

    void HandleRotation(Vector3 moveDir)
    {
        if (model.isAttacking || model.isDashing || model.isBlocking) return;
        if (_cameraTransform == null) return;
        if (moveDir != Vector3.zero)
        {
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

    void RotateToCameraImmediate()
    {
        if (_cameraTransform == null) return;
        Vector3 camForward = _cameraTransform.forward;
        camForward.y = 0;
        if (camForward != Vector3.zero) transform.rotation = Quaternion.LookRotation(camForward);
    }

    private void OnDrawGizmosSelected()
    {
        if (model == null) return;
        Gizmos.color = Color.red;
        Vector3 hitPos = transform.position + transform.forward * 1.0f;
        Gizmos.DrawWireSphere(hitPos, model.attackRange);
    }
}