using UnityEngine;
using UnityEngine.UI;

public class BuildingControlsUI : MonoBehaviour
{
    [Header("--- UI References ---")]
    [SerializeField] private GameObject panelControls; // Kéo Panel_BuildControls vào
    [SerializeField] private Button btnHuy;
    [SerializeField] private Button btnXay;
    [SerializeField] private Button btnXoayTrai;
    [SerializeField] private Button btnXoayPhai;

    private void Start()
    {
        // Gán sự kiện cho các nút
        btnHuy.onClick.AddListener(() => BuildingManager.Instance.HuyCheDoXay());
        btnXay.onClick.AddListener(() => BuildingManager.Instance.XacNhanXayDung());
        
        btnXoayTrai.onClick.AddListener(() => BuildingManager.Instance.XoayNha(-90f));
        btnXoayPhai.onClick.AddListener(() => BuildingManager.Instance.XoayNha(90f));

        // Mặc định tắt
        HienThiPanel(false);
    }

    public void HienThiPanel(bool trangThai)
    {
        if (panelControls != null) panelControls.SetActive(trangThai);
    }
}