using UnityEngine;

public class nhan_AlchemyBarrel : MonoBehaviour
{
    [Header("Cài đặt")]
    public GameObject nhan_puddlePrefab; // Prefab vũng nước/lửa
    public string nhan_arrowTag = "Arrow";
    public GameObject nhan_explosionVFX;

    private void OnCollisionEnter(Collision collision)
    {
        // Check va chạm với mũi tên
        if (collision.gameObject.CompareTag(nhan_arrowTag) || collision.gameObject.GetComponent<ArrowProjectile>())
        {
            Explode();
        }
    }

    void Explode()
    {
        // 1. Sinh ra vũng hiệu ứng
        if (nhan_puddlePrefab != null)
        {
            // Raycast xuống đất để đặt vũng nước nằm bẹp xuống sàn
            RaycastHit hit;
            Vector3 spawnPos = transform.position;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 5f))
            {
                spawnPos = hit.point + Vector3.up * 0.02f; 
            }

            Instantiate(nhan_puddlePrefab, spawnPos, Quaternion.identity);
        }

        // 2. VFX nổ
        if (nhan_explosionVFX != null)
        {
            Instantiate(nhan_explosionVFX, transform.position, transform.rotation);
        }

        // 3. Hủy thùng
        Destroy(gameObject);
    }
}