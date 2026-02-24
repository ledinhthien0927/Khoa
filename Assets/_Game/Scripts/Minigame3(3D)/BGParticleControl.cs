using UnityEngine;

public class BGParticleControl : MonoBehaviour
{
    [Header("Refs")]
    public ParticleSystem ps;
    public MiniGame3Manager manager;

    [Header("Base Control (Light/Dark)")]
    public float minRate = 10f;
    public float maxRate = 35f;
    public float minSpeed = 0.1f;
    public float maxSpeed = 0.4f;

    [Header("Shockwave Impact")]
    [Range(0f, 1f)] public float shockImpact;
    public float shockSimSpeed = 3f;
    public float normalSimSpeed = 0.6f;
    public float shockFadeSpeed = 2f;

    void Update()
    {
        float t = manager.lightPower / manager.maxPower;

        // 🌗 Emission theo thế lực
        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(minRate, maxRate, t);

        // 🌪 Speed theo thế lực
        var main = ps.main;
        main.startSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

        // 💥 Shockwave tác động simulation
        shockImpact = Mathf.MoveTowards(shockImpact, 0f, Time.deltaTime * shockFadeSpeed);
        main.simulationSpeed = Mathf.Lerp(normalSimSpeed, shockSimSpeed, shockImpact);
    }

    // 🔥 GỌI TỪ CrownVisual
    public void OnShockwave(float strength)
    {
        shockImpact = Mathf.Clamp01(strength);
    }
}