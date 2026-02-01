using UnityEngine;
using UnityEngine.AI; // Bắt buộc phải có để dùng NavMesh

[RequireComponent(typeof(NavMeshAgent))]
public class FishWander : MonoBehaviour
{
    public float wanderRadius = 5f; // Bán kính vùng cá sẽ bơi
    public float wanderTimer = 3f;  // Thời gian giữa mỗi lần đổi hướng

    private Transform target;
    private NavMeshAgent agent;
    private float timer;

    // Sử dụng OnEnable để khởi tạo
    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Khi đếm đủ thời gian, tìm điểm mới để bơi tới
        if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    // Hàm tìm điểm ngẫu nhiên trên NavMesh
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        // Tạo một điểm ngẫu nhiên trong khối cầu ảo
        Vector3 randDirection = Random.insideUnitSphere * dist;

        // Cộng điểm đó vào vị trí hiện tại
        randDirection += origin;

        // Tìm vị trí hợp lệ gần nhất trên NavMesh
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
}