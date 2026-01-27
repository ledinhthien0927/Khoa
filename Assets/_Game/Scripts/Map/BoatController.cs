using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class BoatController : MonoBehaviour
{
    [Header("UI & Interactions")]
    public GameObject fPromptIcon;      
    public GameObject openMapMessage;   
    
    [Header("Boat Positions")]
    public Transform steeringPos;     
    public Transform deckEdgePoint;   
    public Transform accessPoint;     
    
    [Header("Camera")]
    public GameObject boatCamera; 

    [Header("Realistic Movement Settings")]
    [Tooltip("Tốc độ bẻ lái (Thấp = tàu nặng, Cao = tàu nhẹ)")]
    [SerializeField] private float turnSpeed = 2.0f; 
    
    [Tooltip("Độ nghiêng thân tàu khi cua (Tạo cảm giác rẽ nước)")]
    [SerializeField] private float tiltAmount = 5.0f; 
    
    [Tooltip("Tốc độ nghiêng trả về cân bằng")]
    [SerializeField] private float tiltSpeed = 3.0f;

    private NavMeshAgent _agent;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        // [QUAN TRỌNG] Tắt tự động xoay của NavMesh để ta tự code xoay cho mượt
        _agent.updateRotation = false; 
        _agent.updatePosition = true;
    }

    void Start()
    {
        if (fPromptIcon != null) fPromptIcon.SetActive(false);
        if (openMapMessage != null) openMapMessage.SetActive(false);
        if (boatCamera != null) boatCamera.SetActive(false);
    }

    void Update()
    {
        // Gọi hàm xử lý xoay trong mỗi khung hình
        HandleBoatRotation();
    }

    // --- LOGIC XOAY TÀU MƯỢT MÀ ---
    void HandleBoatRotation()
    {
        // Chỉ xoay khi tàu đang di chuyển
        if (_agent.velocity.sqrMagnitude > 0.1f)
        {
            // 1. Lấy hướng di chuyển hiện tại của NavMesh
            Vector3 direction = _agent.velocity.normalized;

            // 2. Tính toán góc quay mục tiêu (Chỉ xoay trục Y)
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 3. Xoay từ từ thân tàu về hướng đó (Smooth Turn)
            // Dùng Slerp để xoay mượt mà theo thời gian
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

            // 4. Xử lý độ nghiêng (Banking)
            // Tính góc cua hiện tại để biết nên nghiêng trái hay phải
            // Vector3.Dot giúp so sánh hướng bên phải của tàu với hướng di chuyển
            float turnAmount = Vector3.Dot(transform.right, direction); 
            
            // Tính độ nghiêng mục tiêu (Nghiêng trục Z)
            float targetTiltZ = -turnAmount * tiltAmount; 

            // Áp dụng độ nghiêng vào rotation hiện tại
            Vector3 currentEuler = transform.rotation.eulerAngles;
            float newTiltZ = Mathf.LerpAngle(currentEuler.z, targetTiltZ, Time.deltaTime * tiltSpeed);
            
            transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, newTiltZ);
        }
        else
        {
            // Khi dừng lại, trả thuyền về trạng thái cân bằng (hết nghiêng)
            Vector3 currentEuler = transform.rotation.eulerAngles;
            float newTiltZ = Mathf.LerpAngle(currentEuler.z, 0, Time.deltaTime * tiltSpeed);
            transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, newTiltZ);
        }
    }

    public void TogglePrompt(bool isVisible)
    {
        if (fPromptIcon != null) fPromptIcon.SetActive(isVisible);
        if (openMapMessage != null) openMapMessage.SetActive(isVisible);
    }

    public void SetBoatCamera(bool isActive)
    {
        if (boatCamera != null) boatCamera.SetActive(isActive);
    }

    // --- NAVMESH LOGIC ---
    public void SetDestination(Vector3 targetPos)
    {
        if (_agent != null)
        {
            _agent.isStopped = false;
            _agent.SetDestination(targetPos);
        }
    }

    public bool IsReachedDestination()
    {
        if (_agent.pathPending) return false;
        if (_agent.remainingDistance <= _agent.stoppingDistance + 0.5f)
        {
            if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f) return true;
        }
        return false;
    }
}