using UnityEngine;
using TMPro; // Thư viện TextMeshPro

public class GameView : MonoBehaviour
{
    [Header("--- UI References ---")]
    [SerializeField] private TextMeshProUGUI txtThoiGian;
    [SerializeField] private TextMeshProUGUI txtDanSo;
    [SerializeField] private GameObject panelCanhBao; // Panel đỏ khi có Wave

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

    public void BatTatCanhBao(bool trangThai)
    {
        if (panelCanhBao != null) panelCanhBao.SetActive(trangThai);
    }
}