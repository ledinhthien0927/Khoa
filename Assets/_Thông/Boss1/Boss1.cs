using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class Boss1 : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float RunSpeed = 3.5f; 
    public float WalkSpeed = 1.5f;

    [Header("UI & VFX Settings")]
    public Slider HealthBarSlider;
    public GameObject HealthBarObj;
    public TrailRenderer WeaponTrail; 
    public ParticleSystem MoveDustVFX;
    public GameObject DeathDustVFX; 
    public float FadeOutDuration = 3.0f;

    [Header("Jump Attack Camera Shake")]
    public float JumpShakeDuration = 0.3f;
    public float JumpShakeMagnitude = 0.8f;

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

    public enum State { Spawning, Idle, Chasing, Strafing, Approaching, Attacking, Retreating, Dodging, Hit, Dead }

    [Header("Debug Info")]
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
    private List<GameObject> _activeMinions = new List<GameObject>();

    public void EnableTrail() { if (WeaponTrail != null) WeaponTrail.emitting = true; }
    public void DisableTrail() { if (WeaponTrail != null) WeaponTrail.emitting = false; }
    public void TriggerJumpShake() { if (BossCameraShake.Instance != null) BossCameraShake.Instance.Shake(JumpShakeDuration, JumpShakeMagnitude); }

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

        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(false);
        if (WeaponTrail != null) WeaponTrail.emitting = false;

        CurrentState = State.Idle;
    }

    void Update()
    {
        if (CurrentHealth <= 0 && CurrentState != State.Dead) { Die(); return; }

        if (!IsEnraged && CurrentHealth <= MaxHealth * 0.5f) { ActivatePhase2(); }

        UpdateHealthUI();
        if (HealthBarObj != null && _mainCamera != null)
            HealthBarObj.transform.rotation = Quaternion.LookRotation(HealthBarObj.transform.position - _mainCamera.transform.position);

        if (_player == null || CurrentState == State.Dead || CurrentState == State.Spawning) return;

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
            case State.Chasing: LogicChasing(); break;
            case State.Strafing: LogicStrafing(); break;
            case State.Approaching: LogicApproaching(); break;
            case State.Attacking:
            case State.Dodging:
            case State.Hit: FaceTarget(); break;
            case State.Retreating: LogicRetreating(); break;
        }
    }

    void HandleDustVFX()
    {
        if (MoveDustVFX == null) return;
        
        // Xác định xem có đang di chuyển không
        bool isMoving = _agent != null && _agent.velocity.sqrMagnitude > 0.1f;
        
        // Điều kiện được phép phát bụi (Lưu ý: Nếu copy cho Boss 2 thì thay State.Spawning thành State.PhaseChange)
        bool canPlay = isMoving && CurrentState != State.Dead && CurrentState != State.Spawning && CurrentState != State.Idle;
        
        if (canPlay) 
        { 
            // 1. Bật hiệu ứng
            if (!MoveDustVFX.isPlaying) MoveDustVFX.Play(); 
            
            // 2. Tính toán hướng di chuyển thực tế (bất kể Boss đang quay mặt đi đâu)
            Vector3 moveDir = _agent.velocity.normalized;
            moveDir.y = 0; // Bỏ qua trục Y để bụi không bị chĩa xuống đất hay chĩa lên trời
            
            if (moveDir != Vector3.zero)
            {
                // 3. Xoay cục Particle System chĩa ngược lại (-moveDir) so với hướng đang đi
                MoveDustVFX.transform.rotation = Quaternion.LookRotation(-moveDir);
            }
        } 
        else 
        { 
            if (MoveDustVFX.isPlaying) MoveDustVFX.Stop(); 
        }
    }

void ActivatePhase2()
    {
        IsEnraged = true;
        RunSpeed *= 1.5f; DashInSpeed *= 1.3f;
        SkillCooldown /= 2.0f; HellfireCooldown /= 2.0f; SummonCooldown /= 2.0f;
        _skillTimer = 0; _hellfireTimer = 0; _summonTimer = 0;
        
        // Chỉ làm to Boss lên 1.3 lần
        transform.localScale = transform.localScale * 1.3f;
        
        // (Đã xóa đoạn code ép đổi màu đỏ ở đây)
        
        if (SummonVFX != null) Instantiate(SummonVFX, transform.position, Quaternion.identity);
    }   

    void LogicIdle() { if (CheckForPlayer()) return; }
    
    void LogicChasing() { 
        float dist = Vector3.Distance(transform.position, _player.position); 
        if (dist <= ChaseRange) { CurrentState = State.Strafing; _timer = 2f; return; } 
        _agent.speed = RunSpeed; _agent.SetDestination(_player.position); 
        if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized); 
    }
    
    void LogicStrafing() { 
        float dist = Vector3.Distance(transform.position, _player.position); 
        if (dist > ChaseRange + 3f) { CurrentState = State.Chasing; return; } 
        FaceTarget(); _agent.speed = WalkSpeed; 
        Vector3 side = Vector3.Cross(Vector3.up, (_player.position - transform.position).normalized); 
        _strafeChangeTimer -= Time.deltaTime; 
        if (_strafeChangeTimer <= 0) { _strafeDirection *= -1; _strafeChangeTimer = Random.Range(2f, 4f); } 
        _agent.SetDestination(transform.position + side * _strafeDirection * 2f); 
        _timer -= Time.deltaTime; 
        if (_timer <= 0) CurrentState = State.Approaching; 
    }

    void LogicApproaching()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (EnableJumpAttack && _jumpTimer <= 0 && dist >= ChaseRange - 2f) { StartCoroutine(JumpAttackRoutine()); return; }
        if (CanSummon && _summonTimer <= 0 && _activeMinions.Count < MaxMinions) { StartCoroutine(SummonRoutine()); return; }
        if (EnableHellfire && _hellfireTimer <= 0 && dist <= 10.0f) { StartCoroutine(HellfireRoutine()); return; }
        if (EnableRapidSkill && _skillTimer <= 0 && dist <= AttackRange + 3.0f) { StartCoroutine(RapidSkillRoutine()); return; }
        
        if (dist <= AttackRange - 0.3f) {
            bool useCombo = Random.Range(0, 100) < 40; 
            StartCoroutine(AttackRoutine(useCombo));
            return;
        }

        _agent.speed = RunSpeed; _agent.SetDestination(_player.position); FaceTarget();
    }
    
    void LogicRetreating() { FaceTarget(); _agent.speed = WalkSpeed; Vector3 back = (transform.position - _player.position).normalized; _agent.SetDestination(transform.position + back * 4.0f); _timer -= Time.deltaTime; if (_timer <= 0) { CurrentState = State.Strafing; _timer = 2.0f; } }

    IEnumerator AttackRoutine(bool isCombo)
    {
        CurrentState = State.Attacking; _agent.velocity = Vector3.zero; _agent.isStopped = true; FaceTarget(); 
        _animator.SetTrigger("Attack1"); yield return new WaitForSeconds(Attack1HitTime);
        DealDamageToPlayer(DamageAmount, 0f); yield return new WaitForSeconds(Attack1Duration - Attack1HitTime);

        if (isCombo && Vector3.Distance(transform.position, _player.position) <= AttackRange + 0.8f)
        {
            FaceTarget(); _animator.SetTrigger("Attack2"); yield return new WaitForSeconds(Attack2HitTime);
            DealDamageToPlayer(DamageAmount, 0f); yield return new WaitForSeconds(Attack2Duration - Attack2HitTime);
        }
        else yield return new WaitForSeconds(0.2f);

        if (Random.Range(0, 100) < DodgeChance) StartCoroutine(JumpBackRoutine());
        else { _agent.isStopped = false; CurrentState = State.Retreating; _timer = 1.5f; }
    }

    IEnumerator JumpAttackRoutine()
    {
        CurrentState = State.Attacking; _jumpTimer = JumpCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; FaceTarget();
        _animator.SetTrigger("SkillCharge"); if (RapidSkillVFX != null) RapidSkillVFX.SetActive(true); 
        yield return new WaitForSeconds(JumpChargeDuration); 
        _animator.SetTrigger("JumpAttack"); yield return new WaitForSeconds(JumpTakeOffTime);

        Vector3 startPos = transform.position; Vector3 targetPos = _player.position;
        NavMeshHit hit; if (NavMesh.SamplePosition(targetPos, out hit, 3.0f, NavMesh.AllAreas)) targetPos = hit.position;
        _agent.enabled = false; 

        float flyDuration = JumpLandTime - JumpTakeOffTime; float elapsedTime = 0f;
        if (WeaponTrail != null) WeaponTrail.emitting = true;

        while (elapsedTime < flyDuration)
        {
            elapsedTime += Time.deltaTime; float t = elapsedTime / flyDuration; 
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += JumpHeight * 4.0f * t * (1.0f - t); 
            transform.position = currentPos; yield return null;
        }

        transform.position = targetPos; _agent.enabled = true; 
        if (WeaponTrail != null) WeaponTrail.emitting = false;

        _agent.isStopped = true; _agent.velocity = Vector3.zero;
        if (JumpAttackVFX != null) { GameObject fx = Instantiate(JumpAttackVFX, transform.position, Quaternion.identity); Destroy(fx, 3.0f); }

        Collider[] hits = Physics.OverlapSphere(transform.position, JumpRadius);
        foreach (var h in hits) { if (h.CompareTag("Player")) DealDamageToPlayer(JumpDamage, 0f); }
        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(false); 

        float remainingTime = JumpAnimTotalDuration - JumpLandTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);
        _agent.isStopped = false; CurrentState = State.Strafing; _timer = 2f;
    }

    IEnumerator RapidSkillRoutine()
    {
        CurrentState = State.Attacking; _skillTimer = SkillCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; FaceTarget(); 
        _animator.SetTrigger("SkillCharge"); if (RapidSkillVFX != null) RapidSkillVFX.SetActive(true);
        yield return new WaitForSeconds(ChargeDuration);
        
        _agent.isStopped = false; _agent.speed = DashInSpeed; _agent.acceleration = 100f; _agent.SetDestination(_player.position); _animator.SetTrigger("SkillDash"); 
        float t = 0; while (_agent.remainingDistance > AttackRange - 0.2f && t < 1.0f) { t += Time.deltaTime; yield return null; }
        _agent.velocity = Vector3.zero; _agent.isStopped = true; 

        float finalDist = Vector3.Distance(transform.position, _player.position);
        if (finalDist <= AttackRange + 1.0f)
        {
            FaceTarget(); _animator.SetFloat("AttackSpeed", SkillAnimSpeed);
            for (int i = 0; i < RapidHitCount; i++)
            {
                if (Vector3.Distance(transform.position, _player.position) > AttackRange + 2.0f) break;
                FaceTarget(); _animator.Play("RapidSlash", 0, 0f); DealDamageToPlayer(DamageAmount * 0.5f, 0f); 
                yield return new WaitForSeconds(RapidHitInterval);
            }
            _animator.SetFloat("AttackSpeed", 1.0f);
        }
        else yield return new WaitForSeconds(0.5f); 

        if (RapidSkillVFX != null) RapidSkillVFX.SetActive(false);
        yield return StartCoroutine(JumpBackRoutine());
    }

    IEnumerator HellfireRoutine() { CurrentState = State.Attacking; _hellfireTimer = HellfireCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; FaceTarget(); _animator.SetTrigger("CastSpell"); yield return new WaitForSeconds(HellfireCastTime); float duration = HellfireDuration; List<Vector3> allFirePositions = new List<Vector3>(); while (duration > 0) { if (CurrentState != State.Attacking) yield break; if (_player != null) { Vector3 targetPos = _player.position; targetPos.y = transform.position.y + 0.05f; bool playerPosSafe = true; foreach (Vector3 exist in allFirePositions) { if (Vector3.Distance(targetPos, exist) < MinSpawnDistance) { playerPosSafe = false; break; } } if (playerPosSafe) { SpawnAoE(targetPos); allFirePositions.Add(targetPos); } } int randomCount = Random.Range(2, 4); for (int i = 0; i < randomCount; i++) { Vector3 bestPos = Vector3.zero; bool foundPos = false; for (int attempt = 0; attempt < 15; attempt++) { Vector3 offset = Random.insideUnitSphere * 8.0f; if (offset.magnitude < 3.0f) offset = offset.normalized * 3.0f; Vector3 candidate = transform.position + offset; candidate.y = transform.position.y + 0.05f; bool tooClose = false; foreach (Vector3 exist in allFirePositions) { if (Vector3.Distance(candidate, exist) < MinSpawnDistance) { tooClose = true; break; } } if (!tooClose) { bestPos = candidate; foundPos = true; break; } } if (foundPos) { SpawnAoE(bestPos); allFirePositions.Add(bestPos); } } yield return new WaitForSeconds(WaveInterval); duration -= WaveInterval; FaceTarget(); } _agent.isStopped = false; CurrentState = State.Strafing; _timer = 2f; }
    IEnumerator SummonRoutine() { CurrentState = State.Attacking; _summonTimer = SummonCooldown; _agent.isStopped = true; _agent.velocity = Vector3.zero; FaceTarget(); _animator.SetTrigger("CastSpell"); yield return new WaitForSeconds(SummonCastTime); int spawnCount = MaxMinions - _activeMinions.Count; for (int i = 0; i < spawnCount; i++) { Vector3 rnd = transform.position + Random.insideUnitSphere * SummonRadius; NavMeshHit hit; if (NavMesh.SamplePosition(rnd, out hit, 2.0f, NavMesh.AllAreas)) { if (SummonVFX != null) Instantiate(SummonVFX, hit.position, Quaternion.identity); if (MinionPrefab != null) { _activeMinions.Add(Instantiate(MinionPrefab, hit.position, Quaternion.LookRotation(transform.forward))); } } yield return new WaitForSeconds(0.3f); } yield return new WaitForSeconds(1.0f); _agent.isStopped = false; CurrentState = State.Strafing; _timer = 2f; }
    IEnumerator JumpBackRoutine() { CurrentState = State.Dodging; _agent.isStopped = false; _animator.SetTrigger("Dodge"); _agent.speed = JumpBackSpeed; _agent.acceleration = 100f; _agent.SetDestination(transform.position - transform.forward * 3.5f); yield return new WaitForSeconds(JumpBackDuration); _agent.speed = WalkSpeed; _agent.acceleration = 8f; _agent.velocity = Vector3.zero; CurrentState = State.Strafing; _timer = 2.0f; }
    IEnumerator HitReactionRoutine() { if (CurrentState == State.Dead) yield break; CurrentState = State.Hit; _agent.isStopped = true; _agent.velocity = Vector3.zero; _animator.SetTrigger("Hit"); yield return new WaitForSeconds(0.5f); _agent.isStopped = false; CurrentState = State.Strafing; _timer = 1f; }
    
    public HitResult TakeDamage(DamageInfo info) { if (CurrentState == State.Dead) return HitResult.Ignored; CurrentHealth -= info.amount; UpdateHealthUI(); if (CurrentHealth <= 0) { Die(); return HitResult.Critical; } if (CurrentState == State.Attacking || CurrentState == State.Spawning) return HitResult.Hit; bool shouldStagger = IsEnraged ? (Random.value < 0.3f) : true; if (shouldStagger && CurrentState != State.Dodging) { StopAllCoroutines(); StartCoroutine(HitReactionRoutine()); } return HitResult.Hit; }
    void DealDamageToPlayer(float dmg, float force) { if (_playerCombat != null && Vector3.Distance(transform.position, _player.position) <= AttackRange + 1.0f) { _playerCombat.TakeDamage(new DamageInfo { amount = dmg, attacker = gameObject, hitPoint = _player.position + Vector3.up, type = DamageType.Physical, knockbackForce = force, duration = 0f }); } }
    void SpawnAoE(Vector3 pos) { if (AoEPrefab != null) { GameObject aoe = Instantiate(AoEPrefab, pos, Quaternion.identity); Destroy(aoe, 4.0f); var zone = aoe.GetComponent<MonoBehaviour>(); if (zone != null) { aoe.SendMessage("Setup", new object[] { gameObject, DamageAmount * 1.5f }, SendMessageOptions.DontRequireReceiver); } } }
    
    void Die() 
    { 
        CurrentState = State.Dead; 
        if (_agent != null) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
        StopAllCoroutines(); 
        if (_animator != null) _animator.SetTrigger("Die"); 
        if (TryGetComponent(out Collider col)) col.enabled = false; 
        if (HealthBarObj != null) HealthBarObj.SetActive(false);

        // Luôn luôn gọi Banner vì đây là Boss
        if (BossDefeatBanner.Instance != null) BossDefeatBanner.Instance.ShowBanner(); 

        if (DeathDustVFX != null) { GameObject dust = Instantiate(DeathDustVFX, transform.position, Quaternion.identity); Destroy(dust, 5.0f); }
        StartCoroutine(FadeOutCorpseRoutine());
        this.enabled = false; 
    }

    IEnumerator FadeOutCorpseRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        System.Collections.Generic.List<Renderer> validRenderers = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in allRenderers) { if (r is ParticleSystemRenderer || r is TrailRenderer) continue; validRenderers.Add(r); }

        foreach (Renderer r in validRenderers)
        {
            if (r == null || r.materials == null) continue;
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_Surface"))
                {
                    m.SetFloat("_Surface", 1.0f); m.SetOverrideTag("RenderType", "Transparent");
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0); m.DisableKeyword("_ALPHAPREMULTIPLY_ON"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
            }
        }

        float elapsed = 0f;
        while (elapsed < FadeOutDuration)
        {
            elapsed += Time.deltaTime; float alpha = Mathf.Lerp(1f, 0f, elapsed / FadeOutDuration);
            foreach (Renderer r in validRenderers)
            {
                if (r == null || r.materials == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m.HasColor("_BaseColor")) { Color c = m.GetColor("_BaseColor"); c.a = alpha; m.SetColor("_BaseColor", c); }
                    else if (m.HasColor("_Color")) { Color c = m.GetColor("_Color"); c.a = alpha; m.SetColor("_Color", c); }
                }
            }
            yield return null; 
        }
        Destroy(gameObject);
    }    
    
    void UpdateHealthUI() { if (HealthBarSlider != null) HealthBarSlider.value = CurrentHealth; }
    bool CheckForPlayer() { if (_player == null) return false; float d = Vector3.Distance(transform.position, _player.position); if (d <= ChaseRange + 4f) { CurrentState = State.Chasing; return true; } return false; }
    void HandleAnimation() { Vector3 v = transform.InverseTransformDirection(_agent.velocity); _animator.SetFloat("InputX", v.z, 0.1f, Time.deltaTime); _animator.SetFloat("InputY", -v.x, 0.1f, Time.deltaTime); } 
    void FaceTarget() { if (_player == null) return; Vector3 d = (_player.position - transform.position).normalized; d.y = 0; if (d != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 15f); }
    void OnDrawGizmosSelected() { Vector3 c = transform.position + Vector3.up; Gizmos.color = Color.red; Gizmos.DrawWireSphere(c, AttackRange); }
}