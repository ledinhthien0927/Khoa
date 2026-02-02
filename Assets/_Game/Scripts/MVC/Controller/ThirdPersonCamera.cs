using UnityEngine;
using UnityEngine.EventSystems; // BẮT BUỘC: Để kiểm tra chuột có nhấn vào UI không

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Hướng Nhìn")]
    public Transform target;           
    public Vector3 pivotOffset = new Vector3(0, 1.5f, 0); 
    public Vector3 shoulderOffset = new Vector3(0.5f, 0, 0); 

    [Header("Settings - Độ nhạy chuột")]
    public float mouseSensitivity = 3.0f; // Độ nhạy bình thường
    public float aimSensitivity = 1.0f;   // [ĐÃ KHÔI PHỤC] Độ nhạy khi ngắm (chậm hơn)
    
    [Header("Settings - Di chuyển & Xoay")]
    public float followSpeed = 20f;    
    public Vector2 pitchLimit = new Vector2(-40, 80); // Giới hạn góc ngẩng lên/xuống

    [Header("Zoom & Giới hạn")]
    public float distance = 5.0f;      
    public float minDistance = 2.0f;
    public float maxDistance = 10.0f;

    [Header("Wall Collision - Xuyên tường")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f; 
    public float collisionOffset = 0.2f; 

    // Private variables
    private float _yaw;   
    private float _pitch; 
    private float _currentDistance;
    private bool _isCursorLocked = true; // True = Đang chơi, False = Đang dùng UI
    private bool _isAiming = false;      // [ĐÃ KHÔI PHỤC] Biến kiểm tra trạng thái ngắm

    void Start()
    {
        // Khởi đầu game: Khóa chuột để chơi ngay
        LockCursor(true);
        
        // Lấy góc quay hiện tại làm gốc
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;
        _currentDistance = distance;
    }

    void Update()
    {
        // 1. Luôn kiểm tra input chuột (Esc hoặc Click)
        HandleCursorInput();

        // 2. Chỉ cho phép xoay Camera khi chuột ĐANG KHÓA (Ẩn)
        if (_isCursorLocked)
        {
            HandleRotationInput();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 3. Tính toán vị trí Camera (Luôn chạy để Camera bám theo Player dù đang bật UI)
        CalculateCameraPosition();
    }

    // ================= [QUAN TRỌNG] KẾT NỐI VỚI PLAYER CONTROLLER =================

    // Hàm này để PlayerController gọi sang khi giương cung
    public void SetAiming(bool isAiming)
    {
        _isAiming = isAiming;
    }

    // ================= LOGIC XỬ LÝ CHUỘT & UI =================

    void HandleCursorInput()
    {
        // A. Nhấn ESC để bật/tắt chuột
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(!_isCursorLocked);
        }

        // B. Nhấn Chuột Trái để quay lại game
        if (Input.GetMouseButtonDown(0))
        {
            // Kiểm tra: Có đang nhấn lên UI (Nút, Bảng nhiệm vụ...) không?
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Đang nhấn vào UI -> KHÔNG LÀM GÌ CẢ (Giữ chuột để bấm tiếp)
                return;
            }

            // Nếu nhấn vào Khoảng không (Màn hình game) -> Khóa chuột lại để chơi
            LockCursor(true);
        }
    }

    void LockCursor(bool isLocked)
    {
        _isCursorLocked = isLocked;
        Cursor.visible = !isLocked; 
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    // ================= LOGIC XOAY CAMERA (ĐÃ SỬA LẠI LOGIC NGẮM) =================

    void HandleRotationInput()
    {
        // [ĐÃ KHÔI PHỤC] Chọn độ nhạy dựa trên trạng thái ngắm
        // Nếu đang ngắm (_isAiming = true) thì dùng aimSensitivity, ngược lại dùng mouseSensitivity
        float currentSens = _isAiming ? aimSensitivity : mouseSensitivity;

        // Xoay trái phải (Yaw)
        _yaw += Input.GetAxis("Mouse X") * currentSens;
        
        // Ngẩng lên xuống (Pitch)
        _pitch -= Input.GetAxis("Mouse Y") * currentSens;
        _pitch = Mathf.Clamp(_pitch, pitchLimit.x, pitchLimit.y);

        // Zoom ra vào bằng con lăn
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * 5, minDistance, maxDistance);
    }

    // ================= LOGIC TÍNH TOÁN VỊ TRÍ & XUYÊN TƯỜNG =================

    void CalculateCameraPosition()
    {
        Vector3 targetPivotPosition = target.position + pivotOffset;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 desiredDirection = rotation * Vector3.back; 
        Vector3 desiredPosition = targetPivotPosition + (rotation * shoulderOffset) + (desiredDirection * distance);

        RaycastHit hit;
        Vector3 directionToCam = (desiredPosition - targetPivotPosition).normalized;
        float distToCam = Vector3.Distance(targetPivotPosition, desiredPosition);
        Vector3 finalPosition;

        if (Physics.SphereCast(targetPivotPosition, collisionRadius, directionToCam, out hit, distToCam, collisionLayers))
        {
            _currentDistance = hit.distance - collisionOffset;
            if (_currentDistance < collisionRadius) _currentDistance = collisionRadius;
            
            finalPosition = targetPivotPosition + (rotation * shoulderOffset) + (directionToCam * _currentDistance);
        }
        else
        {
            _currentDistance = Mathf.Lerp(_currentDistance, distance, Time.deltaTime * 20f); 
            finalPosition = targetPivotPosition + (rotation * shoulderOffset) + (desiredDirection * _currentDistance);
        }

        transform.position = Vector3.Lerp(transform.position, finalPosition, followSpeed * Time.deltaTime);
        transform.rotation = rotation; 
    }
}