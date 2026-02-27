using UnityEngine;

public class EnergyClashBar : MonoBehaviour
{
    public RectTransform centerPoint;
    public RectTransform lightFill;
    public RectTransform darkFill;


    [Header("Wave Effect")]
    public float waveHeight = 4f;
    public float waveSpeed = 8f;


    public float GetValue()
    {
        return value;
    }


    public float barWidth = 800f;

    float value = 0f; // -1 → dark, +1 → light

    public void SetValue(float lightPercent, float darkPercent)
    {
        float target = lightPercent - darkPercent;
        target = Mathf.Clamp(target, -1f, 1f);

        value = Mathf.Lerp(value, target, Time.deltaTime * 6f);

        UpdateVisual();
    }

    void UpdateVisual()
    {
        float half = barWidth * 0.5f;

        // vị trí centerpoint
        float centerX = value * half;
        centerPoint.anchoredPosition =
            new Vector2(centerX, centerPoint.anchoredPosition.y);

        // chiều dài ánh sáng (từ trái → center)
        float lightWidth = Mathf.Clamp(centerX + half, 0, barWidth);
        lightFill.sizeDelta =
            new Vector2(lightWidth, lightFill.sizeDelta.y);

        // chiều dài bóng tối (từ phải → center)
        float darkWidth = Mathf.Clamp(half - centerX, 0, barWidth);
        darkFill.sizeDelta =
            new Vector2(darkWidth, darkFill.sizeDelta.y);


        float glow = Mathf.Abs(value);
        centerPoint.localScale =
        Vector3.one * (1 + glow * 0.25f);

        float wave = Mathf.Sin(Time.time * waveSpeed) * waveHeight;

        // Light lượn lên
        lightFill.sizeDelta = new Vector2(
            lightFill.sizeDelta.x,
            80 + wave
        );

        // Dark lượn xuống (đối nghịch cho đẹp)
        darkFill.sizeDelta = new Vector2(
            darkFill.sizeDelta.x,
            80 - wave
        );


    }

    public bool IsLightWin() => value >= 1f;
    public bool IsDarkWin() => value <= -1f;
}
