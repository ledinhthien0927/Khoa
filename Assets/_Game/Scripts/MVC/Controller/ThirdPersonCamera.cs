using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Hướng Nhìn")]
    public Transform target;           
    public Vector3 pivotOffset = new Vector3(0, 1.5f, 0); 
    public Vector3 shoulderOffset = new Vector3(0.5f, 0, 0); 

    [Header("Settings - Di chuyển & Xoay")]
    public float mouseSensitivity = 3.0f;
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
    private bool _isCursorLocked = true; // Biến kiểm soát trạng thái chuột

    void Start()
    {
        // Mặc định khi vào game là khóa chuột ngay
        LockCursor(true);

        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;
        _currentDistance = distance;
    }

    void Update()
    {
        // Xử lý ẩn/hiện chuột
        HandleCursorInput();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Nếu chuột đang hiện (đang dùng menu), thì KHÔNG xoay camera
        if (!_isCursorLocked) return;

        // 1. Nhận Input xoay camera
        _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, pitchLimit.x, pitchLimit.y);

        // 2. Xử lý Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * 5, minDistance, maxDistance);

        // 3. Tính toán vị trí & Va chạm
        CalculateCameraPosition();
    }

    void HandleCursorInput()
    {
        // Bấm ESC để mở khóa chuột (hiện chuột)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }

        // Bấm chuột trái vào game để khóa lại (ẩn chuột)
        if (Input.GetMouseButtonDown(0) && !_isCursorLocked)
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

    void CalculateCameraPosition()
    {
        // Tính toán vị trí gốc (Pivot)
        Vector3 targetPivotPosition = target.position + pivotOffset;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);

        // Hướng lùi ra sau
        Vector3 desiredDirection = rotation * Vector3.back; 
        Vector3 desiredPosition = targetPivotPosition + (rotation * shoulderOffset) + (desiredDirection * distance);

        // Xử lý va chạm
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

        // Di chuyển camera
        transform.position = Vector3.Lerp(transform.position, finalPosition, followSpeed * Time.deltaTime);
        transform.rotation = rotation; 
    }
}