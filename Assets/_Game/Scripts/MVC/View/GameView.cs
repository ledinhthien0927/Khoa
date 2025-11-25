using UnityEngine;
using TMPro; // Thư viện TextMeshPro
using System.Collections;

public class GameView : MonoBehaviour
{
    [Header("--- UI References ---")]
    [SerializeField] private TextMeshProUGUI txtThoiGian;
    [SerializeField] private TextMeshProUGUI txtDanSo;
    [SerializeField] private GameObject panelCanhBao; // Panel đỏ khi có Wave

    [Header("--- Environment References (MỚI) ---")]
    [SerializeField] private Light anhSangMatTroi; // Kéo Directional Light vào đây

    public void HienThiThoiGian(float thoiGian, bool dangChienDau)
    {
        if (dangChienDau)
        {
            txtThoiGian.text = "<color=red>ĐANG TẤN CÔNG!</color>";
        }
        else
        {
            // Đổi giây sang phút:giây
            int phut = Mathf.FloorToInt(thoiGian / 60);
            int giay = Mathf.FloorToInt(thoiGian % 60);
            txtThoiGian.text = string.Format("{0:00}:{1:00}", phut, giay);
        }
    }

    public void HienThiDanSo(int hienTai, int toiDa)
{
    // Thêm dòng này để test xem hàm có chạy không
    Debug.Log($"View đang vẽ dân số: {hienTai}/{toiDa}"); 

    if (txtDanSo != null)
    {
        txtDanSo.text = $"Dân số: {hienTai} / {toiDa}";
    }
    else
    {
        Debug.LogError("LỖI: Chưa kéo Txt_DanSo vào script GameView!");
    }
}

    public void CapNhatAnhSang(float phanTramThoiGian)
    {
        if (anhSangMatTroi == null) return;

        // phanTramThoiGian: 1.0 (Bắt đầu) -> 0.0 (Hết giờ)
        
        // Quy ước góc quay của đèn (Rotation X):
        // 20 độ: Sáng sớm
        // 90 độ: Giữa trưa
        // 180 độ: Tối thui
        
        // Công thức nội suy (Lerp): Biến thiên từ 20 -> 180 dựa trên thời gian trôi qua
        // Vì thời gian chạy từ 1 -> 0, nên ta lấy (1 - phanTram) để chạy từ 0 -> 1
        float tienDo = 1f - phanTramThoiGian; 
        
        float gocQuay = Mathf.Lerp(20f, 180f, tienDo);

        // Xoay đèn theo trục X
        anhSangMatTroi.transform.rotation = Quaternion.Euler(gocQuay, -30f, 0f);

        // Đổi màu ánh sáng cho đẹp (Optional): Sáng trắng -> Chiều cam -> Tối xanh
        if (tienDo < 0.5f) // Sáng -> Trưa
            anhSangMatTroi.color = Color.Lerp(Color.yellow, Color.white, tienDo * 2);
        else // Trưa -> Tối
            anhSangMatTroi.color = Color.Lerp(Color.white, new Color(0.1f, 0.1f, 0.4f), (tienDo - 0.5f) * 2);
    }

    public void BatTatCanhBao(bool kichHoat)
    {
        // 1. Dù bật hay tắt, việc đầu tiên là dừng các lệnh nhấp nháy cũ (nếu có)
        // để tránh bị lỗi chồng chéo 2 luồng nháy cùng lúc.
        StopAllCoroutines();
        
        if (kichHoat)
        {
            // Nếu bật -> Bắt đầu chạy quy trình nhấp nháy 3 lần
            StartCoroutine(IE_HieuUngNhayCanhBao());
        }
        else
        {
            // Nếu tắt -> Tắt ngay lập tức
            if (panelCanhBao != null) panelCanhBao.SetActive(false);
        }
    }

    // Coroutine: Hàm xử lý theo trình tự thời gian
    private IEnumerator IE_HieuUngNhayCanhBao()
    {
        // Lặp 3 lần
        for (int i = 0; i < 3; i++)
        {
            // Bật lên
            if (panelCanhBao != null) panelCanhBao.SetActive(true);
            
            // Đợi 0.5 giây (Sáng)
            yield return new WaitForSeconds(0.5f);

            // Tắt đi
            if (panelCanhBao != null) panelCanhBao.SetActive(false);

            // Đợi 0.5 giây (Tối)
            yield return new WaitForSeconds(0.5f);
            
            // Tổng cộng 1 vòng lặp tốn 1 giây (0.5s sáng + 0.5s tối)
        }
        
        // Sau khi chạy xong vòng lặp 3 lần, code tự dừng -> Panel tự tắt.
    }

}