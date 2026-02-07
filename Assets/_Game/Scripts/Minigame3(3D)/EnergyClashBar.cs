using UnityEngine;

public class EnergyClashBar : MonoBehaviour
{
    [Header("UI")]
    public RectTransform darkFill;
    public RectTransform lightFill;

    [Header("Settings")]
    public float maxValue = 100f;

    // Giá trị đối kháng:
    // -100 = Dark thắng
    // 0    = Cân bằng
    // +100 = Light thắng
    [Header("Runtime")]
    public float balance = 0f;

    float halfWidth;

    void Start()
    {
        halfWidth = ((RectTransform)transform).rect.width / 2f;
    }

    void Update()
    {
        UpdateVisual();
    }

    public void AddDark(float value)
    {
        balance -= value;
        balance = Mathf.Clamp(balance, -maxValue, maxValue);
    }

    public void AddLight(float value)
    {
        balance += value;
        balance = Mathf.Clamp(balance, -maxValue, maxValue);
    }

    void UpdateVisual()
    {
        if (balance < 0)
        {
            float percent = Mathf.Abs(balance) / maxValue;
            darkFill.sizeDelta = new Vector2(halfWidth * percent, darkFill.sizeDelta.y);
            lightFill.sizeDelta = new Vector2(0, lightFill.sizeDelta.y);
        }
        else
        {
            float percent = balance / maxValue;
            lightFill.sizeDelta = new Vector2(halfWidth * percent, lightFill.sizeDelta.y);
            darkFill.sizeDelta = new Vector2(0, darkFill.sizeDelta.y);
        }
    }

    public bool IsDarkWin()
    {
        return balance <= -maxValue;
    }

    public bool IsLightWin()
    {
        return balance >= maxValue;
    }
}
