using UnityEngine;

public class EnergyClashFX : MonoBehaviour
{
    public ParticleSystem ps;
    public EnergyClashBar bar;

    void Update()
    {
        float power = Mathf.Abs(bar.GetValue());

        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(10, 80, power);

        var main = ps.main;
        main.startSize = Mathf.Lerp(0.05f, 0.15f, power);
    }
}
