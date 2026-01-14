using UnityEngine;
using System.Collections;

public class BoatController : MonoBehaviour
{
    [Header("Settings")]
    public float boatSpeed = 10f;       
    public Transform steeringPos;       
    public GameObject fPromptIcon;      

    [Header("UI Message")]
    public GameObject openMapMessage;   

    [Header("Camera System")]
    public GameObject boatCamera; 

    private void Start()
    {
        if (fPromptIcon != null) fPromptIcon.SetActive(false);
        if (openMapMessage != null) openMapMessage.SetActive(false);
        if (boatCamera != null) boatCamera.SetActive(false);
    }

    public void TogglePrompt(bool isVisible)
    {
        if (fPromptIcon != null) fPromptIcon.SetActive(isVisible);
        if (openMapMessage != null) openMapMessage.SetActive(isVisible);
    }

    public void SetBoatCamera(bool isActive)
    {
        if (boatCamera != null) boatCamera.SetActive(isActive);
    }

    // --- [SỬA ĐỔI QUAN TRỌNG] ---
    public IEnumerator MoveToTarget(Vector3 targetPos)
    {
        // 1. Quay đầu (Giữ nguyên)
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0; // Đảm bảo chỉ xoay ngang, không chúi đầu xuống đất
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            float rotateTime = 0f;
            while (rotateTime < 1f)
            {
                rotateTime += Time.deltaTime * 2f;
                // Chỉ xoay trục Y
                Quaternion currentRot = transform.rotation;
                Quaternion targetRot = Quaternion.Euler(currentRot.eulerAngles.x, lookRot.eulerAngles.y, currentRot.eulerAngles.z);
                transform.rotation = Quaternion.Slerp(currentRot, targetRot, rotateTime);
                yield return null;
            }
        }

        // 2. Di chuyển (SỬA LẠI LOGIC KHOẢNG CÁCH)
        // Dùng khoảng cách phẳng (2D) để tránh lỗi lệch độ cao
        while (FlatDistance(transform.position, targetPos) > 1.0f) // Tăng phạm vi nhận diện lên 1.0f cho dễ trúng
        {
            // Di chuyển thuyền nhưng giữ nguyên độ cao Y hiện tại của thuyền (để không bị chìm/bay)
            Vector3 targetLevelPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            
            transform.position = Vector3.MoveTowards(transform.position, targetLevelPos, boatSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // Hàm tính khoảng cách bỏ qua trục Y
    float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector2 aFlat = new Vector2(a.x, a.z);
        Vector2 bFlat = new Vector2(b.x, b.z);
        return Vector2.Distance(aFlat, bFlat);
    }
}