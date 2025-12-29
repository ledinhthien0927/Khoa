using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target; // Kéo Player vào đây
    public Vector3 offset = new Vector3(0, 5, -8); // Góc nhìn mặc định
    public float smoothSpeed = 10f; // Tốc độ di chuyển (Tăng lên để phản ứng nhanh với tường)

    [Header("Wall Collision - Xử lý che khuất")]
    public LayerMask collisionLayers; // Chọn lớp những vật cản (Tường, Nhà...)
    public float cameraRadius = 0.5f; // Bán kính camera để không bị xuyên tường
    public float wallOffset = 0.2f;   // Khoảng cách an toàn để camera không dính sát mặt tường

    // Biến xử lý Rung lắc
    private float _shakeTimer;
    private float _shakeMagnitude;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Tính vị trí mong muốn chuẩn (khi không bị che)
        Vector3 desiredPosition = target.position + offset;

        // 2. Xử lý va chạm tường (Raycast)
        // Bắt đầu bắn tia từ ngực nhân vật (cao hơn chân 1.5m) để tránh va chạm với mặt đất
        Vector3 castOrigin = target.position + Vector3.up * 1.5f;
        Vector3 dirToCamera = (desiredPosition - castOrigin).normalized;
        float distToCamera = Vector3.Distance(castOrigin, desiredPosition);

        // Bắn tia kiểm tra xem có gì chắn giữa Nhân vật và Camera không
        if (Physics.SphereCast(castOrigin, cameraRadius, dirToCamera, out RaycastHit hit, distToCamera, collisionLayers))
        {
            // Nếu trúng tường -> Đặt Camera ngay trước điểm va chạm
            // (hit.point là điểm trúng, ta cộng thêm wallOffset để đẩy nó ra xa tường một chút)
            desiredPosition = hit.point + (hit.normal * wallOffset);
        }

        // 3. Di chuyển Camera mượt mà tới vị trí đã tính toán
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 4. Cộng thêm độ rung (Nếu có)
        if (_shakeTimer > 0)
        {
            smoothedPosition += Random.insideUnitSphere * _shakeMagnitude;
            _shakeTimer -= Time.deltaTime;
        }
        else
        {
            _shakeTimer = 0;
        }

        transform.position = smoothedPosition;
        
        // Luôn nhìn vào Nhân vật (cộng thêm 1.5m để nhìn vào lưng/đầu thay vì nhìn vào chân)
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    // Hàm gọi Rung từ bên ngoài
    public void TriggerShake(float duration, float magnitude)
    {
        _shakeTimer = duration;
        _shakeMagnitude = magnitude;
    }
}