using UnityEngine;
using UnityEngine.AI;

public class VillagerAI : MonoBehaviour
{
    [Header("--- Settings ---")]
    [SerializeField] private float thoiGianVoGiaCu = 300f; // 5 phút
    
    private NavMeshAgent agent;
    private float boDem;
    private bool daCoNha = false;
    private bool dangBoDi = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        boDem = thoiGianVoGiaCu;

        // Báo cáo tăng dân số
        GameManager.Instance.ThayDoiDanSo(1);
    }

    private void OnDestroy()
    {
        // Báo cáo giảm dân số (nếu game chưa tắt)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ThayDoiDanSo(-1);
        }
    }

    private void Update()
    {
        if (dangBoDi)
        {
            if (!agent.pathPending && agent.remainingDistance < 1f) Destroy(gameObject);
            return;
        }

        if (!daCoNha)
        {
            boDem -= Time.deltaTime;
            if (boDem <= 0) BoDi();
        }
    }

    private void BoDi()
    {
        dangBoDi = true;
        Debug.Log($"{gameObject.name} bỏ đi vì vô gia cư!");
        agent.SetDestination(new Vector3(50, 0, 50)); // Đi ra biên giới
    }

    public void GanNha()
    {
        daCoNha = true;
        boDem = thoiGianVoGiaCu; // Reset tâm trạng
    }
}