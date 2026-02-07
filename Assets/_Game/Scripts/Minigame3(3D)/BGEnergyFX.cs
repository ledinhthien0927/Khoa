using UnityEngine;

public class BGEnergyFX : MonoBehaviour
{
    public Material bgMat;
    public MiniGame3Manager manager;

    public Color darkColor = new Color(0.15f, 0.05f, 0.25f);
    public Color lightColor = new Color(0.9f, 0.8f, 0.5f);

    public float pulseSpeed = 1.2f;

    void Update()
    {
        float t = manager.lightPower / manager.maxPower;

        float wave = Mathf.Sin(Time.time * pulseSpeed) * 0.05f;
        Color finalColor = Color.Lerp(darkColor, lightColor, t);

        bgMat.color = finalColor + finalColor * wave;
    }
}
