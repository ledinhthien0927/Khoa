using UnityEngine;
using UnityEngine.AI; // Cần thiết để điều khiển NavMeshAgent

public class MonsterController : MonoBehaviour
{
    public MonsterData data;
    private NavMeshAgent agent;
    
    // Biến phụ trợ cho logic tấn công
    private float lastAttackTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Lấy tốc độ từ ScriptableObject gán cho Agent
        if (agent != null && data != null)
        {
            agent.speed = data.speed;
        }

        if (MonsterManager.Instance != null)
        {
            MonsterManager.Instance.RegisterMonster(this);
        }
    }

    // 1. Hàm di chuyển đến vị trí chỉ định
    public void MoveToPosition(Vector3 targetPos)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false; // Đảm bảo agent không bị tạm dừng
            agent.SetDestination(targetPos);
        }
    }

    // 2. Hàm dừng di chuyển (Dành cho quái Ranged khi đứng bắn)
    public void StopMoving()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true; // Tạm dừng việc di chuyển trên NavMesh
        }
    }

    // 3. Hàm thực hiện tấn công (Manager gọi hàm này)
    public void Attack(Vector3 targetPos)
    {
        // Xoay quái vật về hướng Player cho tự nhiên
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0; // Không xoay theo trục dọc để tránh quái bị chúi xuống đất
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }

        // Kiểm tra thời gian hồi chiêu (ví dụ: mỗi 2 giây bắn 1 lần)
        if (Time.time >= lastAttackTime + 2f)
        {
            ExecuteAttackAction();
            lastAttackTime = Time.time;
        }
    }

    // Hàm cụ thể để thực hiện hành động bắn/chém
    private void ExecuteAttackAction()
    {
        // Hiện tại dùng Log để kiểm tra logic
        Debug.Log($"<color=orange>{gameObject.name} (loại {data.type}) đang tấn công mục tiêu!</color>");
        
        // Sau này bạn có thể Instantiate viên đạn hoặc chạy Animation ở đây
    }

    void OnDestroy()
    {
        // Luôn hủy đăng ký khi đối tượng bị xóa để tránh lỗi bộ nhớ
        if (MonsterManager.Instance != null)
        {
            MonsterManager.Instance.UnregisterMonster(this);
        }
    }
}