using UnityEngine;

public class MiniGame3Manager : MonoBehaviour
{
    public static MiniGame3Manager Instance;

    public float darkPower = 50f;
    public float lightPower = 50f;
    public float maxPower = 100f;
    public float darkIncreaseSpeed = 6f;

    public Transform crown;

    public CrownVisual crownVisual;
    public EnergyClashBar energyBar;
    public CameraShake cameraShake;
    public AudioSource bgm;
    public BGEnergyFX BG;

    private bool isGameEnded = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isGameEnded) return;

        // DARK LUÔN TĂNG
        float delta = darkIncreaseSpeed * Time.deltaTime;
        darkPower += delta;
        lightPower -= delta;

        darkPower = Mathf.Clamp(darkPower, 0f, maxPower);
        lightPower = Mathf.Clamp(lightPower, 0f, maxPower);

        // CHECK WIN
        if (darkPower >= maxPower)
        {
            EndGame(" THUA – HẮC ÁM THẮNG");
            return;
        }

        if (lightPower >= maxPower)
        {
            EndGame(" THẮNG – THANH TẨY HOÀN TẤT");
            return;
        }

        // VISUAL
        crown.localRotation = Quaternion.Euler(
            0,
            Mathf.Sin(Time.time * 6f) * darkPower * 0.04f,
            0
        );

        float darkPercent = darkPower / maxPower;
        float lightPercent = lightPower / maxPower;

        energyBar.SetValue(lightPercent, darkPercent);
        crownVisual.UpdateVisual(darkPercent, lightPercent);

        cameraShake.Shake(darkPercent * 0.08f);
        bgm.pitch = Mathf.Lerp(0.9f, 1.2f, darkPercent);
    }

    public void AddLight(float value)
    {
        if (isGameEnded) return;

        lightPower += value;
        darkPower -= value;

        lightPower = Mathf.Clamp(lightPower, 0f, maxPower);
        darkPower = Mathf.Clamp(darkPower, 0f, maxPower);

        if (lightPower >= maxPower)
        {
            EndGame(" THẮNG – THANH TẨY HOÀN TẤT");
        }
    }

    void EndGame(string message)
    {
        if (isGameEnded) return;

        isGameEnded = true;
        Debug.Log(message);
        Time.timeScale = 0f;
    }
}