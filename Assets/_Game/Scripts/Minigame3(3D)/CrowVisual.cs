using UnityEngine;

public class CrownVisual : MonoBehaviour
{
    [Header("Colors")]
    public Color darkColor = new Color(0.25f, 0f, 0.4f);
    public Color lightColor = new Color(1f, 0.9f, 0.5f);

    [Header("Emission")]
    public float maxEmission = 2.5f;
    public float pulseSpeed = 3f;

    [Header("Aura (Dark mạnh hơn)")]
    public ParticleSystem auraParticle;
    public float maxAuraRate = 30f;

    [Header("Orbit (2 vòng ngược chiều)")]
    public ParticleSystem orbitLeft;
    public ParticleSystem orbitRight;
    public float maxOrbitSpeed = 3f;

    [Header("Shockwave")]
    public ParticleSystem shockwave;
    [Range(0f, 1f)] public float shockThreshold = 0.8f;
    public float shockCooldown = 1.2f;

    float lastShockTime = -999f;

    Renderer rend;
    Material crownMat;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        crownMat = rend.materials[1];
    }

    public void UpdateVisual(float darkPercent, float lightPercent)
    {
        float balance = lightPercent - darkPercent;
        float t = Mathf.InverseLerp(-1f, 1f, balance);

        float lightDom = Mathf.Clamp01(balance);
        float darkDom = Mathf.Clamp01(-balance);

        // 🎨 Color
        Color bodyColor = Color.Lerp(darkColor, lightColor, t);
        crownMat.color = bodyColor;

        // 🌟 Emission
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        Color emissionColor = Color.Lerp(darkColor, lightColor, t);
        crownMat.SetColor(
            "_EmissionColor",
            emissionColor * (lightDom + darkDom) * pulse * maxEmission
        );

        // ✨ Aura
        if (auraParticle != null)
        {
            var em = auraParticle.emission;
            em.rateOverTime = Mathf.Lerp(5f, maxAuraRate, darkDom);

            var main = auraParticle.main;
            main.startColor = Color.Lerp(lightColor, darkColor, darkDom);
            main.startSize = Mathf.Lerp(0.05f, 0.18f, darkDom);
        }

        // 🌀 Orbit trái
        if (orbitLeft != null)
        {
            var vel = orbitLeft.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalY = Mathf.Lerp(0.2f, maxOrbitSpeed, lightDom);
        }

        // 🌀 Orbit phải
        if (orbitRight != null)
        {
            var vel = orbitRight.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalY = -Mathf.Lerp(0.2f, maxOrbitSpeed, lightDom);
        }

        // 💥 Shockwave trigger
        if (Time.time - lastShockTime > shockCooldown)
        {
            if (darkPercent >= shockThreshold || lightPercent >= shockThreshold)
            {
                EmitShockwave();
                lastShockTime = Time.time;
            }
        }
    }
    [SerializeField] BGEnergyFX bgFX;
    [SerializeField] BGParticleControl bgParticle;
    void EmitShockwave()
   
    {
        shockwave.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        shockwave.Play();

        if (bgParticle != null)
            bgParticle.OnShockwave(1f);
    }
}