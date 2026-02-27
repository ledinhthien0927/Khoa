using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class thgEnemyAI : MonoBehaviour, IDamageable
{
    // ... (Các setting cũ giữ nguyên) ...
    [Header("Stats")]
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float RunSpeed = 3.5f; 
    public float WalkSpeed = 1.5f;

    [Header("UI Settings")]
    public Slider HealthBarSlider;
    public GameObject HealthBarObj;
    
    // [MỚI] BIẾN TRAIL
    [Header("VFX Settings")]
    [Tooltip("Kéo object có TrailRenderer (gắn trên vũ khí) vào đây")]
    public TrailRenderer WeaponTrail; 

    // ... (Giữ nguyên các Header Minion, Patrol, Combat...) ...
    [Header("Minion Settings")]
    public bool PlaySpawnAnim = false;     
    public float SpawnDuration = 2.0f;     
    public bool IsSimpleMinion = false; 

    [Header("Patrol Settings")]
    public Transform[] PatrolPoints;
    public float PatrolSpeed = 2.0f;
    public float IdleTime = 3.0f;
    public float DetectionRange = 10.0f;

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

    [Header("SKILL 1: RAPID BARRAGE")]
    public bool EnableRapidSkill = true;   
    public float SkillCooldown = 8.0f;     
    public int RapidHitCount = 6;          
    public float RapidHitInterval = 0.2f;  
    public float SkillAnimSpeed = 2.5f;    
    public float ChargeDuration = 1.0f;    
    public float DashInSpeed = 12.0f;
    public GameObject RapidSkillVFX; 

    [Header("SKILL 2: HELLFIRE")]
    public bool EnableHellfire = true;     
    public float HellfireCastTime = 1.5f;  
    public GameObject AoEPrefab;
    public float HellfireCooldown = 15f;
    public float HellfireDuration = 4.0f; 
    public float MinSpawnDistance = 3.5f; 
    public float WaveInterval = 1.5f; 

    [Header("SKILL 3: SUMMON")]
    public bool CanSummon = false;         
    public float SummonCastTime = 2.0f;    
    public GameObject MinionPrefab;
    public GameObject SummonVFX;
    public int MaxMinions = 3;
    public float SummonCooldown = 20f;
    public float SummonRadius = 3.0f;

    [Header("SKILL 4: JUMP ATTACK")]
    public bool EnableJumpAttack = true;
    public float JumpCooldown = 12.0f;
    public float JumpChargeDuration = 1.2f; 
    public float JumpAnimTotalDuration = 3.2f; 
    public float JumpTakeOffTime = 1.0f; 
    public float JumpLandTime = 2.5f;    
    public float JumpHeight = 5.0f; 
    public float JumpDamage = 40.0f;
    public float JumpRadius = 4.0f;
    public GameObject JumpAttackVFX; 

    [Header("Death Settings")]
    public float CorpseDestroyTime = 5.0f;

    // ... (Các biến Private giữ nguyên) ...
    [Header("Debug Info")]
    public bool HasToken = false;
    public State CurrentState;
    public bool IsEnraged = false; 

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private IDamageable _playerCombat; 
    private Camera _mainCamera;
    private float _timer;
    private float _skillTimer = 0f;
    private float _hellfireTimer = 0f;
    private float _summonTimer = 0f;
    private float _jumpTimer = 0f;
    private float _strafeDirection = 1f;
    private float _strafeChangeTimer;
    private int _currentPatrolIndex = 0;
    private List<GameObject> _activeMinions = new List<GameObject>();
    private static int _currentAttackers = 0;
    private const int MAX_ATTACKERS = 2;
    [Header("Movement VFX")]
    public ParticleSystem MoveDustVFX;

    [Header("Death Settings")]
    [Tooltip("VFX bụi khi Boss ngã xuống")]
    
    
    public GameObject DeathDustVFX; 
    [Tooltip("Thời gian để Boss mờ hoàn toàn")]
    public float FadeOutDuration = 3.0f;
    
    [Header("Jump Attack Camera Shake")]
    public float JumpShakeDuration = 0.3f; // Thời gian rung
    public float JumpShakeMagnitude = 0.8f; // Độ mạnh của rung
    public void TriggerJumpShake()
    {
        // Đã đổi CameraShake thành BossCameraShake
        if (BossCameraShake.Instance != null)
        {
            BossCameraShake.Instance.Shake(JumpShakeDuration, JumpShakeMagnitude);
        }
    }
    public void EnableTrail()
    {
        if (WeaponTrail != null) WeaponTrail.emitting = true;
    }

    public void DisableTrail()
    {
        if (WeaponTrail != null) WeaponTrail.emitting = false;
    }

    public enum State { Spawning, Idle, Patrol, Chasing, Strafing, Approaching, Attacking, Retreating, Dodging, Hit, Dead }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _agent.updateRotation = false; 
        
        CurrentHealth = MaxHealth;
        IsEnraged = false;
        _mainCamera = Camera.main;

        if (HealthBarSlider != null) { HealthBarSlider.maxValue = MaxHealth; HealthBarSlider.value = CurrentHealth; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { _player = p.transform; _playerCombat = p.GetComponent<IDamageable>(); }

        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(false);
        
        // [MỚI] Tắt Trail lúc đầu
        SetTrail(false);

        if (PlaySpawnAnim) StartCoroutine(SpawnRoutine());
        else 
        {
            if (PatrolPoints != null && PatrolPoints.Length > 0)
            {
                CurrentState = State.Patrol;
                _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position);
            }
            else CurrentState = State.Idle;
        }
    }

    void Update()
    {
        if (CurrentHealth <= 0 && CurrentState != State.Dead) { Die(); return; }

        if (!IsEnraged && !IsSimpleMinion && CurrentHealth <= MaxHealth * 0.5f) 
        {
            ActivatePhase2();
        }

        UpdateHealthUI();
        if (HealthBarObj != null && _mainCamera != null)
            HealthBarObj.transform.rotation = Quaternion.LookRotation(HealthBarObj.transform.position - _mainCamera.transform.position);

        if (_player == null || CurrentState == State.Dead) return;
        if (CurrentState == State.Spawning) return;

        if (_skillTimer > 0) _skillTimer -= Time.deltaTime; 
        if (_hellfireTimer > 0) _hellfireTimer -= Time.deltaTime;
        if (_summonTimer > 0) _summonTimer -= Time.deltaTime;
        if (_jumpTimer > 0) _jumpTimer -= Time.deltaTime;

        _activeMinions.RemoveAll(item => item == null);
        HandleAnimation();
        HandleDustVFX();
        switch (CurrentState)
        {
            case State.Idle: LogicIdle(); break;
            case State.Patrol: LogicPatrol(); break;
            case State.Chasing: LogicChasing(); break;
            case State.Strafing: LogicStrafing(); break;
            case State.Approaching: LogicApproaching(); break;
            case State.Attacking:
            case State.Dodging:
            case State.Hit: FaceTarget(); break;
            case State.Retreating: LogicRetreating(); break;
        }
    }

    // ... (Giữ nguyên các hàm Logic di chuyển & Phase 2) ...
    void ActivatePhase2()
    {
        IsEnraged = true;
        RunSpeed *= 1.5f; DashInSpeed *= 1.3f;
        SkillCooldown /= 2.0f; HellfireCooldown /= 2.0f; SummonCooldown /= 2.0f;
        _skillTimer = 0; _hellfireTimer = 0; _summonTimer = 0;
        transform.localScale = transform.localScale * 1.3f;
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rends) 
        { 
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", Color.red); 
            r.material.color = Color.red; 
        }
        if (SummonVFX != null) Instantiate(SummonVFX, transform.position, Quaternion.identity);
    }
    void LogicIdle() { if (CheckForPlayer()) return; _timer -= Time.deltaTime; if (_timer <= 0) NextPatrolPoint(); }
    void LogicPatrol() { if (CheckForPlayer()) return; _agent.speed = PatrolSpeed; if (_agent.velocity.sqrMagnitude > 0.1f) { Vector3 dir = _agent.velocity.normalized; dir.y = 0; if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f); } if (!_agent.pathPending && _agent.remainingDistance < 0.5f) { CurrentState = State.Idle; _timer = IdleTime; } }
    void NextPatrolPoint() { if (PatrolPoints.Length == 0) return; _currentPatrolIndex = (_currentPatrolIndex + 1) % PatrolPoints.Length; CurrentState = State.Patrol; _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position); }
    void LogicChasing() { float dist = Vector3.Distance(transform.position, _player.position); if (dist > DetectionRange * 1.5f) { CurrentState = State.Patrol; if (PatrolPoints.Length > 0) _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position); return; } if (dist <= ChaseRange) { if (IsSimpleMinion) { CurrentState = State.Approaching; HasToken = true; return; } else { CurrentState = State.Strafing; _timer = 2f; return; } } _agent.speed = RunSpeed; _agent.SetDestination(_player.position); if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized); }
    void LogicStrafing() { if (IsSimpleMinion) { CurrentState = State.Approaching; HasToken = true; return; } float dist = Vector3.Distance(transform.position, _player.position); if (dist > ChaseRange + 3f) { CurrentState = State.Chasing; return; } FaceTarget(); _agent.speed = WalkSpeed; Vector3 side = Vector3.Cross(Vector3.up, (_player.position - transform.position).normalized); _strafeChangeTimer -= Time.deltaTime; if (_strafeChangeTimer <= 0) { _strafeDirection *= -1; _strafeChangeTimer = Random.Range(2f, 4f); } _agent.SetDestination(transform.position + side * _strafeDirection * 2f); _timer -= Time.deltaTime; if (_timer <= 0 && !HasToken) TryGetToken(); }

    void LogicApproaching()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (!IsSimpleMinion)
        {
            if (EnableJumpAttack && _jumpTimer <= 0 && dist >= ChaseRange - 2f) { StartCoroutine(JumpAttackRoutine()); return; }
            if (CanSummon && _summonTimer <= 0 && _activeMinions.Count < MaxMinions) { StartCoroutine(SummonRoutine()); return; }
            if (EnableHellfire && _hellfireTimer <= 0 && dist <= 10.0f) { StartCoroutine(HellfireRoutine()); return; }
            if (EnableRapidSkill && _skillTimer <= 0 && dist <= AttackRange + 3.0f) { StartCoroutine(RapidSkillRoutine()); return; }
        }
        
        if (dist <= AttackRange - 0.3f) {
            bool useCombo = Random.Range(0, 100) < 40; 
            StartCoroutine(AttackRoutine(useCombo));
            return;
        }

        _agent.speed = RunSpeed; 
        _agent.SetDestination(_player.position); 
        FaceTarget();
    }
    void LogicRetreating() { if (IsSimpleMinion) { CurrentState = State.Approaching; return; } FaceTarget(); _agent.speed = WalkSpeed; Vector3 back = (transform.position - _player.position).normalized; _agent.SetDestination(transform.position + back * 4.0f); _timer -= Time.deltaTime; if (_timer <= 0) { CurrentState = State.Strafing; _timer = 2.0f; } }
    IEnumerator SpawnRoutine() { CurrentState = State.Spawning; _agent.isStopped = true; _agent.velocity = Vector3.zero; if (TryGetComponent(out Collider col)) col.enabled = false; yield return new WaitForSeconds(SpawnDuration); if (col != null) col.enabled = true; _agent.isStopped = false; CurrentState = State.Chasing; if (IsSimpleMinion) HasToken = true; }

    // [CẬP NHẬT] Thêm Trail vào đòn đánh thường
    IEnumerator AttackRoutine(bool isCombo)
    {
        CurrentState = State.Attacking;
        _agent.velocity = Vector3.zero; _agent.isStopped = true;

        FaceTarget(); 
        _animator.SetTrigger("Attack1");
        
        // [ĐÃ XÓA] SetTrail(true); -> Để Animation Event tự bật
        yield return new WaitForSeconds(Attack1HitTime);
        DealDamageToPlayer(DamageAmount, 0f); 
        yield return new WaitForSeconds(Attack1Duration - Attack1HitTime);
        // [ĐÃ XÓA] SetTrail(false); -> Để Animation Event tự tắt

        if (isCombo && Vector3.Distance(transform.position, _player.position) <= AttackRange + 0.8f)
        {
            FaceTarget(); 
            _animator.SetTrigger("Attack2");
            // [ĐÃ XÓA] SetTrail(true);
            yield return new WaitForSeconds(Attack2HitTime);
            DealDamageToPlayer(DamageAmount, 0f);
            yield return new WaitForSeconds(Attack2Duration - Attack2HitTime);
            // [ĐÃ XÓA] SetTrail(false);
        }
        else yield return new WaitForSeconds(0.2f);

        if (!IsSimpleMinion) 
        {
            ReturnToken();
            if (Random.Range(0, 100) < DodgeChance) StartCoroutine(JumpBackRoutine());
            else { _agent.isStopped = false; CurrentState = State.Retreating; _timer = 1.5f; }
        }
        else
        {
            _agent.isStopped = false; CurrentState = State.Approaching; yield return new WaitForSeconds(0.5f); 
        }
    }

    IEnumerator JumpAttackRoutine()
    {
        CurrentState = State.Attacking;
        _jumpTimer = JumpCooldown;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        FaceTarget();

        // 1. GỒNG
        _animator.SetTrigger("SkillCharge");
        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(true); 
        yield return new WaitForSeconds(JumpChargeDuration); 

        // 2. BẮT ĐẦU NHẢY (Animation lấy đà)
        _animator.SetTrigger("JumpAttack"); 
        yield return new WaitForSeconds(JumpTakeOffTime);

        // --- BAY PARABOL ---
        Vector3 startPos = transform.position;
        Vector3 targetPos = _player.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 3.0f, NavMesh.AllAreas))
        {
            targetPos = hit.position;
        }

        _agent.enabled = false; // Tắt Agent để bay lên trời

        float flyDuration = JumpLandTime - JumpTakeOffTime;
        float elapsedTime = 0f;

        // [GIỮ LẠI] Bật Trail bằng Code vì đang bay thủ công
        SetTrail(true);

        while (elapsedTime < flyDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / flyDuration; 
            
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += JumpHeight * 4.0f * t * (1.0f - t); // Công thức Parabol
            
            transform.position = currentPos; 
            yield return null;
        }

        transform.position = targetPos; 
        _agent.enabled = true; // Bật lại Agent
        
        // [GIỮ LẠI] Tắt Trail khi chạm đất
        SetTrail(false);
        // -------------------------

        // 3. CHẠM ĐẤT
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        // Sinh VFX nổ
        if (JumpAttackVFX != null)
        {
            GameObject fx = Instantiate(JumpAttackVFX, transform.position, Quaternion.identity);
            Destroy(fx, 3.0f);
        }

        // Gây sát thương
        Collider[] hits = Physics.OverlapSphere(transform.position, JumpRadius);
        foreach (var h in hits)
        {
            if (h.CompareTag("Player"))
            {
                DealDamageToPlayer(JumpDamage, 0f); // Force = 0 (Không stun)
            }
        }
        
        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(false); 

        // 4. HỒI PHỤC (Chờ hết animation đứng dậy)
        float remainingTime = JumpAnimTotalDuration - JumpLandTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        _agent.isStopped = false;
        ReturnToken(); 
        CurrentState = State.Strafing;
    }

    IEnumerator RapidSkillRoutine()
    {
        CurrentState = State.Attacking; 
        _skillTimer = SkillCooldown; 
        
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero; 
        FaceTarget(); 
        
        // 1. GỒNG (Charge)
        _animator.SetTrigger("SkillCharge");
        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(true);
        yield return new WaitForSeconds(ChargeDuration);
        
        // 2. LAO VÀO (Dash)
        _agent.isStopped = false; 
        _agent.speed = DashInSpeed; 
        _agent.acceleration = 100f; 
        _agent.SetDestination(_player.position);
        _animator.SetTrigger("SkillDash"); 
        
        float t = 0; 
        // Lao trong tối đa 1 giây hoặc khi đến gần đích
        while (_agent.remainingDistance > AttackRange - 0.2f && t < 1.0f) 
        { 
            t += Time.deltaTime; 
            yield return null; 
        }
        
        // Dừng lại
        _agent.velocity = Vector3.zero; 
        _agent.isStopped = true; 

        // 3. KIỂM TRA TRÚNG HAY TRƯỢT
        float finalDist = Vector3.Distance(transform.position, _player.position);

        // Chỉ chém nếu Player vẫn ở gần (AttackRange + 1.0f là độ du di)
        if (finalDist <= AttackRange + 1.0f)
        {
            FaceTarget(); 
            _animator.SetFloat("AttackSpeed", SkillAnimSpeed);
            
            // [LƯU Ý] Ở đây KHÔNG CÓ lệnh SetTrail(true) 
            // Bạn nhớ vào Animation "RapidSlash" add Event: EnableTrail ở đầu, DisableTrail ở cuối

            for (int i = 0; i < RapidHitCount; i++)
            {
                // Nếu đang chém mà Player chạy xa quá thì dừng
                if (Vector3.Distance(transform.position, _player.position) > AttackRange + 2.0f) break;

                FaceTarget(); 
                _animator.Play("RapidSlash", 0, 0f); 
                DealDamageToPlayer(DamageAmount * 0.5f, 0f); // Force = 0 (Không stun)
                yield return new WaitForSeconds(RapidHitInterval);
            }
            
            _animator.SetFloat("AttackSpeed", 1.0f);
        }
        else
        {
            // Nếu trượt thì đứng đơ ra 0.5s (bị hớ)
            yield return new WaitForSeconds(0.5f); 
        }

        // 4. KẾT THÚC
        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(false);
        ReturnToken(); 
        yield return StartCoroutine(JumpBackRoutine());
    }

    // ... (Giữ nguyên Hellfire, Summon, JumpBack, Die, Helper functions) ...
    IEnumerator HellfireRoutine() { CurrentState = State.Attacking; _hellfireTimer = HellfireCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; FaceTarget(); _animator.SetTrigger("CastSpell"); yield return new WaitForSeconds(HellfireCastTime); float duration = HellfireDuration; List<Vector3> allFirePositions = new List<Vector3>(); while (duration > 0) { if (CurrentState != State.Attacking) yield break; if (_player != null) { Vector3 targetPos = _player.position; targetPos.y = transform.position.y + 0.05f; bool playerPosSafe = true; foreach (Vector3 exist in allFirePositions) { if (Vector3.Distance(targetPos, exist) < MinSpawnDistance) { playerPosSafe = false; break; } } if (playerPosSafe) { SpawnAoE(targetPos); allFirePositions.Add(targetPos); } } int randomCount = Random.Range(2, 4); for (int i = 0; i < randomCount; i++) { Vector3 bestPos = Vector3.zero; bool foundPos = false; for (int attempt = 0; attempt < 15; attempt++) { Vector3 offset = Random.insideUnitSphere * 8.0f; if (offset.magnitude < 3.0f) offset = offset.normalized * 3.0f; Vector3 candidate = transform.position + offset; candidate.y = transform.position.y + 0.05f; bool tooClose = false; foreach (Vector3 exist in allFirePositions) { if (Vector3.Distance(candidate, exist) < MinSpawnDistance) { tooClose = true; break; } } if (!tooClose) { bestPos = candidate; foundPos = true; break; } } if (foundPos) { SpawnAoE(bestPos); allFirePositions.Add(bestPos); } } yield return new WaitForSeconds(WaveInterval); duration -= WaveInterval; FaceTarget(); } ReturnToken(); _agent.isStopped = false; CurrentState = State.Strafing; }
    IEnumerator SummonRoutine() { CurrentState = State.Attacking; _summonTimer = SummonCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; FaceTarget(); _animator.SetTrigger("CastSpell"); yield return new WaitForSeconds(SummonCastTime); int spawnCount = MaxMinions - _activeMinions.Count; for (int i = 0; i < spawnCount; i++) { Vector3 rnd = transform.position + Random.insideUnitSphere * SummonRadius; NavMeshHit hit; if (NavMesh.SamplePosition(rnd, out hit, 2.0f, NavMesh.AllAreas)) { if (SummonVFX != null) Instantiate(SummonVFX, hit.position, Quaternion.identity); if (MinionPrefab != null) { _activeMinions.Add(Instantiate(MinionPrefab, hit.position, Quaternion.LookRotation(transform.forward))); } } yield return new WaitForSeconds(0.3f); } yield return new WaitForSeconds(1.0f); ReturnToken(); _agent.isStopped = false; CurrentState = State.Strafing; }
    IEnumerator JumpBackRoutine() { CurrentState = State.Dodging; _agent.isStopped = false; _animator.SetTrigger("Dodge"); _agent.speed = JumpBackSpeed; _agent.acceleration = 100f; _agent.SetDestination(transform.position - transform.forward * 3.5f); yield return new WaitForSeconds(JumpBackDuration); _agent.speed = WalkSpeed; _agent.acceleration = 8f; _agent.velocity = Vector3.zero; CurrentState = State.Strafing; _timer = 2.0f; }
    IEnumerator HitReactionRoutine() { if (CurrentState == State.Dead) yield break; CurrentState = State.Hit; _agent.isStopped = true; _agent.velocity = Vector3.zero; _animator.SetTrigger("Hit"); yield return new WaitForSeconds(0.5f); _agent.isStopped = false; if (IsSimpleMinion) CurrentState = State.Approaching; else CurrentState = State.Strafing; }
    public HitResult TakeDamage(DamageInfo info) { if (CurrentState == State.Dead) return HitResult.Ignored; CurrentHealth -= info.amount; UpdateHealthUI(); if (CurrentHealth <= 0) { Die(); return HitResult.Critical; } if (CurrentState == State.Attacking || CurrentState == State.Spawning) { return HitResult.Hit; } bool shouldStagger = IsEnraged ? (Random.value < 0.3f) : true; if (shouldStagger && CurrentState != State.Dodging) { StopAllCoroutines(); StartCoroutine(HitReactionRoutine()); } return HitResult.Hit; }
    void DealDamageToPlayer(float dmg, float force) { if (_playerCombat != null && Vector3.Distance(transform.position, _player.position) <= AttackRange + 1.0f) { _playerCombat.TakeDamage(new DamageInfo { amount = dmg, attacker = gameObject, hitPoint = _player.position + Vector3.up, type = DamageType.Physical, knockbackForce = force, duration = 0f }); } }
    void SpawnAoE(Vector3 pos) { if (AoEPrefab != null) { GameObject aoe = Instantiate(AoEPrefab, pos, Quaternion.identity); Destroy(aoe, 4.0f); var zone = aoe.GetComponent<MonoBehaviour>(); if (zone != null) { aoe.SendMessage("Setup", new object[] { gameObject, DamageAmount * 1.5f }, SendMessageOptions.DontRequireReceiver); } } }
    void Die() 
    { 
        CurrentState = State.Dead; 
        
        if (_agent != null) 
        { 
            _agent.isStopped = true; 
            _agent.velocity = Vector3.zero; 
        }
        
        StopAllCoroutines(); 
        
        if (_animator != null) _animator.SetTrigger("Die"); 
        
        if (TryGetComponent(out Collider col)) col.enabled = false; 

        // ==========================================
        // TẮT THANH MÁU NGAY LẬP TỨC
        // ==========================================
        if (HealthBarObj != null) 
        {
            HealthBarObj.SetActive(false);
        }

        // ==========================================
        // CHỈ HIỆN BANNER NẾU KHÔNG PHẢI LÀ MINION
        // ==========================================
        if (!IsSimpleMinion)
        {
            if (BossDefeatBanner.Instance != null)
        {
            BossDefeatBanner.Instance.ShowBanner(); 
        }
        }

        // SINH HIỆU ỨNG BỤI
        if (DeathDustVFX != null)
        {
            GameObject dust = Instantiate(DeathDustVFX, transform.position, Quaternion.identity);
            Destroy(dust, 5.0f);
        }

        // GỌI COROUTINE LÀM MỜ XÁC
        StartCoroutine(FadeOutCorpseRoutine());

        this.enabled = false; 
    }

    // COROUTINE LÀM MỜ DẦN MÔ HÌNH
    // COROUTINE LÀM MỜ DẦN MÔ HÌNH (Đã sửa lỗi VFX Shader)
    // COROUTINE LÀM MỜ DẦN MÔ HÌNH (Đổi Transparent bằng Code)
    IEnumerator FadeOutCorpseRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        System.Collections.Generic.List<Renderer> validRenderers = new System.Collections.Generic.List<Renderer>();
        
        foreach (Renderer r in allRenderers)
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer) continue; 
            validRenderers.Add(r);
        }

        // --- BƯỚC CHUYỂN OPAQUE SANG TRANSPARENT ---
        foreach (Renderer r in validRenderers)
        {
            if (r == null || r.materials == null) continue;
            foreach (Material m in r.materials)
            {
                // Can thiệp vào URP Shader để ép nó thành Transparent
                if (m.HasProperty("_Surface"))
                {
                    m.SetFloat("_Surface", 1.0f); // 1 = Transparent
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0); // Tắt ZWrite để tránh lỗi đè hình
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
            }
        }
        // ----------------------------------------------

        float elapsed = 0f;

        while (elapsed < FadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / FadeOutDuration);

            foreach (Renderer r in validRenderers)
            {
                if (r == null || r.materials == null) continue;

                foreach (Material m in r.materials)
                {
                    if (m.HasColor("_BaseColor")) 
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = alpha;
                        m.SetColor("_BaseColor", c);
                    }
                    else if (m.HasColor("_Color")) 
                    {
                        Color c = m.GetColor("_Color");
                        c.a = alpha;
                        m.SetColor("_Color", c);
                    }
                }
            }
            yield return null; 
        }

        Destroy(gameObject);
    }    
    void UpdateHealthUI() { if (HealthBarSlider != null) HealthBarSlider.value = CurrentHealth; }
    bool CheckForPlayer() { float d = Vector3.Distance(transform.position, _player.position); if (d <= DetectionRange) { CurrentState = State.Chasing; return true; } return false; }
    void HandleAnimation() { Vector3 v = transform.InverseTransformDirection(_agent.velocity); _animator.SetFloat("InputX", v.z, 0.1f, Time.deltaTime); _animator.SetFloat("InputY", -v.x, 0.1f, Time.deltaTime); } 
    void TryGetToken() { if (_currentAttackers < MAX_ATTACKERS) { _currentAttackers++; HasToken = true; CurrentState = State.Approaching; } }
    void ReturnToken() { if (HasToken) { _currentAttackers--; HasToken = false; } }
    void FaceTarget() { if (_player == null) return; Vector3 d = (_player.position - transform.position).normalized; d.y = 0; if (d != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 15f); }
    void OnDisable() { ReturnToken(); }
    void OnDrawGizmosSelected() { Vector3 c = transform.position + Vector3.up; Gizmos.color = Color.green; Gizmos.DrawWireSphere(c, DetectionRange); Gizmos.color = Color.red; Gizmos.DrawWireSphere(c, AttackRange); }
    
    // Hàm bật tắt Trail nhanh
    void SetTrail(bool active) { if (WeaponTrail != null) WeaponTrail.emitting = active; }
    void HandleDustVFX()
    {
        if (MoveDustVFX == null) return;

        // Boss đang di chuyển nếu vận tốc > 0.1 và không bị cấm di chuyển
        bool isMoving = _agent != null && _agent.velocity.sqrMagnitude > 0.1f;
        
        // Điều kiện để bụi được phép chạy
        bool canPlay = isMoving && 
                       CurrentState != State.Dead && 
                       CurrentState != State.Spawning && 
                       CurrentState != State.Idle;

        if (canPlay)
        {
            if (!MoveDustVFX.isPlaying) MoveDustVFX.Play();
        }
        else
        {
            if (MoveDustVFX.isPlaying) MoveDustVFX.Stop();
        }
    } 
}