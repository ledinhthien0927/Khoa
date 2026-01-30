using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class Boss2 : MonoBehaviour, IDamageable
{
    // ==========================================
    // 1. SETTINGS
    // ==========================================
    [Header("Stats")]
    public float MaxHealth = 500f;
    public float CurrentHealth;
    public float RunSpeed = 4.0f; 

    [Header("UI Settings")]
    public Slider HealthBarSlider;
    public GameObject HealthBarObj;

    [Header("Combat Ranges")]
    public float ChaseRange = 8.0f;    
    public float AttackRange = 2.0f;   
    public float DamageAmount = 25f;   

    // --- PHASE 2 SETTINGS (MỚI CẬP NHẬT) ---
    [Header("PHASE 2 TRANSITION")]
    [Tooltip("FX Nổ 1 lần khi bắt đầu chuyển pha")]
    public GameObject Phase2ExplosionVFX; 

    [Tooltip("FX Buff/Aura bám theo người Boss liên tục")]
    public GameObject Phase2BuffVFX; 

    public float Phase2AnimDuration = 2.5f; 
    public float Phase2Knockback = 10f; 

    // --- SKILL 1: SPIN ---
    [Header("SKILL 1: SPIN (XOAY KIẾM)")]
    public bool EnableSpin = true;
    public GameObject SpinIndicatorObj; 
    public float SpinCooldown = 12f;
    public float SpinDuration = 4.0f;      
    public float SpinRadius = 3.0f;        
    public float SpinDamagePerTick = 10f;  
    public float SpinHitRate = 0.5f;       
    public float SpinMoveSpeed = 5.0f;     
    public float SpinAnimSpeed = 2.0f;
    public float SpinWarningTime = 0.5f; 

    // --- SKILL 2: FIRE RING ---
    [Header("SKILL 2: FIRE RING (VÒNG LỬA)")]
    public bool EnableFireRing = true;
    public GameObject FireRingVFXObj; 
    public float FireRingCooldown = 15f;
    public float FireCastTime = 1.5f;     
    public float FireRingDuration = 5.0f; 
    public float FireRingRadius = 4.0f;   
    public float FireRingDamage = 20f;    

    // --- SKILL 3: GROUND SMASH ---
    [Header("SKILL 3: GROUND SMASH (ĐÁNH NẶNG)")]
    public bool EnableSmash = true;
    public GameObject SmashVFXPrefab; 
    public Vector3 SmashRotationOffset = new Vector3(0, 0, 0); 
    public float SmashCooldown = 8.0f;
    public float SmashCastTime = 0.6f; 
    public float SmashRange = 5.0f;    
    public float SmashAngle = 60f;     
    public float SmashDamage = 35f;
    public float SmashKnockback = 12.0f; 

    [Header("Other Settings")]
    public float Attack1HitTime = 0.4f;  
    public float Attack1Duration = 1.0f; 
    public float DodgeChance = 40f;
    public float JumpBackSpeed = 10.0f;
    
    // ==========================================
    // SYSTEM VARIABLES
    // ==========================================
    public enum State { Idle, Patrol, Chasing, Strafing, Approaching, Attacking, Spinning, FireRing, GroundSmash, PhaseChange, Retreating, Dodging, Hit, Dead }
    
    [Header("Debug Info")]
    public State CurrentState;
    public bool IsEnraged = false; 

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private IDamageable _playerCombat; 
    private Camera _mainCamera;
    
    private float _timer;
    private float _spinTimer = 0f;
    private float _fireRingTimer = 0f;
    private float _smashTimer = 0f;
    private float _strafeDirection = 1f;
    private float _strafeChangeTimer;
    
    // Biến lưu trữ object Buff để xóa khi chết
    private GameObject _activeBuffInstance; 

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _agent.updateRotation = false; 
        CurrentHealth = MaxHealth;
        _mainCamera = Camera.main;

        if (HealthBarSlider != null) { HealthBarSlider.maxValue = MaxHealth; HealthBarSlider.value = CurrentHealth; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { _player = p.transform; _playerCombat = p.GetComponent<IDamageable>(); }
        
        CurrentState = State.Idle;

        if (SpinIndicatorObj != null) SpinIndicatorObj.SetActive(false);
        if (FireRingVFXObj != null) FireRingVFXObj.SetActive(false);
    }

    void Update()
    {
        if (CurrentHealth <= 0 && CurrentState != State.Dead) { Die(); return; }

        // Kích hoạt chuyển pha
        if (!IsEnraged && CurrentHealth <= MaxHealth * 0.5f) 
        {
            StartCoroutine(PhaseChangeRoutine());
        }

        UpdateHealthUI();
        if (HealthBarObj != null && _mainCamera != null)
            HealthBarObj.transform.rotation = Quaternion.LookRotation(HealthBarObj.transform.position - _mainCamera.transform.position);

        if (_player == null || CurrentState == State.Dead) return;

        if (_spinTimer > 0) _spinTimer -= Time.deltaTime;
        if (_fireRingTimer > 0) _fireRingTimer -= Time.deltaTime;
        if (_smashTimer > 0) _smashTimer -= Time.deltaTime;

        HandleAnimation();

        switch (CurrentState)
        {
            case State.Idle: LogicIdle(); break;
            case State.Chasing: LogicChasing(); break;
            case State.Strafing: LogicStrafing(); break;
            case State.Approaching: LogicApproaching(); break;
            case State.Spinning: LogicSpinning(); break;
            case State.FireRing: break; 
            case State.GroundSmash: break; 
            case State.PhaseChange: break; 
            case State.Attacking:
            case State.Dodging:
            case State.Hit: FaceTarget(); break;
            case State.Retreating: LogicRetreating(); break;
        }
    }

    // --- PHASE CHANGE LOGIC ---
    IEnumerator PhaseChangeRoutine()
    {
        // 1. KHÓA TRẠNG THÁI
        IsEnraged = true; 
        CurrentState = State.PhaseChange; 

        // Dừng di chuyển
        if (_agent != null && _agent.isOnNavMesh) 
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        // 2. DIỄN HOẠT & HIỆU ỨNG (Bọc trong Try-Catch để nếu lỗi thì không chết Code)
        try
        {
            if (_animator != null) _animator.SetTrigger("Phase2Trigger");

            // Xử lý VFX Nổ
            if (Phase2ExplosionVFX != null)
            {
                GameObject boom = Instantiate(Phase2ExplosionVFX, transform.position, Quaternion.identity);
                Destroy(boom, 3.0f);
            }

            // Xử lý VFX Buff
            if (Phase2BuffVFX != null)
            {
                _activeBuffInstance = Instantiate(Phase2BuffVFX, transform.position, Quaternion.identity);
                _activeBuffInstance.transform.SetParent(transform); 
                _activeBuffInstance.transform.localPosition = Vector3.zero; 
            }

            // Đẩy lùi Player (Có kiểm tra kỹ để không lỗi)
            if (_player != null && Vector3.Distance(transform.position, _player.position) < 6.0f)
            {
                if (_playerCombat != null)
                {
                    Vector3 pushDir = (_player.position - transform.position).normalized; 
                    pushDir.y = 0;
                    _playerCombat.TakeDamage(new DamageInfo { 
                        amount = 0, 
                        attacker = gameObject, 
                        hitPoint = _player.position, 
                        hitDirection = pushDir, 
                        knockbackForce = Phase2Knockback, 
                        type = DamageType.Heavy, 
                        duration = 0.5f 
                    });
                }
            }
        }
        catch (System.Exception ex)
        {
            // Nếu có lỗi, in ra Console để bạn biết, nhưng code vẫn chạy tiếp xuống dưới!
            Debug.LogError("BOSS ERROR (Phase 2): " + ex.Message);
        }

        // 3. CHỜ DIỄN XONG
        yield return new WaitForSeconds(Phase2AnimDuration);

        // 4. TĂNG SỨC MẠNH
        RunSpeed *= 1.3f; 
        SpinMoveSpeed *= 1.3f; 
        SpinCooldown *= 0.6f; 
        FireRingCooldown *= 0.6f; 
        SmashCooldown *= 0.6f;

        // Đổi màu đỏ
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rends) 
        { 
            // Kiểm tra null để tránh lỗi nếu object không có material
            if (r != null && r.material != null) r.material.color = Color.red; 
        }

        // 5. [QUAN TRỌNG NHẤT] KÍCH HOẠT LẠI TRẠNG THÁI (Bọc kỹ)
        Debug.Log("BOSS: Kết thúc chuyển Phase -> Quay lại chiến đấu!");
        
        if (_agent != null && _agent.isOnNavMesh) 
        {
            _agent.isStopped = false;
        }

        // Ép Boss chuyển sang trạng thái đi vòng (Strafing) hoặc đuổi (Chasing)
        CurrentState = State.Strafing; 
        _timer = 0.5f; // Đặt timer thấp để nó hành động ngay
    }

    // --- LOGIC DI CHUYỂN ---
    void LogicIdle() { if (CheckForPlayer()) return; }
    void LogicChasing() {
        if (Vector3.Distance(transform.position, _player.position) <= ChaseRange) { CurrentState = State.Strafing; _timer = 2f; return; }
        _agent.speed = RunSpeed; _agent.SetDestination(_player.position);
        FaceTargetMoving();
    }
    void LogicStrafing() {
        if (Vector3.Distance(transform.position, _player.position) > ChaseRange + 3f) { CurrentState = State.Chasing; return; }
        FaceTarget(); _agent.speed = RunSpeed * 0.5f;
        _strafeChangeTimer -= Time.deltaTime; 
        if (_strafeChangeTimer <= 0) { _strafeDirection *= -1; _strafeChangeTimer = Random.Range(2f, 4f); }
        Vector3 side = Vector3.Cross(Vector3.up, (_player.position - transform.position).normalized);
        _agent.SetDestination(transform.position + side * _strafeDirection * 3f);
        _timer -= Time.deltaTime; 
        if (_timer <= 0) { CurrentState = State.Approaching; }
    }
    void LogicApproaching() {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (EnableFireRing && _fireRingTimer <= 0 && dist <= FireRingRadius + 1.0f) { StartCoroutine(FireRingRoutine()); return; }
        if (EnableSmash && _smashTimer <= 0 && dist <= SmashRange && dist > AttackRange) { StartCoroutine(GroundSmashRoutine()); return; }
        if (EnableSpin && _spinTimer <= 0 && dist <= 6.0f) { StartCoroutine(SpinRoutine()); return; }
        if (dist <= AttackRange - 0.3f) { StartCoroutine(AttackRoutine()); return; }
        _agent.speed = RunSpeed; _agent.SetDestination(_player.position); FaceTarget();
    }
    void LogicSpinning() { _agent.speed = SpinMoveSpeed; _agent.SetDestination(_player.position); }
    void LogicRetreating() {
        FaceTarget(); _agent.speed = RunSpeed * 0.5f;
        Vector3 back = (transform.position - _player.position).normalized;
        _agent.SetDestination(transform.position + back * 5.0f);
        _timer -= Time.deltaTime; if (_timer <= 0) { CurrentState = State.Strafing; _timer = 2.0f; }
    }

    // --- SKILLS ---
    IEnumerator GroundSmashRoutine() { CurrentState = State.GroundSmash; _smashTimer = SmashCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; if (_player != null) { Vector3 t = _player.position; t.y = transform.position.y; transform.LookAt(t); } _animator.SetTrigger("SmashTrigger"); yield return new WaitForSeconds(SmashCastTime); if (SmashVFXPrefab != null) { Quaternion r = transform.rotation * Quaternion.Euler(SmashRotationOffset); GameObject v = Instantiate(SmashVFXPrefab, transform.position, r); Destroy(v, 2.0f); } CheckSmashHit(); yield return new WaitForSeconds(1.0f); _agent.isStopped = false; CurrentState = State.Strafing; _timer = 1.0f; }
    void CheckSmashHit() { if (_player == null) return; float dist = Vector3.Distance(transform.position, _player.position); if (dist <= SmashRange) { Vector3 dir = (_player.position - transform.position).normalized; float angle = Vector3.Angle(transform.forward, dir); if (angle < SmashAngle / 2) DealDamageToPlayer(SmashDamage, SmashKnockback, DamageType.Heavy, 0.5f); } }
    IEnumerator FireRingRoutine() { CurrentState = State.FireRing; _fireRingTimer = FireRingCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; FaceTarget(); _animator.SetTrigger("CastSkill"); yield return new WaitForSeconds(FireCastTime); if (FireRingVFXObj != null) FireRingVFXObj.SetActive(true); float duration = FireRingDuration; while (duration > 0) { if (Vector3.Distance(transform.position, _player.position) <= FireRingRadius) DealDamageToPlayer(FireRingDamage * Time.deltaTime, 0f, DamageType.Magic, 0f); duration -= Time.deltaTime; yield return null; } if (FireRingVFXObj != null) FireRingVFXObj.SetActive(false); _agent.isStopped = false; CurrentState = State.Strafing; _timer = 1.0f; }
    IEnumerator SpinRoutine() { CurrentState = State.Spinning; _spinTimer = SpinCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; if (SpinIndicatorObj != null) SpinIndicatorObj.SetActive(true); yield return new WaitForSeconds(SpinWarningTime); _agent.isStopped = false; _animator.SetFloat("SpinSpeed", SpinAnimSpeed); _animator.SetBool("IsSpinning", true); _animator.SetTrigger("SpinTrigger"); float elapsed = 0f; float damageTick = 0f; while (elapsed < SpinDuration) { elapsed += Time.deltaTime; damageTick -= Time.deltaTime; if (damageTick <= 0) { if (Vector3.Distance(transform.position, _player.position) <= SpinRadius) DealDamageToPlayer(SpinDamagePerTick, 2f, DamageType.Physical, 0f); damageTick = SpinHitRate; } yield return null; } _animator.SetBool("IsSpinning", false); _animator.SetFloat("SpinSpeed", 1.0f); if (SpinIndicatorObj != null) SpinIndicatorObj.SetActive(false); _agent.velocity = Vector3.zero; yield return new WaitForSeconds(0.5f); CurrentState = State.Strafing; _timer = 1.5f; }
    IEnumerator AttackRoutine() { CurrentState = State.Attacking; _agent.velocity = Vector3.zero; _agent.isStopped = true; FaceTarget(); _animator.SetTrigger("Attack1"); yield return new WaitForSeconds(Attack1HitTime); DealDamageToPlayer(DamageAmount, 5f, DamageType.Physical, 0f); yield return new WaitForSeconds(Attack1Duration - Attack1HitTime); if (Random.Range(0, 100) < DodgeChance) StartCoroutine(JumpBackRoutine()); else { _agent.isStopped = false; CurrentState = State.Retreating; _timer = 1.0f; } }
    IEnumerator JumpBackRoutine() { CurrentState = State.Dodging; _agent.isStopped = false; _animator.SetTrigger("Dodge"); _agent.speed = JumpBackSpeed; _agent.SetDestination(transform.position - transform.forward * 4.0f); yield return new WaitForSeconds(0.5f); _agent.speed = RunSpeed; CurrentState = State.Strafing; _timer = 1.5f; }
    
    IEnumerator HitReactionRoutine()
    {
        // Kiểm tra an toàn lần 2
        if (CurrentState == State.PhaseChange) yield break;

        CurrentState = State.Hit;
        _agent.isStopped = true; 
        _agent.velocity = Vector3.zero;
        
        _animator.SetTrigger("Hit");
        
        yield return new WaitForSeconds(0.5f);
        
        _agent.isStopped = false;
        CurrentState = State.Strafing;
        _timer = 1f;
    }
    public HitResult TakeDamage(DamageInfo info)
    {
        if (CurrentState == State.Dead) return HitResult.Ignored;

        CurrentHealth -= info.amount;
        UpdateHealthUI();

        if (CurrentHealth <= 0)
        {
            Die();
            return HitResult.Critical;
        }

        // [SỬA LỖI QUAN TRỌNG TẠI ĐÂY]
        // Nếu Boss đang gồng Chiêu Lửa, Xoay, Đập đất HOẶC ĐANG CHUYỂN PHA
        // Thì TUYỆT ĐỐI KHÔNG ĐƯỢC ngắt Coroutine (StopAllCoroutines)
        if (CurrentState == State.Spinning || 
            CurrentState == State.FireRing || 
            CurrentState == State.GroundSmash || 
            CurrentState == State.PhaseChange) // <--- Thêm dòng này
        {
            // Boss đang bận tung chiêu, nhận dam nhưng không bị khựng
            return HitResult.Hit;
        }

        // Chỉ bị khựng (Hit Reaction) khi đang đi bộ hoặc đứng chơi
        if (info.type == DamageType.Stun || info.type == DamageType.Heavy)
        {
            StopAllCoroutines();
            StartCoroutine(HitReactionRoutine());
        }

        return HitResult.Hit;
    }    void DealDamageToPlayer(float dmg, float force, DamageType type, float stunDuration) { if (_playerCombat != null && Vector3.Distance(transform.position, _player.position) <= 20f) { Vector3 pushDir = (_player.position - transform.position).normalized; pushDir.y = 0; _playerCombat.TakeDamage(new DamageInfo { amount = dmg, attacker = gameObject, hitPoint = _player.position, hitDirection = pushDir, knockbackForce = force, type = type, duration = stunDuration }); } }
    
    void Die() { 
        CurrentState = State.Dead; 
        _agent.isStopped = true; 
        StopAllCoroutines(); 
        _animator.SetTrigger("Die"); 
        
        if (SpinIndicatorObj != null) SpinIndicatorObj.SetActive(false); 
        if (FireRingVFXObj != null) FireRingVFXObj.SetActive(false); 

        // [MỚI] Xóa Buff khi chết
        if (_activeBuffInstance != null) Destroy(_activeBuffInstance);

        Destroy(gameObject, 5f); 
        this.enabled = false; 
    }

    void UpdateHealthUI() { if (HealthBarSlider != null) HealthBarSlider.value = CurrentHealth; }
    bool CheckForPlayer() { if (_player != null && Vector3.Distance(transform.position, _player.position) <= 15f) { CurrentState = State.Chasing; return true; } return false; }
    void HandleAnimation() { Vector3 v = transform.InverseTransformDirection(_agent.velocity); _animator.SetFloat("InputX", v.z, 0.1f, Time.deltaTime); _animator.SetFloat("InputY", -v.x, 0.1f, Time.deltaTime); }
    void FaceTarget() { if (_player != null) { Vector3 d = (_player.position - transform.position).normalized; d.y = 0; if (d != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 15f); } }
    void FaceTargetMoving() { if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized); }
    void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, SpinRadius); Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, FireRingRadius); Gizmos.color = Color.blue; Vector3 leftRay = Quaternion.Euler(0, -SmashAngle / 2, 0) * transform.forward * SmashRange; Vector3 rightRay = Quaternion.Euler(0, SmashAngle / 2, 0) * transform.forward * SmashRange; Gizmos.DrawLine(transform.position, transform.position + leftRay); Gizmos.DrawLine(transform.position, transform.position + rightRay); Gizmos.DrawLine(transform.position + leftRay, transform.position + rightRay); }
}