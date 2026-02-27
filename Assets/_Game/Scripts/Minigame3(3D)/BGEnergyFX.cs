using UnityEngine;

public class BGEnergyFX : MonoBehaviour
{
    [Header("Refs")]
    public Renderer bgRenderer;
    public MiniGame3Manager manager;

    [Header("Colors")]
    public Color darkColor = new Color(0.15f, 0.05f, 0.25f);
    public Color lightColor = new Color(0.9f, 0.8f, 0.5f);
    public Color shockColor = new Color(0.6f, 0.1f, 0.6f);

    [Header("Pulse")]
    public float pulseSpeed = 1.2f;
    public float pulseStrength = 0.05f;

    [Header("Shockwave Impact")]
    [Range(0f, 1f)] public float shockImpact;
    public float shockFadeSpeed = 2f;

    void Update()
    {
        // 🌗 Cân bằng light / dark
        float t = manager.lightPower / manager.maxPower;

        // 🌊 Pulse nền
        float wave = Mathf.Sin(Time.time * pulseSpeed) * pulseStrength;

        // 🎨 Màu cơ bản theo thế lực
        Color baseColor = Color.Lerp(darkColor, lightColor, t);

        // 💥 Shockwave tác động môi trường
        shockImpact = Mathf.MoveTowards(shockImpact, 0f, Time.deltaTime * shockFadeSpeed);
        Color shockTint = Color.Lerp(baseColor, shockColor, shockImpact);

        // 🎯 Final color
        bgRenderer.material.color = shockTint + shockTint * wave;
    }

    // 🔥 GỌI TỪ CrownVisual khi shockwave nổ
    public void OnShockwave(float strength)
    {
        shockImpact = Mathf.Clamp01(strength);
    }
}