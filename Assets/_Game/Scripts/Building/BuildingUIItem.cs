using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingUIItem : MonoBehaviour
{
    [Header("--- UI References ---")]
    [SerializeField] private TextMeshProUGUI txtTen;
    [SerializeField] private TextMeshProUGUI txtGia;
    [SerializeField] private Image imgIcon;
    [SerializeField] private Button btnChon;

    private BuildingData dataNha; // Lưu trữ data của nút này

    private void Start()
    {
        // Gán sự kiện click
        btnChon.onClick.AddListener(OnClickXayDung);
    }

    // Hàm này được Menu gọi để điền thông tin vào nút
    public void SetupDuLieu(BuildingData data)
    {
        dataNha = data;

        // Cập nhật giao diện
        if (txtTen != null) txtTen.text = data.tenCongTrinh;
        
        // Hiển thị giá (Ví dụ: "50 Gỗ - 10 Vàng")
        string noiDungGia = "";
        if (data.giaGo > 0) noiDungGia += $"{data.giaGo} Gỗ ";
        if (data.giaVang > 0) noiDungGia += $"{data.giaVang} Vàng";
        if (txtGia != null) txtGia.text = noiDungGia;

        // Cập nhật icon (nếu có hình preview, còn không thì giữ nguyên hình nút)
        // (Lưu ý: Bạn có thể thêm biến Sprite icon vào BuildingData sau này)
    }

    private void OnClickXayDung()
    {
        BuildingManager.Instance.ChonCongTrinhDeXay(dataNha);
        
        // MỚI: Đóng menu ngay khi chọn
        if (BuildMenuController.Instance != null)
        {
            BuildMenuController.Instance.DongMenu();
        }
    }
}