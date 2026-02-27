using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class MinionAI : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float MaxHealth = 50f;
    public float CurrentHealth;
    public float RunSpeed = 3.5f; 
    public float WalkSpeed = 1.5f;

    [Header("2D Health Bar Settings")]
    [Tooltip("Kéo Prefab MinionHealth2D vào đây")]
    public GameObject HealthBarPrefab; 
    public float HealthBarYOffset = 2.0f; 
    public float ShowDuration = 4.0f; 
    public float YellowBarSpeed = 2.0f; 

    [Header("VFX Settings")]
    public TrailRenderer WeaponTrail; 
    public ParticleSystem MoveDustVFX;
    public GameObject DeathDustVFX; 
    public float FadeOutDuration = 3.0f;

    [Header("Spawn Settings")]
    public bool PlaySpawnAnim = true;
    public float SpawnDuration = 2.0f;

    [Header("Patrol Settings")]
    public Transform[] PatrolPoints;
    public float PatrolSpeed = 2.0f;
    public float IdleTime = 3.0f;
    public float DetectionRange = 10.0f;

    [Header("Combat Ranges")]
    public float ChaseRange = 6.0f;    
    public float AttackRange = 1.5f;   
    public float DamageAmount = 10f;   

    [Header("Attack Timing")]
    public float Attack1Duration = 1.0f; 
    public float Attack1HitTime = 0.4f;  

    public enum State { Spawning, Idle, Patrol, Chasing, Approaching, Attacking, Hit, Dead }

    [Header("Debug Info")]
    public State CurrentState;
    public bool HasToken = false;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private IDamageable _playerCombat; 
    private Camera _mainCamera;
    
    private float _timer;
    private int _currentPatrolIndex = 0;

    private static int _currentAttackers = 0;
    private const int MAX_ATTACKERS = 2;

    // --- UI 2D VARIABLES ---
    private GameObject _uiInstance;
    private RectTransform _uiRect;
    private Slider _backSlider;  // Thanh Vàng
    private Slider _frontSlider; // Thanh Đỏ
    private TextMeshProUGUI _damageText;
    
    private float _showTimer = 0f;
    private float _damageResetTimer = 0f;
    private float _accumulatedDamage = 0f;

    public void EnableTrail() { if (WeaponTrail != null) WeaponTrail.emitting = true; }
    public void DisableTrail() { if (WeaponTrail != null) WeaponTrail.emitting = false; }

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _agent.updateRotation = false; 
        CurrentHealth = MaxHealth;
        _mainCamera = Camera.main;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { _player = p.transform; _playerCombat = p.GetComponent<IDamageable>(); }

        if (WeaponTrail != null) WeaponTrail.emitting = false;

        Setup2DHealthBar();

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

    void Setup2DHealthBar()
    {
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas != null && HealthBarPrefab != null)
        {
            _uiInstance = Instantiate(HealthBarPrefab, mainCanvas.transform);
            _uiRect = _uiInstance.GetComponent<RectTransform>();

            Slider[] sliders = _uiInstance.GetComponentsInChildren<Slider>();
            if (sliders.Length >= 2)
            {
                _backSlider = sliders[0];  // Thanh đầu tiên là Vàng
                _frontSlider = sliders[1]; // Thanh thứ hai là Đỏ
                
                _backSlider.maxValue = MaxHealth; _backSlider.value = MaxHealth;
                _frontSlider.maxValue = MaxHealth; _frontSlider.value = MaxHealth;
            }

            _damageText = _uiInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (_damageText != null) _damageText.text = "";

            _uiInstance.SetActive(false); // Ẩn lúc đầu
        }
    }

    void Update()
    {
        if (CurrentHealth <= 0 && CurrentState != State.Dead) { Die(); return; }

        UpdateHealthUI();

        if (_player == null || CurrentState == State.Dead || CurrentState == State.Spawning) return;

        HandleAnimation();
        HandleDustVFX();

        switch (CurrentState)
        {
            case State.Idle: LogicIdle(); break;
            case State.Patrol: LogicPatrol(); break;
            case State.Chasing: LogicChasing(); break;
            case State.Approaching: LogicApproaching(); break;
            case State.Attacking:
            case State.Hit: FaceTarget(); break;
        }
    }

    void UpdateHealthUI() 
    { 
        if (_uiInstance == null) return;

        // Xử lý hiệu ứng thanh vàng tụt từ từ
        if (_backSlider != null && _backSlider.value > CurrentHealth)
        {
            _backSlider.value = Mathf.Lerp(_backSlider.value, CurrentHealth, Time.deltaTime * YellowBarSpeed);
        }

        // Đếm ngược để ẩn toàn bộ UI
        if (_showTimer > 0)
        {
            _showTimer -= Time.deltaTime;
            if (_showTimer <= 0)
            {
                _uiInstance.SetActive(false);
                _accumulatedDamage = 0f;
            }
        }

        // Đếm ngược để xóa số dame dính phải
        if (_damageResetTimer > 0)
        {
            _damageResetTimer -= Time.deltaTime;
            if (_damageResetTimer <= 0 && _damageText != null)
            {
                _damageText.text = "";
                _accumulatedDamage = 0f;
            }
        }

        // Xử lý bám theo quái và ẩn khi ra rìa màn hình
        if (_uiInstance.activeSelf)
        {
            Vector3 worldPos = transform.position + Vector3.up * HealthBarYOffset;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // Kiểm tra xem quái có bị lọt ra ngoài camera không (z < 0 là ở sau lưng Camera)
            bool isOffScreen = screenPos.z < 0 || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height;

            if (isOffScreen)
            {
                // Dùng localScale để giấu đi mà không làm hỏng tiến trình đếm ngược
                _uiInstance.transform.localScale = Vector3.zero;
            }
            else
            {
                _uiInstance.transform.localScale = Vector3.one;
                _uiRect.position = screenPos; // Cố định tại khung này
            }
        }
    }

    void HandleDustVFX()
    {
        if (MoveDustVFX == null) return;
        bool isMoving = _agent != null && _agent.velocity.sqrMagnitude > 0.1f;
        bool canPlay = isMoving && CurrentState != State.Dead && CurrentState != State.Spawning && CurrentState != State.Idle;
        if (canPlay) { if (!MoveDustVFX.isPlaying) MoveDustVFX.Play(); } else { if (MoveDustVFX.isPlaying) MoveDustVFX.Stop(); }
    }

    IEnumerator SpawnRoutine() { CurrentState = State.Spawning; _agent.isStopped = true; _agent.velocity = Vector3.zero; if (TryGetComponent(out Collider col)) col.enabled = false; yield return new WaitForSeconds(SpawnDuration); if (col != null) col.enabled = true; _agent.isStopped = false; CurrentState = State.Chasing; }
    void LogicIdle() { if (CheckForPlayer()) return; _timer -= Time.deltaTime; if (_timer <= 0) NextPatrolPoint(); }
    void LogicPatrol() { if (CheckForPlayer()) return; _agent.speed = PatrolSpeed; if (_agent.velocity.sqrMagnitude > 0.1f) { Vector3 dir = _agent.velocity.normalized; dir.y = 0; if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f); } if (!_agent.pathPending && _agent.remainingDistance < 0.5f) { CurrentState = State.Idle; _timer = IdleTime; } }
    void NextPatrolPoint() { if (PatrolPoints.Length == 0) return; _currentPatrolIndex = (_currentPatrolIndex + 1) % PatrolPoints.Length; CurrentState = State.Patrol; _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position); }
    void LogicChasing() { float dist = Vector3.Distance(transform.position, _player.position); if (dist > DetectionRange * 1.5f) { CurrentState = State.Patrol; if (PatrolPoints.Length > 0) _agent.SetDestination(PatrolPoints[_currentPatrolIndex].position); return; } if (dist <= ChaseRange) { CurrentState = State.Approaching; TryGetToken(); return; } _agent.speed = RunSpeed; _agent.SetDestination(_player.position); if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized); }

    void LogicApproaching()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= AttackRange - 0.3f && HasToken) { StartCoroutine(AttackRoutine()); return; }
        if (!HasToken && dist <= AttackRange + 1.0f) { _agent.isStopped = true; FaceTarget(); TryGetToken(); return; }
        _agent.isStopped = false; _agent.speed = RunSpeed; _agent.SetDestination(_player.position); FaceTarget();
    }

    IEnumerator AttackRoutine()
    {
        CurrentState = State.Attacking; _agent.velocity = Vector3.zero; _agent.isStopped = true; FaceTarget(); 
        _animator.SetTrigger("Attack1"); yield return new WaitForSeconds(Attack1HitTime);
        DealDamageToPlayer(DamageAmount, 0f); yield return new WaitForSeconds(Attack1Duration - Attack1HitTime);
        ReturnToken(); _agent.isStopped = false; CurrentState = State.Approaching; yield return new WaitForSeconds(0.5f); 
    }

    IEnumerator HitReactionRoutine() { if (CurrentState == State.Dead) yield break; CurrentState = State.Hit; _agent.isStopped = true; _agent.velocity = Vector3.zero; _animator.SetTrigger("Hit"); yield return new WaitForSeconds(0.5f); _agent.isStopped = false; CurrentState = State.Approaching; }
    
    public HitResult TakeDamage(DamageInfo info) 
    { 
        if (CurrentState == State.Dead) return HitResult.Ignored; 
        
        float previousHealth = CurrentHealth;
        CurrentHealth -= info.amount; 

        // Xử lý bật UI, cộng dồn dame
        _showTimer = ShowDuration;
        _damageResetTimer = 2.0f;
        _accumulatedDamage += info.amount;

        if (_uiInstance != null)
        {
            if (!_uiInstance.activeSelf)
            {
                _uiInstance.SetActive(true);
                // Giữ thanh vàng ở mức máu cũ khi vừa bị đánh để tạo cảm giác delay
                if (_backSlider != null) _backSlider.value = previousHealth; 
            }
            if (_frontSlider != null) _frontSlider.value = CurrentHealth;
            if (_damageText != null) _damageText.text = Mathf.RoundToInt(_accumulatedDamage).ToString();
        }

        if (CurrentHealth <= 0) { Die(); return HitResult.Critical; } 
        if (CurrentState == State.Attacking || CurrentState == State.Spawning) return HitResult.Hit; 
        StopAllCoroutines(); 
        StartCoroutine(HitReactionRoutine()); 
        return HitResult.Hit; 
    }

    void DealDamageToPlayer(float dmg, float force) { if (_playerCombat != null && Vector3.Distance(transform.position, _player.position) <= AttackRange + 1.0f) { _playerCombat.TakeDamage(new DamageInfo { amount = dmg, attacker = gameObject, hitPoint = _player.position + Vector3.up, type = DamageType.Physical, knockbackForce = force, duration = 0f }); } }
    
   void Die() 
    { 
        CurrentState = State.Dead; 
        
        // Dừng di chuyển an toàn
        if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
        
        StopAllCoroutines(); 
        
        if (_animator != null) _animator.SetTrigger("Die"); 
        if (TryGetComponent(out Collider col)) col.enabled = false; 

        // Gọi Coroutine làm mờ xác (trong này đã có lệnh đếm ngược 1 giây mới tắt thanh máu)
        StartCoroutine(FadeOutCorpseRoutine());
    }
    IEnumerator FadeOutCorpseRoutine()
    {   
        // 1. Chờ 1 giây đầu tiên, sau đó hủy thanh máu 2D
        yield return new WaitForSeconds(1.0f);
        if (_uiInstance != null) Destroy(_uiInstance);

        // 2. Chờ thêm 1 giây nữa (tổng cộng là 2 giây để diễn xong animation ngã)
        yield return new WaitForSeconds(1.0f);
    
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
    
    bool CheckForPlayer() { float d = Vector3.Distance(transform.position, _player.position); if (d <= DetectionRange) { CurrentState = State.Chasing; return true; } return false; }
    void HandleAnimation() { Vector3 v = transform.InverseTransformDirection(_agent.velocity); _animator.SetFloat("InputX", v.z, 0.1f, Time.deltaTime); _animator.SetFloat("InputY", -v.x, 0.1f, Time.deltaTime); } 
    void FaceTarget() { if (_player == null) return; Vector3 d = (_player.position - transform.position).normalized; d.y = 0; if (d != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 15f); }
    void TryGetToken() { if (_currentAttackers < MAX_ATTACKERS) { _currentAttackers++; HasToken = true; } }
    void ReturnToken() { if (HasToken) { _currentAttackers--; HasToken = false; } }
    void OnDisable() { ReturnToken(); }
    void OnDrawGizmosSelected() { Vector3 c = transform.position + Vector3.up; Gizmos.color = Color.green; Gizmos.DrawWireSphere(c, DetectionRange); Gizmos.color = Color.red; Gizmos.DrawWireSphere(c, AttackRange); }
}