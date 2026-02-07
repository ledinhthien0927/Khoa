using UnityEngine;

public class LightBubble : MonoBehaviour
{
    public float speed = 2.2f;
    public float lifeTime = 5f;
    public int lightValue = 10;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        transform.position += Vector3.right *
            Mathf.Sin(Time.time * 3f) * 0.015f;
    }

    void OnMouseDown()
    {
        MiniGame3Manager.Instance.AddLight(lightValue);
        Destroy(gameObject);
    }
}
