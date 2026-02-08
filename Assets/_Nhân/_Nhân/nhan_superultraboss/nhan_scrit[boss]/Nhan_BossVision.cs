using UnityEngine;
using System.Collections; // <--- Dòng này quan trọng, bị thiếu ở bản cũ

public class Nhan_BossVision : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 15f;    // Tầm nhìn xa
    [Range(0, 360)]
    public float viewAngle = 110f;    // Góc nhìn (110 độ)

    [Header("Masks")]
    public LayerMask targetMask;      // Chọn layer "Player"
    public LayerMask obstacleMask;    // Chọn layer "Obstacle" / "Default" (Tường, đất)

    [HideInInspector] public Transform playerRef;
    [HideInInspector] public bool canSeePlayer = false;
    [HideInInspector] public Vector3 lastKnownPosition; // Vị trí cuối cùng nhìn thấy Player

    void Start()
    {
        // Tự tìm Player bằng Tag
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerRef = p.transform;
        
        // Mặc định vị trí cuối cùng là chỗ Boss đứng
        lastKnownPosition = transform.position;
        
        // Chạy check tầm nhìn mỗi 0.2s (để tối ưu game, không check mỗi khung hình)
        StartCoroutine(FOVRoutine());
    }

    IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);
        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    void FieldOfViewCheck()
    {
        if (!playerRef) return;

        // 1. Check khoảng cách
        float distToTarget = Vector3.Distance(transform.position, playerRef.position);
        if (distToTarget > viewRadius)
        {
            canSeePlayer = false;
            return;
        }

        // 2. Check góc nhìn (Boss có đang quay mặt về phía Player không?)
        Vector3 dirToTarget = (playerRef.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
        {
            // 3. Check vật cản (Raycast)
            // Bắn tia từ mắt Boss (cộng thêm Vector3.up để tầm mắt cao hơn chân) đến Player
            if (!Physics.Raycast(transform.position + Vector3.up, dirToTarget, distToTarget, obstacleMask))
            {
                // KHÔNG vướng tường -> Thấy Player
                canSeePlayer = true;
                lastKnownPosition = playerRef.position; // Cập nhật vị trí cuối cùng thấy
            }
            else
            {
                // Vướng tường -> Mất dấu
                canSeePlayer = false;
            }
        }
        else
        {
            canSeePlayer = false; // Player đứng sau lưng Boss
        }
    }
    
    // Vẽ tầm nhìn trong Editor để dễ chỉnh (Gizmos)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        
        if(canSeePlayer)
        {
            Gizmos.color = Color.red;
            if(playerRef) Gizmos.DrawLine(transform.position + Vector3.up, playerRef.position + Vector3.up);
        }
    }
}