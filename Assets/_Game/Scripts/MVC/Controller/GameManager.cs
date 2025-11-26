using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Bắt buộc để dùng lệnh Load Scene

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- MVC Components ---")]
    public GameModel model;       // Chứa dữ liệu (Số dân, Thời gian...)
    [SerializeField] private GameView view; // Điều khiển UI và Ánh sáng

    [Header("--- Events ---")]
    [HideInInspector] public UnityEvent OnBatDauWave;
    [HideInInspector] public UnityEvent OnKetThucWave;

    private void Awake()
    {
        // --- SINGLETON PATTERN (Phiên bản Reset Scene) ---
        // Vì ta sẽ reload lại Scene khi Game Over, nên ta không dùng DontDestroyOnLoad
        // Mỗi lần load màn mới, một GameManager mới sẽ được sinh ra.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Khởi tạo model nếu chưa có (để tránh lỗi Null)
        if (model == null) model = new GameModel();
    }

    private void Start()
    {
        KhoiTaoGame();
    }

    // Hàm setup mọi thứ về trạng thái ban đầu
    private void KhoiTaoGame()
    {
        // 1. Reset dữ liệu
        model.thoiGianDemNguoc = model.thoiGianGiuaCacDot;
        model.soDotDaVao = 0;
        model.daThuaCuoc = false;
        model.dangTrongTranChien = false;

        // Tài nguyên
        model.go = 100;
        model.vang = 50;
        
        // 2. Đảm bảo thời gian chạy bình thường (đề phòng trước đó bị pause)
        Time.timeScale = 1; 

        // 3. Cập nhật View lần đầu
        if (view != null)
        {
            view.HienThiDanSo(model.danSoHienTai, model.danSoToiDa);
            view.HienThiThoiGian(model.thoiGianDemNguoc, false);
            view.HienThiGameOver(false); // Ẩn bảng thua
            view.CapNhatAnhSang(0);      // Trời sáng
            view.HienThiTaiNguyen(model.go, model.vang); // tài nguyên
        }
    }

    private void Update()
    {
        // Nếu đã thua hoặc chưa setup xong thì không chạy logic
        if (model.daThuaCuoc) return;

        XuLyThoiGian();
    }

    public void CapNhatGioiHanDanSo(int soLuongThem)
    {
        model.danSoToiDa += soLuongThem;
        
        // Cập nhật View
        if (view != null)
        {
            view.HienThiDanSo(model.danSoHienTai, model.danSoToiDa);
        }
    }

    public bool ThayDoiTaiNguyen(int luongGo, int luongVang)
    {
        // Kiểm tra nếu là hành động tiêu xài (số âm)
        if (model.go + luongGo < 0 || model.vang + luongVang < 0)
        {
            Debug.Log("Không đủ tài nguyên!");
            return false; // Giao dịch thất bại
        }

        model.go += luongGo;
        model.vang += luongVang;

        // Cập nhật View
        view.HienThiTaiNguyen(model.go, model.vang);
        return true; // Giao dịch thành công
    }

    private void XuLyThoiGian()
    {
        // Nếu đang trong trận chiến thì không đếm ngược nữa
        if (model.dangTrongTranChien) return;

        model.thoiGianDemNguoc -= Time.deltaTime;

        // Tính phần trăm thời gian (1.0 -> 0.0) để xoay mặt trời
        float phanTram = model.thoiGianDemNguoc / model.thoiGianGiuaCacDot;
        
        // Cập nhật View
        view.HienThiThoiGian(model.thoiGianDemNguoc, false);
        view.CapNhatAnhSang(phanTram);

        // Hết giờ -> Bắt đầu Wave
        if (model.thoiGianDemNguoc <= 0)
        {
            BatDauWave();
        }
    }

    private void BatDauWave()
    {
        model.dangTrongTranChien = true;
        model.soDotDaVao++;
        
        // Cập nhật View
        view.HienThiThoiGian(0, true);
        view.BatTatCanhBao(true); // Nhấp nháy panel đỏ
        
        Debug.Log($"<color=red>Wave {model.soDotDaVao} Start!</color>");
        OnBatDauWave?.Invoke();
    }

    // ---------------------------------------------------------
    // CÁC HÀM PUBLIC (Gọi từ bên ngoài)
    // ---------------------------------------------------------

    // VillagerAI gọi hàm này khi sinh ra hoặc chết đi
    public void ThayDoiDanSo(int soLuong)
    {
        // Nếu game đã over thì không tính toán nữa
        if (model.daThuaCuoc) return;

        model.danSoHienTai += soLuong;
        
        // Tránh số âm (logic an toàn)
        if (model.danSoHienTai < 0) model.danSoHienTai = 0;

        // Cập nhật UI ngay lập tức
        view.HienThiDanSo(model.danSoHienTai, model.danSoToiDa);

        // --- CHECK GAME OVER ---
        // Chỉ xử lý thua khi dân số về 0 VÀ hành động vừa rồi là trừ dân (soLuong < 0)
        // Điều này tránh lỗi vừa vào game dân số là 0 thì bị xử thua oan.
        if (model.danSoHienTai <= 0 && soLuong < 0) 
        {
            XuLyThuaCuoc();
        }
    }

    private void XuLyThuaCuoc()
    {
        Debug.Log("<color=red>GAME OVER! Dân số đã diệt vong.</color>");
        
        model.daThuaCuoc = true;
        Time.timeScale = 0; // Dừng toàn bộ game (đứng hình)
        
        view.HienThiGameOver(true); // Hiện bảng đen
    }

    // Gán vào nút "Chơi Lại"
    public void BamNutChoiLai()
    {
        // Load lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Hàm gọi khi đánh xong quái (Để sau này Thông dùng)
    public void KetThucWave()
    {
        model.dangTrongTranChien = false;
        model.thoiGianDemNguoc = model.thoiGianGiuaCacDot;
        
        view.BatTatCanhBao(false);
        OnKetThucWave?.Invoke();
    }
}