using UnityEngine;
using System.Collections.Generic;

public class BuildMenuController : MonoBehaviour
{
    public static BuildMenuController Instance { get; private set; } // Singleton

    [Header("--- Cấu Hình ---")]
    [SerializeField] private GameObject prefabNutBam;
    [SerializeField] private Transform noiChuaNut;
    [SerializeField] private GameObject panelMenuToanBo; 
    [SerializeField] private List<BuildingData> danhSachCongTrinh; 

    private bool dangHienThi = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        KhoiTaoMenu();
        if (panelMenuToanBo != null) panelMenuToanBo.SetActive(false);
    }

    private void KhoiTaoMenu()
    {
        foreach (Transform child in noiChuaNut) Destroy(child.gameObject);
        foreach (BuildingData data in danhSachCongTrinh)
        {
            GameObject nutMoi = Instantiate(prefabNutBam, noiChuaNut);
            BuildingUIItem logicNut = nutMoi.GetComponent<BuildingUIItem>();
            if (logicNut != null) logicNut.SetupDuLieu(data);
        }
    }

    public void ToggleMenu()
    {
        dangHienThi = !dangHienThi;
        if (panelMenuToanBo != null) panelMenuToanBo.SetActive(dangHienThi);
    }

    // MỚI: Hàm đóng menu (để gọi khi chọn xong)
    public void DongMenu()
    {
        dangHienThi = false;
        if (panelMenuToanBo != null) panelMenuToanBo.SetActive(false);
    }
}