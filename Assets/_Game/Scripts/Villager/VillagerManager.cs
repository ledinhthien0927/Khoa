using UnityEngine;
using System.Collections.Generic;

// Class chứa dữ liệu trạng thái của từng dân (Không phải MonoBehaviour)
[System.Serializable]
public class VillagerData
{
    public VillagerAgent agent; // Tham chiếu đến con dân thực thể
    public float thoiGianVoGiaCu; // Thời gian đếm ngược của con này
    public bool daCoNha;
    public bool dangBoDi;

    public VillagerData(VillagerAgent _agent, float _thoiGian)
    {
        agent = _agent;
        thoiGianVoGiaCu = _thoiGian;
        daCoNha = false;
        dangBoDi = false;
    }
}

public class VillagerManager : MonoBehaviour
{
    public static VillagerManager Instance { get; private set; }

    [Header("--- Cấu Hình Chung ---")]
    [SerializeField] private float thoiGianChiudung = 300f; // 5 phút mặc định

    // Danh sách quản lý tất cả dữ liệu dân làng
    // Chúng ta dùng List để duyệt vòng lặp Update cho nhanh
    private List<VillagerData> danhSachDan = new List<VillagerData>();
    
    // Danh sách chờ xóa (để tránh lỗi khi xóa trong vòng lặp)
    private List<VillagerData> danhSachCanXoa = new List<VillagerData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- CHỈ CÓ 1 HÀM UPDATE DUY NHẤT CHO CẢ LÀNG ---
    private void Update()
    {
        // Duyệt qua tất cả dân làng đang quản lý
        for (int i = 0; i < danhSachDan.Count; i++)
        {
            VillagerData data = danhSachDan[i];
            
            // Nếu con này null (bị xóa bất ngờ) hoặc đang bỏ đi thì bỏ qua
            if (data.agent == null || data.dangBoDi) continue;

            // Logic Vô gia cư
            if (!data.daCoNha)
            {
                data.thoiGianVoGiaCu -= Time.deltaTime;

                if (data.thoiGianVoGiaCu <= 0)
                {
                    XuLyDanBoDi(data);
                }
            }
        }
    }

    // --- CÁC HÀM QUẢN LÝ ---

    public void DangKyDanLang(VillagerAgent agent)
    {
        // Tạo data mới cho con dân này
        VillagerData dataMoi = new VillagerData(agent, thoiGianChiudung);
        danhSachDan.Add(dataMoi);

        // Báo cáo lên GameManager (MVC)
        GameManager.Instance.ThayDoiDanSo(1);
    }

    public void HuyDangKyDanLang(VillagerAgent agent)
    {
        // Tìm data của agent này để xóa
        VillagerData dataCanXoa = danhSachDan.Find(x => x.agent == agent);
        
        if (dataCanXoa != null)
        {
            danhSachDan.Remove(dataCanXoa);
            
            // Báo cáo giảm dân số
            GameManager.Instance.ThayDoiDanSo(-1);
        }
    }

    private void XuLyDanBoDi(VillagerData data)
    {
        data.dangBoDi = true;
        Debug.Log($"Dân {data.agent.name} bỏ đi!");

        // Ra lệnh cho Agent di chuyển
        data.agent.DiChuyenDen(new Vector3(50, 0, 50));

        // Logic tự hủy sau khi đi xa (Dùng Coroutine hoặc check khoảng cách)
        StartCoroutine(IE_KiemTraBienMat(data.agent));
    }

    // Coroutine kiểm tra riêng lẻ (Nhẹ nhàng, không ảnh hưởng Update chính)
    private System.Collections.IEnumerator IE_KiemTraBienMat(VillagerAgent agent)
    {
        while (agent != null && agent.navMeshAgent.remainingDistance > 1f)
        {
            yield return new WaitForSeconds(1f); // Check mỗi 1 giây (Tiết kiệm hơn Update)
        }

        if (agent != null) Destroy(agent.gameObject);
    }
}