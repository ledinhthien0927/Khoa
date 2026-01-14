using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Animator _animator;
    public EnemyData Data; // Model dữ liệu

    // Các biến Logic nội bộ
    private Transform _target;
    private float _strafeDirection = 1f; // 1 = Phải, -1 = Trái
    private float _changeStrafeTimer = 0f;

    void Start()
    {
        // Đăng ký bản thân với Manager khi sinh ra
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(this);
            _target = EnemyManager.Instance.PlayerTransform;
        }

        // Tắt tự xoay của NavMesh (để ta tự code xoay mặt cho chuẩn game hành động)
        _agent.updateRotation = false;
        
        // Reset máu
        if (Data != null) Data.CurrentHP = Data.MaxHP;
    }

    // Hàm Update thủ công (Gọi từ Manager)
    public void ManualUpdate(float deltaTime)
    {
        if (_target == null || (Data != null && Data.IsDead)) return;

        HandleRotation(deltaTime);
        HandleAnimation(deltaTime);
    }

    // --- CÁC HÀM HÀNH ĐỘNG (Được gọi từ Behavior Graph) ---

    // 1. Hàm Chạy đuổi
    public void ChaseTarget()
    {
        if (_target == null) return;
        _agent.speed = Data.RunSpeed;
        _agent.SetDestination(_target.position);
        if (Data != null) Data.IsMoving = true;
    }

    // 2. Hàm Đi vòng quanh (Strafing)
    public void StrafeAround()
    {
        if (_target == null) return;
        _agent.speed = Data.WalkSpeed;
        if (Data != null) Data.IsMoving = true;

        Vector3 vectorToTarget = _target.position - transform.position;
        Vector3 dirSideways = Vector3.Cross(Vector3.up, vectorToTarget.normalized);

        // Random đổi hướng sau vài giây
        _changeStrafeTimer -= Time.deltaTime;
        if (_changeStrafeTimer <= 0)
        {
            _strafeDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
            _changeStrafeTimer = Random.Range(2f, 4f);
        }

        // Di chuyển sang ngang
        _agent.SetDestination(transform.position + dirSideways * _strafeDirection);
    }

    // 3. Hàm Tấn công (ĐÂY LÀ HÀM BẠN ĐANG THIẾU)
    public void PerformAttack()
    {
        // Kích hoạt Trigger trong Animator
        _animator.SetTrigger("Attack"); 
    }

    // 4. Hàm Lùi lại
    public void Retreat()
    {
        if (_target == null) return;
        _agent.speed = Data.WalkSpeed;
        Vector3 dirAway = (transform.position - _target.position).normalized;
        _agent.SetDestination(transform.position + dirAway * 2f);
    }

    // --- HÀM HỖ TRỢ (Xoay & Animation) ---
    private void HandleRotation(float dt)
    {
        if (_target == null) return;
        Vector3 direction = (_target.position - transform.position).normalized;
        direction.y = 0; // Khóa trục Y (không ngửa mặt lên trời)
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, dt * 10f);
        }
    }

    private void HandleAnimation(float dt)
    {
        // Tính toán animation dựa trên vận tốc thật
        Vector3 localVel = transform.InverseTransformDirection(_agent.velocity);
        float speed = _agent.speed > 0.1f ? _agent.speed : 1f;

        _animator.SetFloat("InputX", localVel.x / speed, 0.1f, dt);
        _animator.SetFloat("InputY", localVel.z / speed, 0.1f, dt);
    }
    
    private void OnDestroy()
    {
        if (EnemyManager.Instance != null) EnemyManager.Instance.UnregisterEnemy(this);
    }
}