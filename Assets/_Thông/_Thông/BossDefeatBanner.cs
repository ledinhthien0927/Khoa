using UnityEngine;
using TMPro;
using System.Collections;

public class BossDefeatBanner : MonoBehaviour
{
    public static BossDefeatBanner Instance;

    public CanvasGroup BannerCanvasGroup;
    public TextMeshProUGUI BannerText;

    public float FadeInDuration = 2.0f;  
    public float DisplayDuration = 4.0f; 
    public float FadeOutDuration = 2.0f; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (BannerCanvasGroup != null)
        {
            BannerCanvasGroup.alpha = 0f;
            BannerCanvasGroup.interactable = false;
            BannerCanvasGroup.blocksRaycasts = false;
        }
    }

    // Hàm 1: Gọi khi muốn ép đổi chữ bằng code
    public void ShowBanner(string textMessage)
    {
        if (BannerText != null) BannerText.text = textMessage;
        StopAllCoroutines();
        StartCoroutine(BannerRoutine());
    }

    // Hàm 2: [MỚI] Gọi khi muốn giữ nguyên chữ đã ghi trong TMP
    public void ShowBanner()
    {
        StopAllCoroutines();
        StartCoroutine(BannerRoutine());
    }

    private IEnumerator BannerRoutine()
    {
        float elapsed = 0f;
        while (elapsed < FadeInDuration)
        {
            elapsed += Time.deltaTime;
            BannerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / FadeInDuration);
            yield return null;
        }
        BannerCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(DisplayDuration);

        elapsed = 0f;
        while (elapsed < FadeOutDuration)
        {
            elapsed += Time.deltaTime;
            BannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / FadeOutDuration);
            yield return null;
        }
        BannerCanvasGroup.alpha = 0f;
    }
}