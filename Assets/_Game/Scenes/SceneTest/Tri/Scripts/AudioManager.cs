using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource audioSource;
    public AudioClip sighClip;

    void Awake() => Instance = this;

    public void PlaySigh()
    {
        audioSource.PlayOneShot(sighClip);
    }
}
