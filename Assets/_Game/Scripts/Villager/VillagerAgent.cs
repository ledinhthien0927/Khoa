using UnityEngine;
using UnityEngine.AI;

public class VillagerAgent : MonoBehaviour
{
    // Chỉ chứa các component cần thiết để di chuyển
    [HideInInspector] public NavMeshAgent navMeshAgent;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Khi sinh ra, tự đăng ký bản thân với Manager
        VillagerManager.Instance.DangKyDanLang(this);
    }

    private void OnDestroy()
    {
        // Khi chết, tự báo cáo xóa tên khỏi danh sách
        if (VillagerManager.Instance != null)
        {
            VillagerManager.Instance.HuyDangKyDanLang(this);
        }
    }

    // Hàm hành động: Chỉ thực hiện việc di chuyển
    public void DiChuyenDen(Vector3 viTri)
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(viTri);
        }
    }
}