using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; // BẮT BUỘC ĐỂ DÙNG UI

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class SimpleEnemyAI : MonoBehaviour, IDamageable
{
    // ==========================================
    // 1. SETTINGS
    // ==========================================
    [Header("Stats")]
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float RunSpeed = 3.5f; 
    public float WalkSpeed = 1.5f;

    [Header("UI Settings (UPDATED)")]
    public Slider HealthBarSlider;   // <-- ĐỔI TỪ IMAGE SANG SLIDER
    public GameObject HealthBarObj;  // Canvas chứa thanh máu
    [Header("Death Settings")]
    public float CorpseDestroyTime = 5.0f; // Sau 5 giây xác sẽ biến mất

    [Header("Combat Ranges")]
    public float ChaseRange = 6.0f;    
    public float AttackRange = 1.5f;   
    public float DamageAmount = 15f;   

    [Header("Combo Timing")]
    public float Attack1Duration = 0.8f; 
    public float Attack1HitTime = 0.3f;  
    public float Attack2Duration = 1.0f; 
    public float Attack2HitTime = 0.4f;  

    [Header("Dodge Settings")]
    public float JumpBackSpeed = 10.0f;    
    public float JumpBackDuration = 0.4f;  
    public float DodgeChance = 50f;        

    [Header("Ultimate Skill")]
    public float SkillCooldown = 8.0f;    
    public int RapidHitCount = 6;          
    public float RapidHitInterval = 0.2f;  
    public float SkillAnimSpeed = 2.5f;    
    public float ChargeDuration = 1.0f;    
    public float DashInSpeed = 12.0f;      

    // ==========================================
    // 2. SYSTEM
    // ==========================================
    [Header("Debug Info")]
    public bool HasToken = false;
    public State CurrentState;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private IDamageable _playerCombat;
    private Camera _mainCamera;

    private float _timer;
    private float _skillTimer = 0f;
    private float _strafeDirection = 1f;
    private float _strafeChangeTimer;
    private float _strafeChangeDuration;

    private static int _currentAttackers = 0;
    private const int MAX_ATTACKERS = 2;

    public enum State { Chasing, Strafing, Approaching, Attacking, Retreating, Dodging, Hit, Dead }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _agent.updateRotation = false; 
        
        CurrentHealth = MaxHealth;
        _mainCamera = Camera.main;

        // Cài đặt Slider ban đầu
        if (HealthBarSlider != null)
        {
            HealthBarSlider.maxValue = MaxHealth;
            HealthBarSlider.value = CurrentHealth;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player = p.transform;
            _playerCombat = p.GetComponent<IDamageable>();
        }

        CurrentState = State.Chasing;
        _timer = Random.Range(0.5f, 1.5f);
    }

    void Update()
    {

        if (CurrentHealth <= 0 && CurrentState != State.Dead)
        {
            Die();
            return;
        }
        UpdateHealthUI();

        if (CurrentHealth <= 0 && CurrentState != State.Dead)
        {
            Die();
            return;
        }

        if (HealthBarObj != null && _mainCamera != null)
        {
            HealthBarObj.transform.rotation = Quaternion.LookRotation(HealthBarObj.transform.position - _mainCamera.transform.position);
        }

        if (_player == null || CurrentState == State.Dead) return;
        if (_skillTimer > 0) _skillTimer -= Time.deltaTime; 

        HandleAnimation();

        switch (CurrentState)
        {
            case State.Chasing: LogicChasing(); break;
            case State.Strafing: LogicStrafing(); break;
            case State.Approaching: LogicApproaching(); break;
            case State.Attacking:
            case State.Dodging:
            case State.Hit: FaceTarget(); break;
            case State.Retreating: LogicRetreating(); break;
        }
    }

    // ==========================================
    // LOGIC UI & DAMAGE
    // ==========================================

    public HitResult TakeDamage(DamageInfo info)
    {
        if (CurrentState == State.Dead) return HitResult.Ignored;

        CurrentHealth -= info.amount;
        UpdateHealthUI(); // Cập nhật Slider

        if (CurrentHealth <= 0)
        {
            Die();
            return HitResult.Critical;
        }

        if (CurrentState != State.Dodging && CurrentState != State.Attacking)
        {
            StopAllCoroutines();
            StartCoroutine(HitReactionRoutine());
        }

        return HitResult.Hit;
    }

    void UpdateHealthUI()
    {
        if (HealthBarSlider != null)
        {
            // Slider tự động xử lý việc co giãn ảnh Sliced
            HealthBarSlider.value = CurrentHealth;
        }
    }

    void Die()
    {
        CurrentState = State.Dead;
        ReturnToken();
        
        // 1. Dừng mọi hoạt động
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        StopAllCoroutines(); // Dừng mọi skill/combo đang dở dang

        // 2. Chạy Animation Chết
        _animator.SetTrigger("Die");

        // 3. Tắt Va chạm (Để Player đi xuyên qua xác chết)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4. Tắt Thanh Máu
        if (HealthBarObj != null) HealthBarObj.SetActive(false);

        // --- MỚI THÊM: XÓA XÁC SAU VÀI GIÂY ---
        // Lệnh này sẽ xóa toàn bộ GameObject Enemy khỏi game sau khoảng thời gian cài đặt
        Destroy(gameObject, CorpseDestroyTime);
        // --------------------------------------

        // 5. Tắt Script này (để Update không chạy nữa)
        this.enabled = false;
    }

    // ==========================================
    // GIỮ NGUYÊN CÁC LOGIC DI CHUYỂN & SKILL CŨ
    // ==========================================
    // (Animation logic đã khớp với hình Blend Tree bạn gửi: InputX = Z, InputY = -X)

    void LogicChasing() { float d = Vector3.Distance(transform.position, _player.position); if (d <= ChaseRange) { CurrentState = State.Strafing; _timer = 2f; return; } _agent.speed = RunSpeed; _agent.SetDestination(_player.position); if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized); }
    void LogicStrafing() { float d = Vector3.Distance(transform.position, _player.position); if (d > ChaseRange + 3f) { CurrentState = State.Chasing; return; } FaceTarget(); _agent.speed = WalkSpeed; Vector3 s = Vector3.Cross(Vector3.up, (_player.position - transform.position).normalized); _agent.SetDestination(transform.position + s * _strafeDirection * 2f); _timer -= Time.deltaTime; if (_timer <= 0) { _strafeDirection *= -1; _timer = 2f; TryGetToken(); } }
    void LogicApproaching() { float d = Vector3.Distance(transform.position, _player.position); if (_skillTimer <= 0 && d <= AttackRange + 3f) { StartCoroutine(RapidSkillRoutine()); return; } if (d <= AttackRange - 0.3f) { StartCoroutine(AttackRoutine(true)); return; } _agent.speed = RunSpeed; _agent.SetDestination(_player.position); FaceTarget(); }
    void LogicRetreating() { FaceTarget(); _agent.speed = WalkSpeed; _agent.SetDestination(transform.position + (transform.position - _player.position).normalized * 4f); _timer -= Time.deltaTime; if (_timer <= 0) CurrentState = State.Strafing; }

    IEnumerator RapidSkillRoutine() 
    {
        CurrentState = State.Attacking; _skillTimer = SkillCooldown; _agent.isStopped = false; 
        _agent.velocity = Vector3.zero; _agent.isStopped = true; FaceTarget(); _animator.SetTrigger("SkillCharge"); 
        yield return new WaitForSeconds(ChargeDuration);
        _agent.isStopped = false; _agent.speed = DashInSpeed; _agent.acceleration = 100f; _agent.SetDestination(_player.position);
        float t = 0; while (Vector3.Distance(transform.position, _player.position) > AttackRange - 0.2f && t < 1f) { t += Time.deltaTime; yield return null; }
        _agent.velocity = Vector3.zero; _agent.isStopped = true; _animator.SetFloat("AttackSpeed", SkillAnimSpeed); 
        for (int i = 0; i < RapidHitCount; i++) { FaceTarget(); _animator.Play("RapidSlash", 0, 0f); if (_playerCombat != null) _playerCombat.TakeDamage(new DamageInfo { amount = DamageAmount * 0.5f, attacker = gameObject, hitPoint = _player.position, type = DamageType.Physical, knockbackForce = 1f }); yield return new WaitForSeconds(RapidHitInterval); }
        _animator.SetFloat("AttackSpeed", 1.0f); ReturnToken(); yield return StartCoroutine(JumpBackRoutine());
    }

    IEnumerator AttackRoutine(bool combo) 
    { 
        CurrentState = State.Attacking; _agent.velocity = Vector3.zero; _agent.isStopped = true; FaceTarget(); _animator.SetTrigger("Attack1"); yield return new WaitForSeconds(Attack1HitTime); 
        if (_playerCombat != null && Vector3.Distance(transform.position, _player.position) <= AttackRange + 0.5f) _playerCombat.TakeDamage(new DamageInfo { amount = DamageAmount, attacker = gameObject, hitPoint = _player.position, type = DamageType.Physical, knockbackForce = 3f });
        yield return new WaitForSeconds(Attack1Duration - Attack1HitTime);
        float d = Vector3.Distance(transform.position, _player.position);
        if (combo && d <= AttackRange + 0.8f) { FaceTarget(); _animator.SetTrigger("Attack2"); yield return new WaitForSeconds(Attack2HitTime); if (_playerCombat != null) _playerCombat.TakeDamage(new DamageInfo { amount = DamageAmount, attacker = gameObject, hitPoint = _player.position, type = DamageType.Physical, knockbackForce = 3f }); yield return new WaitForSeconds(Attack2Duration - Attack2HitTime); } else { yield return new WaitForSeconds(0.2f); }
        ReturnToken(); if (Random.Range(0, 100) < DodgeChance) StartCoroutine(JumpBackRoutine()); else { _agent.isStopped = false; CurrentState = State.Retreating; _timer = 1.5f; }
    }

    IEnumerator JumpBackRoutine() { CurrentState = State.Dodging; _agent.isStopped = false; _animator.SetTrigger("Dodge"); _agent.speed = JumpBackSpeed; _agent.acceleration = 100f; Vector3 p = transform.position - transform.forward * 3.5f; _agent.SetDestination(p); yield return new WaitForSeconds(JumpBackDuration); _agent.speed = WalkSpeed; _agent.acceleration = 8f; _agent.velocity = Vector3.zero; CurrentState = State.Strafing; _timer = 2f; }
    IEnumerator HitReactionRoutine() { CurrentState = State.Hit; _agent.isStopped = true; _animator.SetTrigger("Hit"); yield return new WaitForSeconds(0.5f); _agent.isStopped = false; CurrentState = State.Strafing; }

    void HandleAnimation() { Vector3 v = transform.InverseTransformDirection(_agent.velocity); _animator.SetFloat("InputX", v.z, 0.1f, Time.deltaTime); _animator.SetFloat("InputY", -v.x, 0.1f, Time.deltaTime); }
    void TryGetToken() { if (_currentAttackers < MAX_ATTACKERS) { _currentAttackers++; HasToken = true; CurrentState = State.Approaching; } }
    void ReturnToken() { if (HasToken) { _currentAttackers--; HasToken = false; } }
    void FaceTarget() { Vector3 d = (_player.position - transform.position).normalized; d.y = 0; if (d != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 15f); }
    void OnDisable() { ReturnToken(); }
}