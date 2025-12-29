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
    private ThirdPersonCamera _cameraScript;
    private Coroutine _currentActionCoroutine;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        
        if (view == null) view = GetComponent<PlayerView>();
        if (model == null) model = new PlayerModel();

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
            _cameraScript = Camera.main.GetComponent<ThirdPersonCamera>();
        }
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
        // 1. Reset Combo
        if (Time.time > model.lastAttackTime + model.comboResetTime && model.currentComboStep > 0)
        {
            model.currentComboStep = 0;
        }

        // 2. Xử lý Input
        HandleInputPriority();

        // 3. LOGIC TRẠNG THÁI (State Machine)
        
        // --- TRẠNG THÁI KHÓA (LOCKED) ---
        // Khi đang Dash hoặc đang Tấn công
        if (model.isDashing || model.isAttacking)
        {
            // [SỬA LỖI TRƯỢT] Cưỡng ép vận tốc về 0 tuyệt đối
            model.currentVelocity = Vector3.zero;
            model.smoothDampVelocity = Vector3.zero; // Xóa sạch quán tính
            
            // [SỬA LỖI XOAY] Không gọi hàm HandleRotation ở đây -> Nhân vật sẽ không xoay
            
            // Cập nhật Animation đứng yên
            view.UpdateMovementAnimation(0, 0);

            // Vẫn chịu trọng lực (trừ khi đang Dash vì Dash tự lo độ cao)
            if (!_characterController.isGrounded && !model.isDashing)
            {
                _characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
            }
        }
        // --- TRẠNG THÁI TỰ DO (FREE) ---
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

            HandleRotation(moveDir); // Chỉ được xoay khi ở trạng thái này
            HandleMovement(moveDir, h, v);
        }
    }

    // --- CÁC HÀM LOGIC ---

    void HandleInputPriority()
    {
        // 1. DASH
        if (Input.GetKeyDown(KeyCode.Space) && Time.time > model.lastDashTime + model.dashCooldown)
        {
            if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
            model.isAttacking = false; 
            StartCoroutine(PerformJumpSmash());
            return;
        }
        if (model.isDashing) return;

        // 2. BLOCK
        if (Input.GetMouseButton(1))
        {
            model.isBlocking = true;
            view.SetBlocking(true);
            model.currentComboStep = 0;
        }
        else
        {
            model.isBlocking = false;
            view.SetBlocking(false);
        }

        // 3. ATTACK
        if (Input.GetMouseButtonDown(0) && !model.isBlocking)
        {
            // [SỬA LỖI COMBO KHÓ] Delay ngắn lại (0.1s) giúp nhận lệnh nhạy hơn
            if (model.currentComboStep > 0 && Time.time < model.lastAttackTime + model.minComboDelay)
                return;

            PerformComboAttack();
        }
    }

    void PerformComboAttack()
    {
        model.currentComboStep++;
        if (model.currentComboStep > 3) model.currentComboStep = 1;
        model.lastAttackTime = Time.time;
        
        // Xoay mặt về hướng Camera ĐÚNG 1 LẦN khi bắt đầu ra đòn
        RotateToCameraImmediate();
        
        view.TriggerAttack();

        if (_currentActionCoroutine != null) StopCoroutine(_currentActionCoroutine);
        _currentActionCoroutine = StartCoroutine(AttackRoutine(model.currentComboStep));
    }

    IEnumerator AttackRoutine(int step)
    {
        model.isAttacking = true;
        
        // Khóa cứng ngay lập tức
        model.currentVelocity = Vector3.zero;
        model.smoothDampVelocity = Vector3.zero;

        float duration = 0.5f;
        if (model.attackDurations != null && model.attackDurations.Length >= step)
            duration = model.attackDurations[step - 1];

        yield return new WaitForSeconds(duration);

        model.isAttacking = false;
        _currentActionCoroutine = null;
    }

    void HandleImpact(int type)
    {
        if (type == 1 && _cameraScript) _cameraScript.TriggerShake(0.2f, 0.5f);
    }

    // --- DI CHUYỂN & XOAY ---

    void HandleRotation(Vector3 moveDir)
    {
        // [CHỐT CHẶN AN TOÀN] Nếu đang đánh hoặc dash -> Không cho xoay
        if (model.isAttacking || model.isDashing) return;

        if (_cameraTransform == null) return;
        Quaternion targetRotation = transform.rotation;

        if (model.isBlocking) // Khi Block -> Xoay theo Camera
        {
            Vector3 camForward = _cameraTransform.forward; camForward.y = 0;
            if (camForward != Vector3.zero) targetRotation = Quaternion.LookRotation(camForward);
        }
        else // Khi chạy -> Xoay theo hướng di chuyển
        {
            if (moveDir != Vector3.zero) targetRotation = Quaternion.LookRotation(moveDir);
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, model.rotationSpeed * Time.deltaTime);
    }

    void HandleMovement(Vector3 moveDir, float h, float v)
    {
        // [CHỐT CHẶN AN TOÀN]
        if (model.isAttacking || model.isDashing) return;

        float actualSpeed = model.isBlocking ? model.moveSpeed * 0.5f : model.moveSpeed;
        Vector3 targetVelocity = moveDir * actualSpeed;
        
        float smoothTime = (moveDir.magnitude > 0) ? model.accelerationTime : model.decelerationTime;
        model.currentVelocity = Vector3.SmoothDamp(model.currentVelocity, targetVelocity, ref model.smoothDampVelocity, smoothTime);

        _characterController.Move(model.currentVelocity * Time.deltaTime);
        if (!_characterController.isGrounded) _characterController.Move(Vector3.down * 9.81f * Time.deltaTime);

        if (model.isBlocking) view.UpdateMovementAnimation(h, v);
        else 
        {
            float speedPercent = model.currentVelocity.magnitude / model.moveSpeed;
            if (speedPercent > 1) speedPercent = 1;
            view.UpdateMovementAnimation(0, speedPercent);
        }
    }

    void RotateToCameraImmediate()
    {
        if (_cameraTransform == null) return;
        Vector3 camForward = _cameraTransform.forward;
        camForward.y = 0;
        if (camForward != Vector3.zero) transform.rotation = Quaternion.LookRotation(camForward);
    }

    // --- DASH (JUMP SMASH) ---
    IEnumerator PerformJumpSmash()
    {
        model.isDashing = true;
        model.lastDashTime = Time.time;
        model.currentComboStep = 0;

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
        float previousHeight = 0;
        while (timer < model.dashAirTime)
        {
            float percent = timer / model.dashAirTime;
            Vector3 forwardMove = dashDirection * model.dashMoveSpeed * Time.deltaTime;
            float currentHeight = model.jumpCurve.Evaluate(percent) * model.jumpHeightMultiplier;
            float heightDelta = currentHeight - previousHeight;
            previousHeight = currentHeight;
            _characterController.Move(forwardMove + (Vector3.up * heightDelta));
            timer += Time.deltaTime;
            yield return null;
        }

        _characterController.Move(Vector3.down * 1.0f);
        if (_cameraScript) _cameraScript.TriggerShake(0.3f, 0.5f);
        yield return new WaitForSeconds(model.dashRecoveryTime);
        model.isDashing = false;
    }
}