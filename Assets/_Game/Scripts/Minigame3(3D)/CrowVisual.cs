using UnityEngine;

public class CrownVisual : MonoBehaviour
{
    [Header("Colors")]
    public Color darkColor = new Color(0.25f, 0f, 0.4f);
    public Color neutralColor = Color.gray;
    public Color lightColor = new Color(1f, 0.9f, 0.5f);

    [Header("Emission")]
    public float maxEmission = 2.5f;

    Material mat;

    void Awake()
    {
        mat = GetComponent<Renderer>().material;
    }

    public void UpdateVisual(float darkPercent, float lightPercent)
    {
        // So sánh ưu thế
        float balance = lightPercent - darkPercent;
        float t = Mathf.InverseLerp(-1f, 1f, balance);

        // Màu thân vương miện
        Color bodyColor = Color.Lerp(darkColor, lightColor, t);
        mat.color = bodyColor;

        // Emission (chỉ mạnh khi ánh sáng áp đảo)
        float emissionStrength = Mathf.Clamp01(lightPercent - darkPercent);
        mat.SetColor(
            "_EmissionColor",
            lightColor * emissionStrength * maxEmission
        );
    }
}
