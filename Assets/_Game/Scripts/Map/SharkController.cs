using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class SharkController : MonoBehaviour
{
    [Header("Shark Stats")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 7.0f;
    public float burstSpeed = 15.0f;
    
    [Header("Combat Settings")]
    public float detectionRange = 30.0f;
    public float attackRange = 8.0f;     
    public float attackDamage = 20f;
    public float attackCooldown = 3.0f;
    public LayerMask targetLayer; 

    [Header("Lunge Attack Config")]
    public float lungeSpeed = 20.0f;     
    public float lungeDelay = 0.5f;      
    public float lungeDuration = 1.2f;   
    public float damageRadius = 4.0f; 

    [Header("Visuals")]
    public Animator animator;
    public TrailRenderer attackTrail;

    // --- Optimization Vars ---
    private float _pathUpdateTimer;
    private const float PATH_UPDATE_INTERVAL = 0.2f; 
    private Collider[] _hitBuffer = new Collider[10]; 

    // [TỐI ƯU]: Cache ID của Animator để không dùng string mỗi frame
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");

    // [TỐI ƯU]: Tính bình phương khoảng cách 1 lần để so sánh nhanh
    private float _attackRangeSqr;
    private float _detectionRangeSqr;

    // [LOGIC]: Điểm neo để tuần tra (tránh đi lạc quá xa điểm sinh ra)
    public Vector3 AnchorPoint { get; set; } 
    public Vector3 CurrentDestination { get; private set; }

    private NavMeshAgent _agent;
    private float _lastAttackTime;
    private SharkState _currentState;

    private bool _isBursting = false;
    private float _burstEndTime;
    private bool _isLunging = false;

    private Vector3 _circlingTarget;
    private float _circlingTimer;

    public enum SharkState { Patrol, Chase, Attack }

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = true; 
        _agent.acceleration = 60f;
        _currentState = SharkState.Patrol;
    }

    void Start()
    {
        _agent.avoidancePriority = Random.Range(30, 70);
        _lastAttackTime = -attackCooldown; 

        // [TỐI ƯU]: Tính sẵn bình phương range
        _attackRangeSqr = attackRange * attackRange;
        _detectionRangeSqr = detectionRange * detectionRange;
        
        // Mặc định Anchor là vị trí ban đầu nếu Manager quên set
        if (AnchorPoint == Vector3.zero) AnchorPoint = transform.position;
    }

    public void MoveTo(Vector3 targetPos)
    {
        if (_agent.isOnNavMesh)
        {
            CurrentDestination = targetPos;
            _agent.SetDestination(targetPos);
            _agent.isStopped = false;
        }
    }

    public void SetupBurstMode(Vector3 targetPosition)
    {
        _isBursting = true;
        _burstEndTime = Time.time + 5.0f;
        _agent.speed = burstSpeed;
        MoveTo(targetPosition);
    }

    public void ManualUpdate(PlayerController player, bool isPlayerSafe, IDamageable playerDamageable)
    {
        if (player == null) return;
        
        // [TỐI ƯU]: Dùng Hash ID thay vì String
        if (animator) animator.SetFloat(AnimSpeed, _agent.velocity.magnitude);

        if (_isLunging) return; 

        // [LOGIC MỚI]: Tính khoảng cách bình phương (Siêu nhẹ)
        Vector3 offset = player.transform.position - transform.position;
        float sqrDist = offset.sqrMagnitude;

        // [LOGIC MỚI]: Nếu đang Burst mà thấy địch trong tầm đánh -> Hủy Burst ngay để chiến đấu
        if (_isBursting)
        {
            if (sqrDist <= _attackRangeSqr && !isPlayerSafe)
            {
                _isBursting = false; // Ngắt Burst
            }
            else if (Time.time < _burstEndTime) 
            { 
                _agent.speed = burstSpeed; 
                return; 
            }
            else 
            { 
                _isBursting = false; 
                _agent.speed = patrolSpeed; 
            }
        }

        // State Machine Logic
        if (isPlayerSafe || playerDamageable == null)
        {
            SetState(SharkState.Patrol);
            PatrolLogic(); 
            return;
        }

        if (sqrDist <= _attackRangeSqr)
        {
            SetState(SharkState.Attack);
            // Chỉ tính căn bậc 2 khi cần tham số chính xác cho logic hit & run
            AttackLogic(player, Mathf.Sqrt(sqrDist));
        }
        else if (sqrDist <= _detectionRangeSqr)
        {
            SetState(SharkState.Chase);
            ChaseLogic(player);
        }
        else
        {
            SetState(SharkState.Patrol);
            PatrolLogic(); 
        }
    }

    void SetState(SharkState newState) { _currentState = newState; }

    void PatrolLogic()
    {
        _agent.speed = patrolSpeed;
        if (!_agent.hasPath || _agent.remainingDistance < 2.0f)
        {
            if (SharkManager.Instance != null)
            {
                // Truyền 'this' để Manager biết vị trí Anchor của con này
                Vector3 smartDest = SharkManager.Instance.GetSmartPatrolPoint(this);
                if (smartDest != Vector3.zero) MoveTo(smartDest);
            }
        }
    }

    void ChaseLogic(PlayerController player)
    {
        _agent.speed = chaseSpeed;
        if (Time.time > _pathUpdateTimer)
        {
            MoveTo(player.transform.position);
            _pathUpdateTimer = Time.time + PATH_UPDATE_INTERVAL;
        }
    }

    void AttackLogic(PlayerController player, float trueDist)
    {
        Vector3 dir = (player.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) 
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);

        if (Time.time - _lastAttackTime > attackCooldown)
        {
            StartCoroutine(PerformLungeAttack(player));
        }
        else
        {
            // Hit and Run logic
            if (trueDist < 5.0f)
            {
                _agent.speed = 4.0f; 
                if (Time.time > _circlingTimer)
                {
                    Vector3 randomDir = Random.onUnitSphere;
                    randomDir.y = 0; 
                    _circlingTarget = player.transform.position + randomDir.normalized * 7.0f;
                    _circlingTimer = Time.time + 1.5f;
                    MoveTo(_circlingTarget);
                }
            }
            else
            {
                _agent.speed = 2.0f; 
                if (Time.time > _pathUpdateTimer)
                {
                    MoveTo(player.transform.position);
                    _pathUpdateTimer = Time.time + PATH_UPDATE_INTERVAL;
                }
            }
        }
    }

    IEnumerator PerformLungeAttack(PlayerController player)
    {
        _isLunging = true;
        _lastAttackTime = Time.time;

        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        if (animator) animator.SetTrigger(AnimAttack); // Dùng Hash ID
        
        yield return new WaitForSeconds(lungeDelay);

        if (attackTrail != null) { attackTrail.Clear(); attackTrail.emitting = true; }

        _agent.isStopped = false;
        _agent.speed = lungeSpeed;
        _agent.acceleration = 200f; 
        
        // [FIX NAVMESH SLIDING]: Reset path để đảm bảo agent nhận hướng mới sạch sẽ
        _agent.ResetPath(); 

        float timer = 0f;
        bool hasDealtDamage = false;

        while (timer < lungeDuration)
        {
            if (player == null) break;
            
            // Cập nhật vị trí liên tục để đuổi theo (Homing)
            // Lưu ý: Với Lunge quá nhanh, đôi khi Move() tốt hơn SetDestination
            _agent.SetDestination(player.transform.position);

            if (!hasDealtDamage)
            {
                int hitCount = Physics.OverlapSphereNonAlloc(transform.position, damageRadius, _hitBuffer, targetLayer);
                for (int i = 0; i < hitCount; i++)
                {
                    IDamageable damageable = _hitBuffer[i].GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        DamageInfo info = new DamageInfo { amount = attackDamage, attacker = gameObject };
                        damageable.TakeDamage(info);
                        hasDealtDamage = true;
                        // Debug.Log("Shark bit " + _hitBuffer[i].name); // Comment lại để đỡ spam console
                        break; 
                    }
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (attackTrail != null) { attackTrail.emitting = false; }
        
        _agent.speed = chaseSpeed;
        _agent.acceleration = 60f; 
        _isLunging = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
        
        // Vẽ Anchor point để debug
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(AnchorPoint, 1.0f);
    }
}