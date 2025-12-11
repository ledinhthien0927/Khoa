using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Kéo Player vào đây
    public Vector3 offset = new Vector3(0, 5, -8); // Góc nhìn từ trên xuống (giống Diablo/LoL)
    public float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        if (target == null) return;
        
        // Vị trí mong muốn
        Vector3 desiredPosition = target.position + offset;
        // Di chuyển mượt mà tới đó
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        transform.position = smoothedPosition;
        transform.LookAt(target); // Luôn nhìn vào nhân vật
    }
}