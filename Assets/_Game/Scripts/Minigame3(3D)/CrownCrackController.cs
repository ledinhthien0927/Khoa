using UnityEngine;

public class CrownCrackController : MonoBehaviour
{
    Material mat;

    void Awake()
    {
        mat = GetComponent<Renderer>().material;
    }

    public void UpdateCrack(float darkPercent, float lightPercent)
    {
        float crack = Mathf.Clamp01(darkPercent - lightPercent);
        mat.SetFloat("_CrackIntensity", crack);

        Color crackColor = Color.Lerp(
            new Color(1f, 0.8f, 0.3f),   // ánh sáng
            new Color(0.5f, 0f, 0.8f),   // hắc ám
            crack
        );

        mat.SetColor("_CrackColor", crackColor);
    }

    public void Purify()
    {
        mat.SetColor("_CrackColor", Color.white);
        mat.SetFloat("_CrackIntensity", 1f);
    }
}
