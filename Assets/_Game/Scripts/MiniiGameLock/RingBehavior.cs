using UnityEngine;
using System.Collections;

public class RingBehavior3D : MonoBehaviour
{
    // Mảng 8 phần tử tương ứng với 8 nấc xoay (45 độ/nấc)
    // Bạn setup màu trong Inspector từ góc 12h, theo chiều kim đồng hồ
    public LightColor[] gems = new LightColor[8]; 
    public int currentTopIndex = 0; 
    private bool isRotating = false;

    // Trục xoay: Nếu khóa gắn tường nhìn thẳng vào Camera, thường là trục Z
    public Vector3 rotationAxis = Vector3.forward; 

    public void RotateRing(int direction) // 1 là phải, -1 là trái
    {
        if (isRotating) return;

        // Cập nhật mảng logic
        currentTopIndex = (currentTopIndex - direction + 8) % 8;

        // Xoay 3D mượt mà 45 độ
        StartCoroutine(SmoothRotate3D(-direction * 45f, 0.25f)); 
    }

    private IEnumerator SmoothRotate3D(float angle, float duration)
    {
        isRotating = true;
        Quaternion startRot = transform.localRotation;
        Quaternion endRot = startRot * Quaternion.AngleAxis(angle, rotationAxis);
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            // Quaternion.Slerp giúp xoay 3D tròn và đều hơn Lerp
            transform.localRotation = Quaternion.Slerp(startRot, endRot, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = endRot;
        isRotating = false;
    }

    public LightColor GetTopGemColor()
    {
        return gems[currentTopIndex];
    }

    public bool IsRotating()
    {
        return isRotating;
    }
}