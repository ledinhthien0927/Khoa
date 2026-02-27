using UnityEngine;
using System.Collections;

public class NPCFishingAI : MonoBehaviour
{
    public Animator animator;
    public Transform bobber;
    public GameObject fishPrefab;
    public Transform hookPoint;
    public Transform basketPoint;


    public float minWaitTime = 15f;
    public float maxWaitTime = 30f;

    private Vector3 bobberStartPos;

    void Start()
    {
        bobberStartPos = bobber.position;
        StartCoroutine(FishingLoop());
    }

    IEnumerator FishingLoop()
    {
        while (true)
        {
            // 1. Chờ 15–30 giây
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // 2. Phao nhún xuống
            yield return StartCoroutine(BobberDip());

            // 3. Random kết quả
            float chance = Random.value;

            if (chance <= 0.8f)
            {
                animator.SetTrigger("PullEmpty");
                Debug.Log("Không có gì...");
            }
            else
            {
                animator.SetTrigger("PullFish");
                SpawnFish();
                Debug.Log("Bắt được cá!");
            }

            yield return new WaitForSeconds(3f);
        }
    }
    void SpawnFish()
    {
        GameObject fish = Instantiate(fishPrefab, hookPoint.position, Quaternion.identity);
        fish.transform.SetParent(hookPoint);

        StartCoroutine(MoveFishToBasket(fish));
    }

    IEnumerator MoveFishToBasket(GameObject fish)
    {
        yield return new WaitForSeconds(2f);

        fish.transform.SetParent(null);

        float t = 0;
        Vector3 startPos = fish.transform.position;

        while (t < 1f)
        {
            fish.transform.position = Vector3.Lerp(startPos, basketPoint.position, t);
            t += Time.deltaTime;
            yield return null;
        }

        fish.transform.position = basketPoint.position;
    }
    IEnumerator BobberDip()
    {
        Vector3 downPos = bobberStartPos + Vector3.down * 0.3f;

        float t = 0;
        while (t < 0.3f)
        {
            bobber.position = Vector3.Lerp(bobberStartPos, downPos, t / 0.3f);
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        t = 0;
        while (t < 0.3f)
        {
            bobber.position = Vector3.Lerp(downPos, bobberStartPos, t / 0.3f);
            t += Time.deltaTime;
            yield return null;
        }

        bobber.position = bobberStartPos;
    }
}