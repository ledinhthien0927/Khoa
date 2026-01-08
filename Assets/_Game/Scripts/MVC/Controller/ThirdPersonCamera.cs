using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target; 
    public Vector3 offset = new Vector3(0, 5, -8); 
    public float smoothSpeed = 10f; 

    [Header("Wall Collision - Xuyên tường")]
    public LayerMask collisionLayers; 
    public float cameraRadius = 0.5f; 
    public float wallOffset = 0.2f;   

    void LateUpdate()
    {
        if (target == null) return;

        // Tính vị trí mong muốn
        Vector3 desiredPosition = target.position + offset;

        // Xử lý xuyên tường (Raycast từ vai nhân vật)
        Vector3 castOrigin = target.position + Vector3.up * 1.5f;
        Vector3 dirToCamera = (desiredPosition - castOrigin).normalized;
        float distToCamera = Vector3.Distance(castOrigin, desiredPosition);

        if (Physics.SphereCast(castOrigin, cameraRadius, dirToCamera, out RaycastHit hit, distToCamera, collisionLayers))
        {
            desiredPosition = hit.point + (hit.normal * wallOffset);
        }

        // Di chuyển mượt mà
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.LookAt(target.position + Vector3.up * 1.5f); // Luôn nhìn vào lưng/đầu nhân vật
    }
}