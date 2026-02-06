using UnityEngine;

public class CameraShake : MonoBehaviour
{
    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    public void Shake(float intensity)
    {
        transform.localPosition =
            startPos + Random.insideUnitSphere * intensity;
    }

    public void ResetPos()
    {
        transform.localPosition = startPos;
    }
}
