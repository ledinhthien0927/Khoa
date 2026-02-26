using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _mainCameraTransform;

    void Start()
    {
        // Tự động tìm Camera chính trong Scene
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
    }

    // Dùng LateUpdate để đảm bảo UI xoay SAU KHI camera đã di chuyển xong
    void LateUpdate()
    {
        if (_mainCameraTransform != null)
        {
            // Làm cho UI luôn nhìn thẳng vào hướng của Camera
            transform.LookAt(transform.position + _mainCameraTransform.forward);
        }
    }
}