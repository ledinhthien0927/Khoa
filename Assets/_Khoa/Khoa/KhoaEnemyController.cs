using UnityEngine;
using UnityEngine.AI;

// [QUAN TRỌNG] Thêm 'abstract'
public abstract class MonsterController : MonoBehaviour
{
    public MonsterData data;

    [Header("Runtime Stats")]
    public float currentHealth; 
    public bool isAlerted = false;
    public bool isTracking = false; 

    [Header("Base Logic")]
    public float currentDetectionTime = 0f;
    public Vector3? lastKnownPosition = null; 
    public float searchWaitTime = 0f;         
    public Vector3 startPosition;            
    public float currentWanderWaitTime = 0f; 

    // [QUAN TRỌNG] Đổi thành protected để lớp con sử dụng được
    protected NavMeshAgent agent;
    protected Animator anim;
    protected float lastAttackTime = -999f; // Để đánh được ngay đòn đầu

    // Đổi Start thành virtual để lớp con có thể viết thêm nếu cần
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (data != null)
        {
            currentHealth = data.maxHealth; 
            if (agent != null)
            {
                agent.speed = data.speed;
                agent.stoppingDistance = data.attackRange;
            }
        }
        
        startPosition = transform.position;
        if (MonsterManager.Instance != null) MonsterManager.Instance.RegisterMonster(this);
    }

    protected virtual void Update()
    {
        if (agent != null && anim != null) anim.SetFloat("speed", agent.velocity.magnitude);
    }

    // --- [TRÁI TIM CỦA KẾ THỪA] ---
    // Đây là hàm trừu tượng. Mọi con quái con BẮT BUỘC phải tự viết nội dung cho hàm này.
    public abstract void OnCombatBehavior(Transform player);

    // --- CÁC HÀM DÙNG CHUNG (UTILITY) ---
    
    // Hàm hỗ trợ kiểm tra cooldown cho các con
    protected bool CanAttack()
    {
        if (Time.time >= lastAttackTime + data.attackCooldown)
        {
            lastAttackTime = Time.time;
            return true;
        }
        return false;
    }

    public void MoveToPosition(Vector3 targetPos, bool ignoreStoppingDistance = false)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.stoppingDistance = ignoreStoppingDistance ? 0f : data.attackRange;
            agent.SetDestination(targetPos);
        }
    }

    public void StopMoving()
    {
        if (agent != null && agent.isActiveAndEnabled) { agent.isStopped = true; agent.velocity = Vector3.zero; }
    }

    public bool HasReachedDestination()
    {
        if (agent != null && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                return (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
        }
        return false;
    }

    public void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (anim != null) anim.SetTrigger("damage");
        if (currentHealth <= 0) Die();
    }

    protected void Die()
    {
        if (anim != null) anim.SetTrigger("die"); 
        if (agent != null) agent.isStopped = true;
        this.enabled = false; 
        Destroy(gameObject, 2f);
        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
    }
    
    // Hàm này cho AlarmMonster dùng
    public void PlayAlarmAnimation() { if (anim != null) anim.SetTrigger("callAlarm"); }

    void OnDestroy()
    {
        if (MonsterManager.Instance != null) MonsterManager.Instance.UnregisterMonster(this);
    }
    // ... (Các đoạn code logic phía trên giữ nguyên)

    // --- PHẦN VẼ GIZMOS (DEBUG) ---
#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // 1. Vẽ Tầm nhìn (Màu vàng nhạt)
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);

        // 2. Vẽ Góc nhìn (2 đường thẳng ranh giới)
        Vector3 viewAngleA = DirFromAngle(-data.viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(data.viewAngle / 2, false);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * data.detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * data.detectionRange);

        // 3. Vẽ Tầm đánh (Màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);

        // 4. Vẽ Phạm vi đi tuần (Màu xanh Cyan)
        // Nếu đang chạy game thì vẽ từ vị trí bắt đầu (startPosition)
        // Nếu chưa chạy game thì vẽ từ vị trí hiện tại
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawWireSphere(center, data.wanderRadius);

        // 5. Vẽ vị trí Player lần cuối nhìn thấy (Tracking)
        if (lastKnownPosition != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPosition.Value, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownPosition.Value);
        }
    }

    // Hàm phụ trợ tính góc
    protected Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
#endif
} // Kết thúc class MonsterController
