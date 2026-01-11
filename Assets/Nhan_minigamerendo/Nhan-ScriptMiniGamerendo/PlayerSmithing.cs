using System.Collections;
using UnityEngine;

public class PlayerSmithing : MonoBehaviour
{
    public static PlayerSmithing Instance;

    public Animator animator;
    public float delayTime = 0.4f; 
    
    [Header("Âm Thanh")]
    public AudioSource audioSource; 
    public AudioClip hitSound;      

    void Awake()
    {
        Instance = this;
    }

    public void SmashAt(Vector3 targetPos, Vector3 normal, int score)
    {
        // 1. Xoay người
        Vector3 direction = targetPos - transform.position;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 2. Animation
        if(animator != null) animator.SetTrigger("Smash");

        // 3. Đếm giờ & Gọi Manager
        StartCoroutine(WaitAndHit(targetPos, normal, score));
    }

    IEnumerator WaitAndHit(Vector3 pos, Vector3 normal, int score)
    {
        yield return new WaitForSeconds(delayTime);

        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // QUAN TRỌNG: Gọi Manager để quyết định xem có spawn tiếp hay không
        if (SmithingManager.Instance != null)
        {
            SmithingManager.Instance.ProcessHit(pos, normal, score);
        }
    }
}