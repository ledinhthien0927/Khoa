using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TargetController : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.1f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private bool isActive = true;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        // Đảm bảo có Button
        Button button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        if (!isActive) return;

        // Hiệu ứng pulse
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        rectTransform.localScale = originalScale * (1f + pulse);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }

    public void OnClick()
    {
        if (!isActive) return;
        isActive = false;

        // GỌI ĐÚNG GAME MANAGER
        if (MiniGame3GameManager.Instance != null)
        {
            MiniGame3GameManager.Instance.OnTargetClicked();
        }

        // Hiệu ứng click
        StartCoroutine(ClickEffect());
    }

    private IEnumerator ClickEffect()
    {
        float duration = 0.25f;
        float elapsed = 0f;

        Image img = GetComponent<Image>();
        Color startColor = img != null ? img.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rectTransform.localScale = Vector3.Lerp(
                originalScale,
                originalScale * 1.5f,
                t
            );

            if (img != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                img.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
