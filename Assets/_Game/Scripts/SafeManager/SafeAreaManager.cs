using UnityEngine;

public class SafeAreaManager : MonoBehaviour
{
    RectTransform panelRect;

    void Start()
    {
        panelRect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
    }

    // Tùy chọn: Gọi lại nếu màn hình xoay
    void Update()
    {
        ApplySafeArea();
    }
}