using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Transform mainCam;

    void Start()
    {
        // Tự động tìm Camera chính của người chơi
        if (Camera.main != null)
        {
            mainCam = Camera.main.transform;
        }
    }

    // Dùng LateUpdate để đảm bảo UI xoay SAU KHI camera đã di chuyển xong (giúp không bị giật lag hình)
    void LateUpdate()
    {
        if (mainCam != null)
        {
            // Ép thanh máu luôn nhìn về cùng hướng với Camera
            transform.LookAt(transform.position + mainCam.forward);
        }
    }
}