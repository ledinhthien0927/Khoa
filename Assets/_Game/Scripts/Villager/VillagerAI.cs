using UnityEngine;
using UnityEngine.AI; // Bắt buộc để dùng NavMeshAgent

public class VillagerAI : MonoBehaviour
{
    [Header("--- Cài Đặt Chung ---")]
    [Tooltip("Tốc độ di chuyển")]
    [SerializeField] private float tocDoDiChuyen = 3.5f;

    [Header("--- Cơ Chế Vô Gia Cư ---")]
    [Tooltip("Thời gian chịu đựng không có nhà (Giây). 5 phút = 300s")]
    [SerializeField] private float thoiGianVoGiaCu = 300f; 
    
    [Tooltip("Đã có nhà chưa?")]
    public bool daCoNha = false;

    // Biến nội bộ
    private float boDemThoiGian;
    private NavMeshAgent agent;
    private bool dangBoDi = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = tocDoDiChuyen;
        
        // Reset bộ đếm khi vừa sinh ra
        boDemThoiGian = thoiGianVoGiaCu;
    }

    private void Update()
    {
        // Nếu đang bỏ đi thì không tính toán gì nữa
        if (dangBoDi) 
        {
            KiemTraDaRaKhoiDaoChua();
            return;
        }

        // Nếu chưa có nhà thì bắt đầu đếm ngược
        if (!daCoNha)
        {
            XuLyDemNguoc();
        }
    }

    private void XuLyDemNguoc()
    {
        boDemThoiGian -= Time.deltaTime;

        if (boDemThoiGian <= 0)
        {
            QuyetDinhBoDi();
        }
    }

    private void QuyetDinhBoDi()
    {
        dangBoDi = true;
        Debug.Log($"<color=red>Dân làng {gameObject.name} chán nản và quyết định bỏ đảo đi!</color>");

        // Tìm một điểm xa tít mù khơi để đi tới (Ví dụ: Tọa độ 50, 0, 50)
        // Sau này sẽ thay bằng tọa độ bến cảng
        Vector3 diemBienGioi = new Vector3(50, 0, 50); 
        agent.SetDestination(diemBienGioi);
    }

    private void KiemTraDaRaKhoiDaoChua()
    {
        // Nếu đã đến gần điểm biến mất (còn cách 1m)
        if (!agent.pathPending && agent.remainingDistance <= 1f)
        {
            Debug.Log($"<color=red>Dân làng {gameObject.name} đã rời khỏi đảo.</color>");
            Destroy(gameObject); // Xóa khỏi game
            
            // TODO: Trừ chỉ số dân số trong GameManager (Sẽ làm sau)
        }
    }

    // Hàm này để các hệ thống khác (Xây dựng) gọi vào
    public void GanNhaChoDan()
    {
        daCoNha = true;
        boDemThoiGian = thoiGianVoGiaCu; // Reset lại tâm trạng
        Debug.Log($"<color=green>Dân làng {gameObject.name} đã có nhà! Rất vui vẻ.</color>");
    }
}