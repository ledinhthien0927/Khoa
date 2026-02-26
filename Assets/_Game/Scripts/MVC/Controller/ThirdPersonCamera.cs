using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Hướng Nhìn")]
    public Transform target;           
    public Vector3 pivotOffset = new Vector3(0, 1.5f, 0); 
    public Vector3 shoulderOffset = new Vector3(0.5f, 0, 0); 

    [Header("Settings - Độ nhạy chuột")]
    public float mouseSensitivity = 3.0f;
    public float aimSensitivity = 1.0f;   
    
    [Header("Settings - Di chuyển & Xoay")]
    public float followSpeed = 20f;    
    
    [Header("Giới hạn góc ngẩng (Pitch)")]
    [Tooltip("Góc giới hạn bình thường (Tránh nhìn xuyên đất, ví dụ: -10 đến 80)")]
    public Vector2 normalPitchLimit = new Vector2(-10, 80); 
    [Tooltip("Góc giới hạn khi bơi (Tránh nhìn dưới nước, ví dụ: 10 đến 80)")]
    public Vector2 swimPitchLimit = new Vector2(10, 80);    

    [Header("Khoảng cách")]
    public float distance = 5.0f;      

    [Header("Ẩn vật thể che khuất")]
    [Tooltip("Layer của các vật thể sẽ BỊ ẨN khi che khuất nhân vật (Cây cối, Lá, Mái nhà...)")]
    public LayerMask hideableLayers; 

    // Private variables
    private float _yaw;   
    private float _pitch; 
    private float _currentDistance;
    private bool _isCursorLocked = true; 
    private bool _isAiming = false;      
    private bool _isSwimming = false;    

    // Danh sách lưu các vật thể đang bị ẩn để bật lại
    private List<Renderer> _hiddenRenderers = new List<Renderer>();

    // --- [MỚI] Tham chiếu đến PlayerController ---
    private PlayerController _pc;

    void Start()
    {
        LockCursor(true);
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;
        _currentDistance = distance;

        // Lấy thông tin Player để kiểm tra trạng thái (rèn đồ, nói chuyện...)
        if (target != null) _pc = target.GetComponent<PlayerController>();
    }

    void Update()
    {
        HandleCursorInput();
        if (_isCursorLocked)
        {
            HandleRotationInput();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        CalculateCameraPosition();
    }

    // --- KẾT NỐI VỚI PLAYER CONTROLLER ---
    public void SetAiming(bool isAiming)
    {
        _isAiming = isAiming;
    }

    public void SetSwimmingState(bool isSwimming)
    {
        _isSwimming = isSwimming;
        
        // Nếu vừa xuống nước mà góc nhìn đang thấp hơn giới hạn bơi, ép góc ngẩng lên
        if (_isSwimming && _pitch < swimPitchLimit.x)
        {
            _pitch = swimPitchLimit.x;
        }
    }

    // --- LOGIC XỬ LÝ CHUỘT (ĐÃ ĐƯỢC LÀM LẠI) ---
    void HandleCursorInput()
    {
        // [QUAN TRỌNG NHẤT] Đồng bộ với thực tế!
        // Nếu hệ thống Thoại/Rèn lén bật chuột lên, Camera phải cập nhật lại bộ nhớ ngay.
        if (Cursor.visible) 
        {
            _isCursorLocked = false;
        }

        // 1. Phím Escape luôn được quyền bật/tắt chuột
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(!_isCursorLocked);

        // 2. CÁC TRƯỜNG HỢP NGOẠI LỆ (Không bao giờ được khóa chuột)
        // Đang rèn đồ
        if (_pc != null && _pc.model.isSmithing) return;
        
        // Đang nói chuyện (Bạn nhớ thay class kiểm tra thoại của bạn vào đây nhé)
        // if (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing) return;

        // 3. CHỈ KHÓA CHUỘT KHI CÓ THAO TÁC DI CHUYỂN
        bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
        
        // Lúc này _isCursorLocked đã được đồng bộ chuẩn xác nên lệnh này sẽ chạy thành công
        if (isMoving && !_isCursorLocked)
        {
            LockCursor(true);
        }
    }

    void LockCursor(bool isLocked)
    {
        _isCursorLocked = isLocked;
        Cursor.visible = !isLocked; 
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    // --- LOGIC XOAY CAMERA ---
    void HandleRotationInput()
    {
        float currentSens = _isAiming ? aimSensitivity : mouseSensitivity;

        _yaw += Input.GetAxis("Mouse X") * currentSens;
        _pitch -= Input.GetAxis("Mouse Y") * currentSens;
        
        // Chọn giới hạn góc tùy vào việc có đang bơi hay không
        Vector2 currentPitchLimit = _isSwimming ? swimPitchLimit : normalPitchLimit;
        _pitch = Mathf.Clamp(_pitch, currentPitchLimit.x, currentPitchLimit.y);
    }

    // --- TÍNH TOÁN VỊ TRÍ & ẨN VẬT THỂ ---
    void CalculateCameraPosition()
    {
        Vector3 targetPivotPosition = target.position + pivotOffset;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 desiredDirection = rotation * Vector3.back; 
        
        // Luôn giữ khoảng cách cố định, bỏ cơ chế thu gần lại khi chạm tường
        _currentDistance = distance;
        
        Vector3 finalPosition = targetPivotPosition + (rotation * shoulderOffset) + (desiredDirection * _currentDistance);

        // Di chuyển camera
        transform.position = Vector3.Lerp(transform.position, finalPosition, followSpeed * Time.deltaTime);
        transform.rotation = rotation; 

        // Ẩn vật cản (Foliage, Mái nhà...) nằm giữa Camera và Player để nhìn xuyên rõ ràng
        HandleObstacles(targetPivotPosition, transform.position);
    }

    void HandleObstacles(Vector3 targetPos, Vector3 cameraPos)
    {
        // Bật lại các vật thể cũ không còn che khuất
        foreach (var renderer in _hiddenRenderers)
        {
            if (renderer != null) renderer.enabled = true;
        }
        _hiddenRenderers.Clear();

        // Bắn tia từ Player tới Camera để tìm vật che khuất
        float dist = Vector3.Distance(targetPos, cameraPos);
        Vector3 dir = (cameraPos - targetPos).normalized;

        RaycastHit[] hits = Physics.RaycastAll(targetPos, dir, dist, hideableLayers);
        foreach (var hit in hits)
        {
            Renderer r = hit.collider.GetComponent<Renderer>();
            if (r != null)
            {
                r.enabled = false; // Tạm thời ẩn vật thể
                _hiddenRenderers.Add(r);
            }
        }
    }
}