using UnityEngine;
using UnityEngine.Events;
using TMPro; // Thư viện xử lý chữ

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- Cài Đặt Thời Gian ---")]
    [Tooltip("Thời gian giữa các đợt tấn công (giây). 10 phút = 600 giây")]
    [SerializeField] private float thoiGianGiuaCacDot = 600f; 
    
    [Header("--- UI ---")]
    [SerializeField] private TextMeshProUGUI txtHienThiThoiGian; // Kéo cái Text vào đây

    private float thoiGianDemNguoc;
    public int soDotDaVao = 0;
    public bool dangTrongTranChien = false;

    [HideInInspector] public UnityEvent OnBatDauDotTanCong;
    [HideInInspector] public UnityEvent OnKetThucDotTanCong;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        ResetDongHoDemNguoc();
    }

    private void Update()
    {
        QuanLyThoiGian();
    }

    private void QuanLyThoiGian()
    {
        // Cập nhật UI
        if (txtHienThiThoiGian != null)
        {
            // Chuyển đổi giây sang định dạng Phút:Giây (VD: 09:59)
            int phut = Mathf.FloorToInt(thoiGianDemNguoc / 60);
            int giay = Mathf.FloorToInt(thoiGianDemNguoc % 60);
            txtHienThiThoiGian.text = string.Format("{0:00}:{1:00}", phut, giay);
            
            if (dangTrongTranChien) txtHienThiThoiGian.text = "<color=red>ĐANG TẤN CÔNG!</color>";
        }

        if (dangTrongTranChien) return;

        thoiGianDemNguoc -= Time.deltaTime;

        if (thoiGianDemNguoc <= 0)
        {
            BatDauDotTanCong();
        }
    }

    private void BatDauDotTanCong()
    {
        dangTrongTranChien = true;
        soDotDaVao++;
        thoiGianDemNguoc = 0;
        Debug.Log($"<color=red>CẢNH BÁO: Đợt tấn công thứ {soDotDaVao} bắt đầu!</color>");
        OnBatDauDotTanCong?.Invoke();
    }

    public void KetThucDotTanCong()
    {
        dangTrongTranChien = false;
        Debug.Log("<color=green>Đợt kết thúc.</color>");
        OnKetThucDotTanCong?.Invoke();
        ResetDongHoDemNguoc();
    }

    private void ResetDongHoDemNguoc()
    {
        thoiGianDemNguoc = thoiGianGiuaCacDot;
    }
}