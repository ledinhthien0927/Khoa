using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Hướng Nhìn")]
    public Transform target;           
    public Vector3 pivotOffset = new Vector3(0, 1.5f, 0); 
    public Vector3 shoulderOffset = new Vector3(0.5f, 0, 0); 

    [Header("Settings - Độ nhạy chuột")]
    public float mouseSensitivity = 3.0f; // Độ nhạy bình thường
    public float aimSensitivity = 1.0f;   // [MỚI] Độ nhạy khi ngắm (chậm hơn)
    
    [Header("Settings - Di chuyển & Xoay")]
    public float followSpeed = 20f;    
    
    [Header("Zoom & Giới hạn")]
    public float distance = 5.0f;      
    public float minDistance = 2.0f;
    public float maxDistance = 10.0f;
    public Vector2 pitchLimit = new Vector2(-40, 80); 

    [Header("Wall Collision - Xuyên tường")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f; 
    public float collisionOffset = 0.2f; 

    // Private variables
    private float _yaw;   
    private float _pitch; 
    private float _currentDistance;
    private bool _isCursorLocked = true;
    private bool _isAiming = false; // [MỚI] Biến kiểm tra trạng thái ngắm

    void Start()
    {
        LockCursor(true);
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;
        _currentDistance = distance;
    }

    void Update()
    {
        HandleCursorInput();
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (!_isCursorLocked) return;

        // [CẬP NHẬT] Chọn độ nhạy dựa trên trạng thái ngắm
        float currentSens = _isAiming ? aimSensitivity : mouseSensitivity;

        // 1. Nhận Input với độ nhạy tương ứng
        _yaw += Input.GetAxis("Mouse X") * currentSens;
        _pitch -= Input.GetAxis("Mouse Y") * currentSens;
        _pitch = Mathf.Clamp(_pitch, pitchLimit.x, pitchLimit.y);

        // 2. Xử lý Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * 5, minDistance, maxDistance);

        // 3. Tính toán vị trí
        CalculateCameraPosition();
    }

    // [MỚI] Hàm này để PlayerController gọi sang
    public void SetAiming(bool isAiming)
    {
        _isAiming = isAiming;
    }

    // ... (Giữ nguyên các hàm HandleCursorInput, LockCursor, CalculateCameraPosition cũ) ...
    void HandleCursorInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Input.GetMouseButtonDown(0) && !_isCursorLocked) LockCursor(true);
    }

    void LockCursor(bool isLocked)
    {
        _isCursorLocked = isLocked;
        Cursor.visible = !isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }

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