using UnityEngine;

public class MiniGame3Manager : MonoBehaviour
{
    public static MiniGame3Manager Instance;

    public float darkPower = 0;
    public float lightPower = 0;
    public float maxPower = 100f;
    public float darkIncreaseSpeed = 6f;

    public Transform crown;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        darkPower += darkIncreaseSpeed * Time.deltaTime;

        crown.localRotation = Quaternion.Euler(
            0,
            Mathf.Sin(Time.time * 6f) * darkPower * 0.04f,
            0
        );

        if (darkPower >= maxPower)
        {
            Debug.Log(" THUA – HẮC ÁM THẮNG");
            Time.timeScale = 0;
        }

        if (lightPower >= maxPower)
        {
            Debug.Log(" THẮNG – THANH TẨY HOÀN TẤT");
            Time.timeScale = 0;
        }
        float darkPercent = darkPower / maxPower;
        float lightPercent = lightPower / maxPower;

        energyBar.SetValue(lightPercent, darkPercent);

        crownVisual.UpdateVisual(darkPercent, lightPercent);


        if (energyBar.IsDarkWin())
        {
            Debug.Log(" HẮC ÁM THẮNG");
            Time.timeScale = 0;
        }

        if (energyBar.IsLightWin())
        {
            Debug.Log(" ÁNH SÁNG THẮNG");
            Time.timeScale = 0;
        }


        float danger = Mathf.Clamp01(darkPower / maxPower);
        cameraShake.Shake(danger * 0.08f);

         bgm.pitch = Mathf.Lerp(0.9f, 1.2f, darkPower / maxPower);


    }

    public void AddLight(float value)
    {
        lightPower += value;
        darkPower -= value * 0.6f;
    }

    public CrownVisual crownVisual;

    public EnergyClashBar energyBar;

    public CameraShake cameraShake;

    public AudioSource bgm;




}
