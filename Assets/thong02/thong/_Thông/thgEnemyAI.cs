using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class thgEnemyAI : MonoBehaviour, IDamageable
{
    // ==========================================
    // 1. SETTINGS (CÀI ĐẶT)
    // ==========================================
    [Header("Stats")]
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float RunSpeed = 3.5f; 
    public float WalkSpeed = 1.5f;

    [Header("UI Settings")]
    public Slider HealthBarSlider;
    public GameObject HealthBarObj;

    [Header("Spawn Settings (Dành cho Minion)")]
    public bool PlaySpawnAnim = false;     // Minion: TÍCH, Boss: BỎ
    public float SpawnDuration = 2.0f;     // Thời gian đứng đơ khi chui từ đất lên

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

    [Header("SKILL 1: RAPID BARRAGE (Lao vào chém)")]
    public bool EnableRapidSkill = true;   // Boss: TÍCH, Minion: BỎ
    public float SkillCooldown = 8.0f;     
    public int RapidHitCount = 6;          
    public float RapidHitInterval = 0.2f;  
    public float SkillAnimSpeed = 2.5f;    
    public float ChargeDuration = 1.0f;    
    public float DashInSpeed = 12.0f;      

    [Header("SKILL 2: HELLFIRE (Thả lửa)")]
    public bool EnableHellfire = true;     // Boss: TÍCH, Minion: BỎ
    public float HellfireCastTime = 1.5f;  // <--- THỜI GIAN NIỆM CHÚ
    public GameObject AoEPrefab;
    public float HellfireCooldown = 15f;
    public float HellfireDuration = 5.0f;
    public float SpawnRate = 0.5f;

    [Header("SKILL 3: SUMMON (Gọi đệ)")]
    public bool CanSummon = false;         // Boss: TÍCH, Minion: BỎ
    public float SummonCastTime = 2.0f;    // <--- THỜI GIAN NIỆM CHÚ
    public GameObject MinionPrefab;
    public GameObject SummonVFX;
    public int MaxMinions = 3;
    public float SummonCooldown = 20f;
    public float SummonRadius = 3.0f;

    [Header("Death Settings")]
    public float CorpseDestroyTime = 5.0f;

    // ==========================================
    // 2. SYSTEM VARIABLES (BIẾN HỆ THỐNG)
    // ==========================================
    [Header("Debug Info")]
    public bool HasToken = false;
    public State CurrentState;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private IDamageable _playerCombat; 
    private Camera _mainCamera;

    // Các loại Timer
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
        _mainCamera = Camera.main;

        if (HealthBarSlider != null) { HealthBarSlider.maxValue = MaxHealth; HealthBarSlider.value = CurrentHealth; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { _player = p.transform; _playerCombat = p.GetComponent<IDamageable>(); }

        // --- LOGIC QUYẾT ĐỊNH KHỞI ĐẦU ---
        if (PlaySpawnAnim)
        {
            // Nếu là Minion (có tích PlaySpawnAnim) -> Chạy quy trình sinh ra
            StartCoroutine(SpawnRoutine());
        }
        else 
        {
            // Nếu là Boss/Quái thường -> Đi tuần hoặc đứng chơi
            if (PatrolPoints != null && PatrolPoints.Length > 0)
            {
                CurrentState = State.Patrol;
                _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position);
            }
            else
            {
                CurrentState = State.Idle;
            }
        }
    }

    void Update()
    {
        if (CurrentHealth <= 0 && CurrentState != State.Dead) { Die(); return; }

        UpdateHealthUI();
        if (HealthBarObj != null && _mainCamera != null)
            HealthBarObj.transform.rotation = Quaternion.LookRotation(HealthBarObj.transform.position - _mainCamera.transform.position);

        if (_player == null || CurrentState == State.Dead) return;
        
        // Nếu đang chui từ đất lên thì chặn mọi logic khác
        if (CurrentState == State.Spawning) return;

        // --- GIẢM HỒI CHIÊU ---
        if (_skillTimer > 0) _skillTimer -= Time.deltaTime; 
        if (_hellfireTimer > 0) _hellfireTimer -= Time.deltaTime;
        if (_summonTimer > 0) _summonTimer -= Time.deltaTime;

        _activeMinions.RemoveAll(item => item == null); // Dọn dẹp list đệ tử

        HandleAnimation();

        switch (CurrentState)
        {
            case State.Idle: LogicIdle(); break;
            case State.Patrol: LogicPatrol(); break;
            case State.Chasing: LogicChasing(); break;
            case State.Strafing: LogicStrafing(); break;
            case State.Approaching: LogicApproaching(); break; // Trung tâm điều khiển Skill
            case State.Attacking:
            case State.Dodging:
            case State.Hit: FaceTarget(); break;
            case State.Retreating: LogicRetreating(); break;
        }
    }

    // ==========================================
    // 3. LOGIC DI CHUYỂN & CHỌN SKILL
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
        if (dist > DetectionRange * 1.5f) { CurrentState = State.Patrol; _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position); return; }
        if (dist <= ChaseRange) { CurrentState = State.Strafing; _timer = 2f; return; }
        _agent.speed = RunSpeed; _agent.SetDestination(_player.position);
        if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized);
    }

    void LogicStrafing()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > ChaseRange + 3f) { CurrentState = State.Chasing; return; }
        FaceTarget(); _agent.speed = WalkSpeed;
        Vector3 side = Vector3.Cross(Vector3.up, (_player.position - transform.position).normalized);
        _strafeChangeTimer -= Time.deltaTime; if (_strafeChangeTimer <= 0) { _strafeDirection *= -1; _strafeChangeTimer = Random.Range(2f, 4f); }
        _agent.SetDestination(transform.position + side * _strafeDirection * 2f);
        _timer -= Time.deltaTime; if (_timer <= 0 && !HasToken) TryGetToken();
    }

    // --- TRUNG TÂM ĐIỀU KHIỂN SKILL ---
    void LogicApproaching()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        // 1. SUMMON (Chỉ Boss: CanSummon = true)
        if (CanSummon && _summonTimer <= 0 && _activeMinions.Count < MaxMinions)
        {
            StartCoroutine(SummonRoutine());
            return;
        }

        // 2. HELLFIRE (Chỉ Boss: EnableHellfire = true)
        if (EnableHellfire && _hellfireTimer <= 0 && dist <= 10.0f)
        {
            StartCoroutine(HellfireRoutine());
            return;
        }

        // 3. RAPID SLASH (Chỉ Boss: EnableRapidSkill = true)
        if (EnableRapidSkill && _skillTimer <= 0 && dist <= AttackRange + 3.0f)
        {
            StartCoroutine(RapidSkillRoutine());
            return;
        }

        // 4. ĐÁNH THƯỜNG (Minion và Boss)
        if (dist <= AttackRange - 0.3f)
        {
            bool useCombo = Random.Range(0, 100) < 40; 
            StartCoroutine(AttackRoutine(useCombo));
            return;
        }

        _agent.speed = RunSpeed; _agent.SetDestination(_player.position); FaceTarget();
    }

    void LogicRetreating()
    {
        FaceTarget(); _agent.speed = WalkSpeed;
        Vector3 back = (transform.position - _player.position).normalized;
        _agent.SetDestination(transform.position + back * 4.0f);
        _timer -= Time.deltaTime; if (_timer <= 0) { CurrentState = State.Strafing; _timer = 2.0f; }
    }

    // ==========================================
    // 4. COMBAT COROUTINES (HÀNH ĐỘNG)
    // ==========================================

    IEnumerator SpawnRoutine()
    {
        CurrentState = State.Spawning; 
        
        // Khóa di chuyển để diễn cảnh chui lên
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        if (TryGetComponent(out Collider col)) col.enabled = false;

        // Lưu ý: Không gọi _animator.Play() ở đây nữa
        // Hãy để Animator tự chạy Default State (Màu cam) là "Spawn"

        // Đợi đúng thời gian bạn cài trong Inspector
        yield return new WaitForSeconds(SpawnDuration);

        // Mở lại hoạt động
        if (col != null) col.enabled = true;
        _agent.isStopped = false;
        
        CurrentState = State.Chasing; // Minion sinh ra là lao vào đánh luôn
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

        ReturnToken();
        if (Random.Range(0, 100) < DodgeChance) StartCoroutine(JumpBackRoutine());
        else { _agent.isStopped = false; CurrentState = State.Retreating; _timer = 1.5f; }
    }

    IEnumerator RapidSkillRoutine()
    {
        CurrentState = State.Attacking; _skillTimer = SkillCooldown; _agent.isStopped = false;
        
        // Gồng
        _agent.velocity = Vector3.zero; _agent.isStopped = true; FaceTarget(); _animator.SetTrigger("SkillCharge");
        yield return new WaitForSeconds(ChargeDuration);

        // Lao vào
        _agent.isStopped = false; _agent.speed = DashInSpeed; _agent.acceleration = 100f; _agent.SetDestination(_player.position);
        float t = 0; while (Vector3.Distance(transform.position, _player.position) > AttackRange - 0.2f && t < 1.0f) { t += Time.deltaTime; yield return null; }

        // Chém liên hoàn
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
        
        FaceTarget(); 
        _animator.SetTrigger("CastSpell"); 

        // --- CHỜ THỜI GIAN NIỆM CHÚ ---
        yield return new WaitForSeconds(HellfireCastTime);
        // ------------------------------

        float duration = HellfireDuration;
        while (duration > 0)
        {
            if (_player != null) 
            {
                // Ép độ cao vòng lửa xuống mặt đất (bằng chân Boss)
                Vector3 targetPos = _player.position;
                targetPos.y = transform.position.y + 0.05f; 
                SpawnAoE(targetPos);
            }
            
            Vector3 rnd = _player.position + Random.insideUnitSphere * 5.0f; 
            rnd.y = transform.position.y + 0.05f;
            SpawnAoE(rnd);

            yield return new WaitForSeconds(SpawnRate);
            duration -= SpawnRate; FaceTarget();
        }
        ReturnToken(); _agent.isStopped = false; CurrentState = State.Strafing;
    }

    IEnumerator SummonRoutine()
    {
        CurrentState = State.Attacking; _summonTimer = SummonCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero;
        
        FaceTarget(); 
        _animator.SetTrigger("CastSpell");

        // --- CHỜ THỜI GIAN NIỆM CHÚ ---
        yield return new WaitForSeconds(SummonCastTime);
        // ------------------------------

        int spawnCount = MaxMinions - _activeMinions.Count;
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 rnd = transform.position + Random.insideUnitSphere * SummonRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(rnd, out hit, 2.0f, NavMesh.AllAreas))
            {
                if (SummonVFX != null) Instantiate(SummonVFX, hit.position, Quaternion.identity);
                if (MinionPrefab != null) {
                    GameObject m = Instantiate(MinionPrefab, hit.position, Quaternion.LookRotation(transform.forward));
                    _activeMinions.Add(m);
                }
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
        _agent.isStopped = false; CurrentState = State.Strafing;
    }

    // ==========================================
    // 5. HELPER FUNCTIONS
    // ==========================================

    public HitResult TakeDamage(DamageInfo info)
    {
        if (CurrentState == State.Dead) return HitResult.Ignored;
        CurrentHealth -= info.amount; UpdateHealthUI();
        if (CurrentHealth <= 0) { Die(); return HitResult.Critical; }
        if (CurrentState != State.Dodging && CurrentState != State.Attacking) { StopAllCoroutines(); StartCoroutine(HitReactionRoutine()); }
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
            AoEZone z = aoe.GetComponent<AoEZone>();
            if (z != null) z.Setup(gameObject, DamageAmount * 1.5f);
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