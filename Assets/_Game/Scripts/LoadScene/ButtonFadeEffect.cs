using UnityEngine;
using UnityEngine.UI; // Không cần thiết lắm nhưng cứ để
using System.Collections;

public class ButtonFadeEffect : MonoBehaviour
{
    [Header("Cài đặt")]
    [SerializeField] private CanvasGroup btnCanvasGroup; // Biến chứa Canvas Group
    [SerializeField] private float waitTime = 3.0f;      // Thời gian đợi (3s)
    [SerializeField] private float fadeDuration = 1.5f;  // Thời gian để hiện rõ dần (1.5s cho mượt)

    void Start()
    {
        // 1. Thiết lập trạng thái ban đầu: Ẩn hoàn toàn
        if (btnCanvasGroup == null)
            btnCanvasGroup = GetComponent<CanvasGroup>();

        btnCanvasGroup.alpha = 0f;               // Độ trong suốt = 0 (tàng hình)
        btnCanvasGroup.interactable = false;     // Không cho bấm
        btnCanvasGroup.blocksRaycasts = false;   // Chuột xuyên qua

        // 2. Bắt đầu quy trình hiện nút
        StartCoroutine(ProcessFadeIn());
    }

    IEnumerator ProcessFadeIn()
    {
        // Bước 1: Đợi 3 giây
        yield return new WaitForSeconds(waitTime);

        // Bước 2: Hiện từ từ (Fade In)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            
            // Lerp giúp chuyển giá trị alpha mượt từ 0 lên 1
            btnCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            
            yield return null;
        }

        // Bước 3: Kết thúc, đảm bảo nút hiện rõ 100% và cho phép bấm
        btnCanvasGroup.alpha = 1f;
        btnCanvasGroup.interactable = true;
        btnCanvasGroup.blocksRaycasts = true;
    }
}