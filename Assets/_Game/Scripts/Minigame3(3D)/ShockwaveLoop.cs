using UnityEngine;
using System.Collections;

public class ShockwaveLoop : MonoBehaviour
{
    public ParticleSystem shockwave;
    public float interval = 1.5f;

    void Start()
    {
        StartCoroutine(LoopShockwave());
    }

    IEnumerator LoopShockwave()
    {
        while (true)
        {
            shockwave.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            shockwave.Play();
            yield return new WaitForSeconds(interval);
        }
    }
}