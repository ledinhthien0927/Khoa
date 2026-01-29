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

    // [MỚI] Chỉ tấn công những gì thuộc Layer này
    public LayerMask targetLayer; 

    [Header("Lunge Attack Config")]
    public float lungeSpeed = 20.0f;     
    public float lungeDelay = 0.5f;      
    public float lungeDuration = 1.2f;   
    public float damageRadius = 4.0f; // Bán kính cắn

    [Header("Visuals")]
    public Animator animator;
    public TrailRenderer attackTrail; // Hiệu ứng vệt nước

    // Public Property
    public Vector3 CurrentDestination { get; private set; }

    // Private Vars
    private NavMeshAgent _agent;
    private float _lastAttackTime;
    private SharkState _currentState;

    private bool _isBursting = false;
    private float _burstEndTime;
    private bool _isLunging = false;

    // Biến hỗ trợ bơi vòng quanh
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
        // Random độ ưu tiên để tránh kẹt nhau
        _agent.avoidancePriority = Random.Range(30, 70);
        // Cho phép tấn công ngay lần đầu gặp mặt
        _lastAttackTime = -attackCooldown; 
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

    public void ManualUpdate(PlayerController player, bool isPlayerSafe)
    {
        if (player == null) return;
        if (animator) animator.SetFloat("Speed", _agent.velocity.magnitude);

        // 1. Burst Mode (5s đầu)
        if (_isBursting)
        {
            if (Time.time < _burstEndTime) { _agent.speed = burstSpeed; return; }
            else { _isBursting = false; _agent.speed = patrolSpeed; }
        }

        // 2. Đang lao tấn công thì không xử lý logic khác
        if (_isLunging) return; 

        // 3. Kiểm tra an toàn
        if (isPlayerSafe || player.GetComponent<IDamageable>() == null)
        {
            SetState(SharkState.Patrol);
            PatrolLogic(); 
            return;
        }

        // 4. Tính khoảng cách phẳng (bỏ qua độ sâu Y)
        float flatDistance = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(player.transform.position.x, 0, player.transform.position.z)
        );

        // 5. State Machine
        if (flatDistance <= attackRange)
        {
            SetState(SharkState.Attack);
            AttackLogic(player);
        }
        else if (flatDistance <= detectionRange)
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
                Vector3 smartDest = SharkManager.Instance.GetSmartPatrolPoint(this);
                if (smartDest != Vector3.zero) MoveTo(smartDest);
            }
        }
    }

    void ChaseLogic(PlayerController player)
    {
        _agent.speed = chaseSpeed;
        MoveTo(player.transform.position);
    }

    void AttackLogic(PlayerController player)
    {
        // Stalking: Luôn quay mặt về phía player
        Vector3 dir = (player.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) 
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);

        if (Time.time - _lastAttackTime > attackCooldown)
        {
            // TẤN CÔNG
            StartCoroutine(PerformLungeAttack(player));
        }
        else
        {
            // CHỜ HỒI CHIÊU: Hit and Run logic
            float dist = Vector3.Distance(transform.position, player.transform.position);

            if (dist < 5.0f) // Nếu quá gần -> Bơi tản ra
            {
                _agent.speed = 4.0f; 
                if (Time.time > _circlingTimer)
                {
                    Vector3 randomDir = Random.onUnitSphere;
                    randomDir.y = 0; 
                    _circlingTarget = player.transform.position + randomDir.normalized * 7.0f;
                    _circlingTimer = Time.time + 1.5f; 
                }
                MoveTo(_circlingTarget);
            }
            else // Nếu đã xa -> Quay lại dọa
            {
                _agent.speed = 2.0f; 
                MoveTo(player.transform.position);
            }
        }
    }

    // --- COROUTINE TẤN CÔNG (ĐÃ SỬA ĐỂ KHÔNG CẮN ĐỒNG LOẠI) ---
    IEnumerator PerformLungeAttack(PlayerController player)
    {
        _isLunging = true;
        _lastAttackTime = Time.time;

        // 1. Wind-up
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        if (animator) animator.SetTrigger("Attack");
        
        yield return new WaitForSeconds(lungeDelay);

        // 2. Lunge (Bật Trail)
        if (attackTrail != null) { attackTrail.Clear(); attackTrail.emitting = true; }

        _agent.isStopped = false;
        _agent.speed = lungeSpeed;
        _agent.acceleration = 200f; 

        float timer = 0f;
        bool hasDealtDamage = false;

        while (timer < lungeDuration)
        {
            if (player == null) break;
            
            MoveTo(player.transform.position);

            if (!hasDealtDamage)
            {
                // [FIX QUAN TRỌNG] Sử dụng OverlapSphere + LayerMask
                // Quét một vùng cầu bán kính damageRadius xung quanh cá mập
                // CHỈ LẤY những vật thuộc "targetLayer" (Layer Player)
                Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, targetLayer);

                foreach (var hit in hits)
                {
                    // Lấy IDamageable từ vật bị va chạm
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        // Nếu trúng Player -> Gây damage và dừng
                        DamageInfo info = new DamageInfo { amount = attackDamage, attacker = gameObject };
                        damageable.TakeDamage(info);
                        hasDealtDamage = true;
                        Debug.Log("Shark bit " + hit.name); // Sẽ chỉ hiện tên Player
                        break; 
                    }
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. Recovery (Tắt Trail)
        if (attackTrail != null) { attackTrail.emitting = false; }
        
        _agent.speed = chaseSpeed;
        _agent.acceleration = 60f; 
        _isLunging = false;
    }

    // Vẽ Gizmos để debug tầm đánh trong Scene
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}