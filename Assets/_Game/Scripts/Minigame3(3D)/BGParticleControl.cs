using UnityEngine;

public class BGParticleControl : MonoBehaviour
{
    public ParticleSystem ps;
    public MiniGame3Manager manager;

    void Update()
    {
        float t = manager.lightPower / manager.maxPower;

        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(10, 35, t);

        var main = ps.main;
        main.startSpeed = Mathf.Lerp(0.1f, 0.4f, t);
    }
}
