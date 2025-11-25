using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- MVC Components ---")]
    public GameModel model; // Kéo thả hoặc tự new
    [SerializeField] private GameView view; // Kéo script GameView vào đây

    [HideInInspector] public UnityEvent OnBatDauWave;
    [HideInInspector] public UnityEvent OnKetThucWave;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        // Khởi tạo dữ liệu mới
        if (model == null) model = new GameModel();
    }

    private void Start()
    {
        model.thoiGianDemNguoc = model.thoiGianGiuaCacDot;
        model.soDotDaVao = 0;

        // Cập nhật giao diện lần đầu
        view.HienThiDanSo(model.danSoHienTai, model.danSoToiDa);
    }

    private void Update()
    {
        XuLyThoiGian();
    }

    private void XuLyThoiGian()
    {
        if (model.dangTrongTranChien) return;

        model.thoiGianDemNguoc -= Time.deltaTime;
        
        // Cập nhật text đồng hồ
        view.HienThiThoiGian(model.thoiGianDemNguoc, false);

        // --- CODE MỚI: Tính phần trăm thời gian để quay mặt trời ---
        // Ví dụ: Còn 8 phút / 10 phút => 0.8 (80%)
        float phanTram = model.thoiGianDemNguoc / model.thoiGianGiuaCacDot;
        
        // Gọi View cập nhật đèn
        view.CapNhatAnhSang(phanTram);
        // -----------------------------------------------------------

        if (model.thoiGianDemNguoc <= 0)
        {
            BatDauWave();
        }
    }

    private void BatDauWave()
    {
        model.dangTrongTranChien = true;
        model.soDotDaVao++;
        
        view.HienThiThoiGian(0, true);
        view.BatTatCanhBao(true);
        
        Debug.Log($"Wave {model.soDotDaVao} Start!");
        OnBatDauWave?.Invoke();
    }

    // --- CÁC HÀM PUBLIC (Để script khác gọi vào) ---

    public void ThayDoiDanSo(int soLuong)
    {
        model.danSoHienTai += soLuong;
        if (model.danSoHienTai < 0) model.danSoHienTai = 0;

        // Logic xong thì báo View vẽ lại
        view.HienThiDanSo(model.danSoHienTai, model.danSoToiDa);
    }

    public void KetThucWave()
    {
        model.dangTrongTranChien = false;
        model.thoiGianDemNguoc = model.thoiGianGiuaCacDot;
        
        view.BatTatCanhBao(false);
        OnKetThucWave?.Invoke();
    }
}