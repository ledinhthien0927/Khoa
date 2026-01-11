using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 1. Kế thừa IDamageable để có thể bị Player đánh
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class SimpleEnemyAI : MonoBehaviour, IDamageable
{
    // ==========================================
    // SETTINGS & STATS
    // ==========================================
    [Header("Stats")]
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float RunSpeed = 3.5f; 
    public float WalkSpeed = 1.5f;
    
    [Header("Combat Ranges")]
    public float ChaseRange = 6.0f;    
    public float AttackRange = 1.5f;   
    public float DamageAmount = 15f; // Sát thương gây ra cho Player

    [Header("Combo Timing")]
    public float Attack1Duration = 0.8f; 
    public float Attack1HitTime = 0.3f; // Thời điểm gây dmg trong animation 1
    public float Attack2Duration = 1.0f;
    public float Attack2HitTime = 0.4f; // Thời điểm gây dmg trong animation 2

    // ==========================================
    // SYSTEM VARIABLES
    // ==========================================
    [Header("Debug Info")]
    public bool HasToken = false; 
    public State CurrentState;    

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player; // Vẫn giữ Transform để di chuyển
    private IDamageable _playerCombat; // Interface để GÂY DAMGE cho player
    
    private float _timer;
    private float _strafeDirection = 1f; 
    private float _strafeChangeTimer;
    
    // Token Management (Static)
    private static int _currentAttackers = 0;
    private const int MAX_ATTACKERS = 2;

    public enum State
    {
        Chasing,    
        Strafing,   
        Approaching,
        Attacking,  
        Retreating,
        Hit,    // Bị đánh
        Dead    // Chết
    }

    void Start()
    {
        // Setup Components
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _agent.updateRotation = false; 

        CurrentHealth = MaxHealth;

        // Tìm Player và Cache Interface
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) 
        {
            _player = p.transform;
            // Lấy Interface IDamageable của Player để sau này đánh
            _playerCombat = p.GetComponent<IDamageable>();
        }

        CurrentState = State.Chasing;
        _timer = Random.Range(0.5f, 1.5f);
    }

    void Update()
    {
        if (_player == null || CurrentState == State.Dead) return;

        // Cập nhật Animation di chuyển
        HandleAnimation(); 

        switch (CurrentState)
        {
            case State.Chasing:
                LogicChasing();
                break;
            case State.Strafing:
                LogicStrafing();
                break;
            case State.Approaching:
                LogicApproaching();
                break;
            case State.Attacking:
            case State.Hit:
                // Đang đánh hoặc bị đánh thì không di chuyển logic ở đây
                break; 
            case State.Retreating:
                LogicRetreating();
                break;
        }
    }

    // ==========================================================
    // PHẦN 1: GIAO TIẾP VỚI PLAYER (INTERFACE IMPLEMENTATION)
    // ==========================================================

    // Hàm này được gọi khi Player đánh trúng con quái này
    public HitResult TakeDamage(DamageInfo info)
    {
        if (CurrentState == State.Dead) return HitResult.Ignored;

        // 1. Trừ máu
        CurrentHealth -= info.amount;

        // 2. Kiểm tra chết
        if (CurrentHealth <= 0)
        {
            Die();
            return HitResult.Critical; // Trả về cho Player biết là đã kết liễu
        }

        // 3. Phản ứng trúng đòn (Hit Reaction)
        // Ngắt mọi hành động đang làm để giật mình
        StopAllCoroutines(); 
        StartCoroutine(HitReactionRoutine());
        
        return HitResult.Hit; // Trả về kết quả trúng đòn
    }

    void Die()
    {
        CurrentState = State.Dead;
        ReturnToken(); // Trả token ngay lập tức
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        
        _animator.SetTrigger("Die"); // Cần có trigger Die trong Animator
        GetComponent<Collider>().enabled = false; // Tắt va chạm để không cản đường
        this.enabled = false; // Tắt script
    }

    IEnumerator HitReactionRoutine()
    {
        CurrentState = State.Hit;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        _animator.SetTrigger("Hit"); // Cần trigger Hit trong Animator
        
        // Đứng đơ ra 0.5s
        yield return new WaitForSeconds(0.5f);

        // Hồi phục lại trạng thái
        _agent.isStopped = false;
        CurrentState = State.Strafing; // Quay về đi vờn
    }

    // Hàm này dùng để GỬI SÁT THƯƠNG sang Player
    void DealDamageToPlayer()
    {
        // Kiểm tra xem Player có còn trong tầm đánh không
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= AttackRange + 0.5f && _playerCombat != null)
        {
            // Tạo gói tin sát thương
            DamageInfo info = new DamageInfo
            {
                amount = DamageAmount,
                attacker = this.gameObject,
                hitPoint = _player.position, // Tạm lấy vị trí player
                type = DamageType.Physical,
                knockbackForce = 5f
            };

            // Gửi sang Player
            HitResult result = _playerCombat.TakeDamage(info);
            
            // (Tùy chọn) Kiểm tra kết quả trả về
            // if (result == HitResult.Blocked) { ... bị bật lại ... }
        }
    }

    // ==========================================================
    // PHẦN 2: LOGIC DI CHUYỂN & TẤN CÔNG (ĐÃ CẬP NHẬT)
    // ==========================================================

    void HandleAnimation()
    {
        // Lấy vận tốc tương đối
        Vector3 localVel = transform.InverseTransformDirection(_agent.velocity);

        // DỰA THEO HÌNH ẢNH BLEND TREE BẠN GỬI:
        // Pos X = Tới/Lùi (Sprint/Run_B) -> Map với localVel.z
        _animator.SetFloat("InputX", localVel.z, 0.1f, Time.deltaTime);

        // Pos Y = Trái/Phải (Run_L/Run_R) -> Map với localVel.x
        // Trong hình: Run_L (Trái) là Pos Y = 3. Run_R (Phải) là Pos Y = -3.
        // Trong Unity: localVel.x > 0 là Phải, < 0 là Trái.
        // => Phải đảo dấu: localVel.x (Phải +) nhân -1 thành (-), khớp với Run_R (-3).
        _animator.SetFloat("InputY", -localVel.x, 0.1f, Time.deltaTime);
    }

    void LogicApproaching()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        // Trừ hao 0.3m để ép quái chạy sát vào
        if (dist <= AttackRange - 0.3f)
        {
            bool useCombo = Random.Range(0, 100) < 40; 
            StartCoroutine(AttackRoutine(useCombo));
            return;
        }
        _agent.speed = RunSpeed;
        _agent.SetDestination(_player.position);
        FaceTarget();
    }

    IEnumerator AttackRoutine(bool isCombo)
    {
        CurrentState = State.Attacking;
        _agent.velocity = Vector3.zero;
        _agent.isStopped = true;

        // --- ĐÒN 1 ---
        FaceTarget(); 
        _animator.SetTrigger("Attack1"); 
        
        // Đợi đến lúc kiếm chạm người (HitTime) thì mới gây dmg
        yield return new WaitForSeconds(Attack1HitTime);
        DealDamageToPlayer(); // <--- GỌI GIAO TIẾP TẠI ĐÂY

        // Đợi nốt phần còn lại của animation
        yield return new WaitForSeconds(Attack1Duration - Attack1HitTime);

        // --- ĐÒN 2 (Nếu có) ---
        float dist = Vector3.Distance(transform.position, _player.position);
        if (isCombo && dist <= AttackRange + 0.5f)
        {
            FaceTarget();
            _animator.SetTrigger("Attack2");
            
            yield return new WaitForSeconds(Attack2HitTime);
            DealDamageToPlayer(); // <--- GÂY DMG LẦN 2

            yield return new WaitForSeconds(Attack2Duration - Attack2HitTime);
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        ReturnToken(); 
        _agent.isStopped = false; 
        CurrentState = State.Retreating;
        _timer = 1.5f; 
    }

    // --- CÁC HÀM CŨ GIỮ NGUYÊN ---
    void LogicChasing() {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= ChaseRange) { CurrentState = State.Strafing; _timer = 2.0f; return; }
        _agent.speed = RunSpeed; _agent.SetDestination(_player.position);
        if (_agent.velocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(_agent.velocity.normalized);
    }

    void LogicStrafing() {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > ChaseRange + 2.0f) { CurrentState = State.Chasing; return; }
        FaceTarget(); _agent.speed = WalkSpeed;
        Vector3 dirToPlayer = (_player.position - transform.position).normalized;
        Vector3 dirSide = Vector3.Cross(Vector3.up, dirToPlayer);
        _strafeChangeTimer -= Time.deltaTime;
        if (_strafeChangeTimer <= 0) { _strafeDirection = Random.Range(0, 2) == 0 ? -1f : 1f; _strafeChangeTimer = Random.Range(2f, 4f); }
        _agent.SetDestination(transform.position + dirSide * _strafeDirection * 2f);
        _timer -= Time.deltaTime;
        if (_timer <= 0 && !HasToken) TryGetToken();
    }

    void LogicRetreating() {
        FaceTarget(); _agent.speed = WalkSpeed;
        Vector3 dirBack = (transform.position - _player.position).normalized;
        _agent.SetDestination(transform.position + dirBack * 4.0f);
        _timer -= Time.deltaTime;
        if (_timer <= 0) { CurrentState = State.Strafing; _timer = 2.0f; }
    }

    void TryGetToken() { if (_currentAttackers < MAX_ATTACKERS) { _currentAttackers++; HasToken = true; CurrentState = State.Approaching; } else { _timer = 1.0f; } }
    void ReturnToken() { if (HasToken) { _currentAttackers--; HasToken = false; } }
    void FaceTarget() { Vector3 d = (_player.position - transform.position).normalized; d.y = 0; if (d != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 10f); }
    void OnDisable() { ReturnToken(); }
    void OnDrawGizmosSelected() { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, AttackRange); }
}