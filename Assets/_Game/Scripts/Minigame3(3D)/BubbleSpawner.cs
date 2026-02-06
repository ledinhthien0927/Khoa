using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    public GameObject bubblePrefab;
    public float spawnRate = 1f;
    public float rangeX = 2.5f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnBubble), 1f, spawnRate);
    }

    void SpawnBubble()
    {
        Vector3 pos = transform.position +
            new Vector3(Random.Range(-rangeX, rangeX), 0, 0);

        Instantiate(bubblePrefab, pos, Quaternion.identity);
    }
}
