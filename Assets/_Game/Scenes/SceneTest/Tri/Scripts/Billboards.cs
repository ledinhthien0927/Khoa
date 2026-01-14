using UnityEngine;

public class Billboards : MonoBehaviour
{
    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam == null) return;

        transform.LookAt(transform.position + mainCam.transform.forward);
    }
}