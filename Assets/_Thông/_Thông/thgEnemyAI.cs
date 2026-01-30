using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class thgEnemyAI : MonoBehaviour, IDamageable
{
    // ==========================================
    // 1. SETTINGS
    // ==========================================
    [Header("Stats")]
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float RunSpeed = 3.5f; 
    public float WalkSpeed = 1.5f;

    [Header("UI Settings")]
    public Slider HealthBarSlider;
    public GameObject HealthBarObj;

    [Header("Minion Settings (QUAN TRỌNG)")]
    public bool PlaySpawnAnim = false;     // Minion: TÍCH
    public float SpawnDuration = 2.0f;     
    public bool IsSimpleMinion = false;    // <--- TÍCH CÁI NÀY: Minion sẽ chạy thẳng vào đánh, ko đi vòng vèo

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

    [Header("SKILL 2: HELLFIRE")]
    public bool EnableHellfire = true;     
    public float HellfireCastTime = 1.5f;  
    public GameObject AoEPrefab;
    public float HellfireCooldown = 15f;
    public float HellfireDuration = 5.0f;
    public float SpawnRate = 0.5f;

    [Header("SKILL 3: SUMMON")]
    public bool CanSummon = false;         
    public float SummonCastTime = 2.0f;    
    public GameObject MinionPrefab;
    public GameObject SummonVFX;
    public int MaxMinions = 3;
    public float SummonCooldown = 20f;
    public float SummonRadius = 3.0f;

    [Header("Death Settings")]
    public float CorpseDestroyTime = 5.0f;

    // ==========================================
    // SYSTEM VARIABLES
    // ==========================================
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
    private float _strafeDirection = 1f;
    private float _strafeChangeTimer;
    private int _currentPatrolIndex = 0;
    private List<GameObject> _activeMinions = new List<GameObject>();
    private static int _currentAttackers = 0;
    private const int MAX_ATTACKERS = 2;

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

        if (!IsEnraged && CurrentHealth <= MaxHealth * 0.5f) ActivatePhase2();

        UpdateHealthUI();
        if (HealthBarObj != null && _mainCamera != null)
            HealthBarObj.transform.rotation = Quaternion.LookRotation(HealthBarObj.transform.position - _mainCamera.transform.position);

        if (_player == null || CurrentState == State.Dead) return;
        if (CurrentState == State.Spawning) return;

        if (_skillTimer > 0) _skillTimer -= Time.deltaTime; 
        if (_hellfireTimer > 0) _hellfireTimer -= Time.deltaTime;
        if (_summonTimer > 0) _summonTimer -= Time.deltaTime;

        _activeMinions.RemoveAll(item => item == null);
        HandleAnimation();

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

    void ActivatePhase2()
    {
        IsEnraged = true;
        RunSpeed *= 1.5f; DashInSpeed *= 1.3f;
        SkillCooldown /= 2.0f; HellfireCooldown /= 2.0f; SummonCooldown /= 2.0f;
        _skillTimer = 0; _hellfireTimer = 0; _summonTimer = 0;
        transform.localScale = transform.localScale * 1.3f;
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rends) { r.material.SetColor("_BaseColor", Color.red); r.material.color = Color.red; }
        if (SummonVFX != null) Instantiate(SummonVFX, transform.position, Quaternion.identity);
    }

    // ==========================================
    // LOGIC DI CHUYỂN
    // ==========================================

    void LogicIdle()
    {
        if (CheckForPlayer()) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0) NextPatrolPoint();
    }

    void LogicPatrol()
    {
        if (CheckForPlayer()) return;
        _agent.speed = PatrolSpeed;
        if (_agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 dir = _agent.velocity.normalized; dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f) { CurrentState = State.Idle; _timer = IdleTime; }
    }

    void NextPatrolPoint()
    {
        if (PatrolPoints.Length == 0) return;
        _currentPatrolIndex = (_currentPatrolIndex + 1) % PatrolPoints.Length;
        CurrentState = State.Patrol;
        _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position);
    }

    void LogicChasing()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        
        // Nếu người chơi chạy quá xa -> Quay về đi tuần
        if (dist > DetectionRange * 1.5f) { CurrentState = State.Patrol; _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position); return; }
        
        // Nếu đã đến gần (Tầm ChaseRange)
        if (dist <= ChaseRange) 
        { 
            // --- SỬA LOGIC Ở ĐÂY ---
            if (IsSimpleMinion)
            {
                // Nếu là Minion đơn giản: BỎ QUA trạng thái Strafing (đi vòng), lao vào Múc luôn!
                CurrentState = State.Approaching;
                HasToken = true; // Cấp quyền đánh luôn, không cần xếp hàng
                return;
            }
            else
            {
                // Nếu là Boss: Đi vòng vòng tỏ vẻ nguy hiểm
                CurrentState = State.Strafing; 
                _timer = 2f; 
                return; 
            }
        }

        _agent.speed = RunSpeed; _agent.SetDestination(_player.position);
        if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized);
    }

    void LogicStrafing()
    {
        // Nếu lỡ rơi vào đây mà là SimpleMinion thì thoát ngay
        if (IsSimpleMinion) { CurrentState = State.Approaching; HasToken = true; return; }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > ChaseRange + 3f) { CurrentState = State.Chasing; return; }
        
        FaceTarget(); _agent.speed = WalkSpeed;
        Vector3 side = Vector3.Cross(Vector3.up, (_player.position - transform.position).normalized);
        _strafeChangeTimer -= Time.deltaTime; if (_strafeChangeTimer <= 0) { _strafeDirection *= -1; _strafeChangeTimer = Random.Range(2f, 4f); }
        _agent.SetDestination(transform.position + side * _strafeDirection * 2f);
        
        _timer -= Time.deltaTime; 
        if (_timer <= 0 && !HasToken) TryGetToken();
    }

    void LogicApproaching()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (CanSummon && _summonTimer <= 0 && _activeMinions.Count < MaxMinions) { StartCoroutine(SummonRoutine()); return; }
        if (EnableHellfire && _hellfireTimer <= 0 && dist <= 10.0f) { StartCoroutine(HellfireRoutine()); return; }
        if (EnableRapidSkill && _skillTimer <= 0 && dist <= AttackRange + 3.0f) { StartCoroutine(RapidSkillRoutine()); return; }
        
        // Logic Đánh thường
        if (dist <= AttackRange - 0.3f) {
            bool useCombo = Random.Range(0, 100) < 40; 
            StartCoroutine(AttackRoutine(useCombo));
            return;
        }

        // Nếu là Simple Minion thì cứ chạy thẳng tới, không cần suy nghĩ
        _agent.speed = RunSpeed; 
        _agent.SetDestination(_player.position); 
        FaceTarget();
    }

    void LogicRetreating()
    {
        // Minion đơn giản đánh xong không cần lùi, cứ đứng đó đánh tiếp
        if (IsSimpleMinion) { CurrentState = State.Approaching; return; }

        FaceTarget(); _agent.speed = WalkSpeed;
        Vector3 back = (transform.position - _player.position).normalized;
        _agent.SetDestination(transform.position + back * 4.0f);
        _timer -= Time.deltaTime; if (_timer <= 0) { CurrentState = State.Strafing; _timer = 2.0f; }
    }

    // ==========================================
    // COROUTINES
    // ==========================================

    IEnumerator SpawnRoutine()
    {
        CurrentState = State.Spawning; 
        _agent.isStopped = true; _agent.velocity = Vector3.zero;
        if (TryGetComponent(out Collider col)) col.enabled = false;
        yield return new WaitForSeconds(SpawnDuration);
        if (col != null) col.enabled = true;
        _agent.isStopped = false;
        
        // Sinh ra xong là lao vào đánh luôn
        CurrentState = State.Chasing; 
        if (IsSimpleMinion) HasToken = true; 
    }

    IEnumerator AttackRoutine(bool isCombo)
    {
        CurrentState = State.Attacking;
        _agent.velocity = Vector3.zero; _agent.isStopped = true;

        FaceTarget(); _animator.SetTrigger("Attack1");
        yield return new WaitForSeconds(Attack1HitTime);
        DealDamageToPlayer(DamageAmount, 3f);
        yield return new WaitForSeconds(Attack1Duration - Attack1HitTime);

        if (isCombo && Vector3.Distance(transform.position, _player.position) <= AttackRange + 0.8f)
        {
            FaceTarget(); _animator.SetTrigger("Attack2");
            yield return new WaitForSeconds(Attack2HitTime);
            DealDamageToPlayer(DamageAmount, 3f);
            yield return new WaitForSeconds(Attack2Duration - Attack2HitTime);
        }
        else yield return new WaitForSeconds(0.2f);

        // Đánh xong: Nếu là Boss thì trả Token, lùi về
        // Nếu là Simple Minion: Giữ Token để đánh tiếp (Spam đòn)
        if (!IsSimpleMinion) 
        {
            ReturnToken();
            if (Random.Range(0, 100) < DodgeChance) StartCoroutine(JumpBackRoutine());
            else { _agent.isStopped = false; CurrentState = State.Retreating; _timer = 1.5f; }
        }
        else
        {
            // Logic Minion đơn giản: Đánh xong đứng lại thở 1 tí rồi đánh tiếp
            _agent.isStopped = false;
            CurrentState = State.Approaching; 
            yield return new WaitForSeconds(0.5f); // Nghỉ 0.5s giữa các cú đấm
        }
    }

    IEnumerator RapidSkillRoutine()
    {
        CurrentState = State.Attacking; _skillTimer = SkillCooldown; _agent.isStopped = false;
        _agent.velocity = Vector3.zero; _agent.isStopped = true; FaceTarget(); _animator.SetTrigger("SkillCharge");
        yield return new WaitForSeconds(ChargeDuration);

        _agent.isStopped = false; _agent.speed = DashInSpeed; _agent.acceleration = 100f; _agent.SetDestination(_player.position);
        float t = 0; while (Vector3.Distance(transform.position, _player.position) > AttackRange - 0.2f && t < 1.0f) { t += Time.deltaTime; yield return null; }

        _agent.velocity = Vector3.zero; _agent.isStopped = true; _animator.SetFloat("AttackSpeed", SkillAnimSpeed);
        for (int i = 0; i < RapidHitCount; i++)
        {
            FaceTarget(); _animator.Play("RapidSlash", 0, 0f);
            DealDamageToPlayer(DamageAmount * 0.5f, 1f);
            yield return new WaitForSeconds(RapidHitInterval);
        }
        _animator.SetFloat("AttackSpeed", 1.0f);
        ReturnToken(); yield return StartCoroutine(JumpBackRoutine());
    }

    IEnumerator HellfireRoutine()
    {
        CurrentState = State.Attacking; _hellfireTimer = HellfireCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero;
        FaceTarget(); _animator.SetTrigger("CastSpell"); 
        yield return new WaitForSeconds(HellfireCastTime);
        float duration = HellfireDuration;
        while (duration > 0)
        {
            if (_player != null) { Vector3 t = _player.position; t.y = transform.position.y + 0.05f; SpawnAoE(t); }
            Vector3 rnd = _player.position + Random.insideUnitSphere * 5.0f; rnd.y = transform.position.y + 0.05f; SpawnAoE(rnd);
            yield return new WaitForSeconds(SpawnRate);
            duration -= SpawnRate; FaceTarget();
        }
        ReturnToken(); _agent.isStopped = false; CurrentState = State.Strafing;
    }

    IEnumerator SummonRoutine()
    {
        CurrentState = State.Attacking; _summonTimer = SummonCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero;
        FaceTarget(); _animator.SetTrigger("CastSpell");
        yield return new WaitForSeconds(SummonCastTime);
        int spawnCount = MaxMinions - _activeMinions.Count;
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 rnd = transform.position + Random.insideUnitSphere * SummonRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(rnd, out hit, 2.0f, NavMesh.AllAreas))
            {
                if (SummonVFX != null) Instantiate(SummonVFX, hit.position, Quaternion.identity);
                if (MinionPrefab != null) { _activeMinions.Add(Instantiate(MinionPrefab, hit.position, Quaternion.LookRotation(transform.forward))); }
            }
            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(1.0f);
        ReturnToken(); _agent.isStopped = false; CurrentState = State.Strafing;
    }

    IEnumerator JumpBackRoutine()
    {
        CurrentState = State.Dodging; _agent.isStopped = false; _animator.SetTrigger("Dodge");
        _agent.speed = JumpBackSpeed; _agent.acceleration = 100f;
        _agent.SetDestination(transform.position - transform.forward * 3.5f);
        yield return new WaitForSeconds(JumpBackDuration);
        _agent.speed = WalkSpeed; _agent.acceleration = 8f; _agent.velocity = Vector3.zero;
        CurrentState = State.Strafing; _timer = 2.0f;
    }

    IEnumerator HitReactionRoutine()
    {
        CurrentState = State.Hit; _agent.isStopped = true; _agent.velocity = Vector3.zero;
        _animator.SetTrigger("Hit");
        yield return new WaitForSeconds(0.5f);
        _agent.isStopped = false; 
        
        // Nếu là Minion đơn giản, bị đánh xong thì quay lại lao vào đánh tiếp, không đi vòng
        if (IsSimpleMinion) CurrentState = State.Approaching; 
        else CurrentState = State.Strafing;
    }

    // ==========================================
    // HELPERS
    // ==========================================

    public HitResult TakeDamage(DamageInfo info)
    {
        if (CurrentState == State.Dead) return HitResult.Ignored;
        CurrentHealth -= info.amount; UpdateHealthUI();
        if (CurrentHealth <= 0) { Die(); return HitResult.Critical; }

        bool shouldStagger = IsEnraged ? (Random.value < 0.3f) : true;
        if (shouldStagger && CurrentState != State.Dodging && CurrentState != State.Attacking) 
        { StopAllCoroutines(); StartCoroutine(HitReactionRoutine()); }
        return HitResult.Hit;
    }

    void DealDamageToPlayer(float dmg, float force)
    {
        if (_playerCombat != null && Vector3.Distance(transform.position, _player.position) <= AttackRange + 1.0f)
            _playerCombat.TakeDamage(new DamageInfo { amount = dmg, attacker = gameObject, hitPoint = _player.position + Vector3.up, type = DamageType.Physical, knockbackForce = force });
    }

    void SpawnAoE(Vector3 pos)
    {
        if (AoEPrefab != null) {
            GameObject aoe = Instantiate(AoEPrefab, pos, Quaternion.identity);
            if (aoe.TryGetComponent(out AoEZone z)) z.Setup(gameObject, DamageAmount * 1.5f);
        }
    }

    void Die()
    {
        CurrentState = State.Dead; ReturnToken(); _agent.isStopped = true; _agent.velocity = Vector3.zero; StopAllCoroutines();
        _animator.SetTrigger("Die");
        if (TryGetComponent(out Collider col)) col.enabled = false;
        if (HealthBarObj != null) HealthBarObj.SetActive(false);
        Destroy(gameObject, CorpseDestroyTime);
        this.enabled = false;
    }

    void UpdateHealthUI() { if (HealthBarSlider != null) HealthBarSlider.value = CurrentHealth; }
    bool CheckForPlayer() { float d = Vector3.Distance(transform.position, _player.position); if (d <= DetectionRange) { CurrentState = State.Chasing; return true; } return false; }
    void HandleAnimation() { Vector3 v = transform.InverseTransformDirection(_agent.velocity); _animator.SetFloat("InputX", v.z, 0.1f, Time.deltaTime); _animator.SetFloat("InputY", -v.x, 0.1f, Time.deltaTime); }
    void TryGetToken() { if (_currentAttackers < MAX_ATTACKERS) { _currentAttackers++; HasToken = true; CurrentState = State.Approaching; } }
    void ReturnToken() { if (HasToken) { _currentAttackers--; HasToken = false; } }
    void FaceTarget() { if (_player == null) return; Vector3 d = (_player.position - transform.position).normalized; d.y = 0; if (d != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 15f); }
    void OnDisable() { ReturnToken(); }
    void OnDrawGizmosSelected() { Vector3 c = transform.position + Vector3.up; Gizmos.color = Color.green; Gizmos.DrawWireSphere(c, DetectionRange); Gizmos.color = Color.red; Gizmos.DrawWireSphere(c, AttackRange); }
}