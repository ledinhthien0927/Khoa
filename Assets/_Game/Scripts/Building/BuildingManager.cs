using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("--- Cấu Hình ---")]
    [SerializeField] private LayerMask layerMatDat;
    [SerializeField] private LayerMask layerVatCan;
    
    [Header("--- Camera System (MỚI) ---")]
    [SerializeField] private Camera mainCamera;  // Kéo Main Camera vào
    [SerializeField] private Camera buildCamera; // Kéo BuildCamera vào

    [Header("--- UI Controller (MỚI) ---")]
    [SerializeField] private BuildingControlsUI uiControls; // Kéo script UI vừa tạo vào

    [Header("--- Visual ---")]
    [SerializeField] private Material matXanh; 
    [SerializeField] private Material matDo;   

    // Trạng thái nội bộ
    private BuildingData congTrinhDangChon;
    private GameObject doiTuongPreview;
    private bool dangOCheDoXay = false;
    private bool viTriHopLe = false;
    
    // Biến lưu góc xoay hiện tại
    private float gocXoayHienTai = 0f; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (!dangOCheDoXay || congTrinhDangChon == null) return;

        XuLyPreview();
        // Lưu ý: Ta ĐÃ BỎ hàm XuLyInputDatNha() cũ đi 
        // vì bây giờ ta dùng nút bấm UI (V/X) chứ không click chuột nữa.
    }

    public void ChonCongTrinhDeXay(BuildingData data)
    {
        if (dangOCheDoXay) HuyCheDoXay();

        dangOCheDoXay = true;
        congTrinhDangChon = data;
        gocXoayHienTai = 0f; // Reset góc xoay

        // 1. Chuyển Camera
        DoiCamera(true);

        // 2. Hiện UI điều khiển
        if (uiControls != null) uiControls.HienThiPanel(true);

        // 3. Tạo bóng mờ
        doiTuongPreview = Instantiate(data.prefabGoc);
        foreach (var script in doiTuongPreview.GetComponentsInChildren<MonoBehaviour>()) script.enabled = false;
        foreach (var coll in doiTuongPreview.GetComponentsInChildren<Collider>()) coll.enabled = false;
    }

    private void DoiCamera(bool cheDoXay)
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(!cheDoXay);
        if (buildCamera != null) buildCamera.gameObject.SetActive(cheDoXay);
    }

    private void XuLyPreview()
    {
        // --- PHẦN 1: LUÔN CẬP NHẬT GÓC XOAY ---
        // Phải làm việc này đầu tiên, để khi bấm nút là thấy xoay ngay
        if (doiTuongPreview != null)
        {
            doiTuongPreview.transform.rotation = Quaternion.Euler(0, gocXoayHienTai, 0);
        }

        // --- PHẦN 2: CẬP NHẬT VỊ TRÍ (CHỈ KHI KHÔNG CHẠM UI) ---
        // Nếu chuột không đè lên nút thì mới cho di chuyển nhà
        if (!IsPointerOverUI())
        {
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f, layerMatDat))
            {
                // Tính toán độ cao (Offset)
                float chieuCaoOffset = 0f;
                BoxCollider boxCol = congTrinhDangChon.prefabGoc.GetComponent<BoxCollider>();
                if (boxCol != null)
                {
                    chieuCaoOffset = (boxCol.size.y / 2) + boxCol.center.y;
                }

                Vector3 viTriDat = hit.point + new Vector3(0, chieuCaoOffset, 0);
                
                // Cập nhật vị trí mới
                doiTuongPreview.transform.position = viTriDat;
                doiTuongPreview.SetActive(true);
            }
        }

        // --- PHẦN 3: LUÔN KIỂM TRA HỢP LỆ (XANH/ĐỎ) ---
        // Dù nhà đứng yên (do đang bấm nút), nhưng khi xoay nó có thể va vào vật cản
        // nên ta phải check lại màu sắc ngay tại vị trí hiện tại.
        if (doiTuongPreview.activeSelf)
        {
            KiemTraViTriXayDung(doiTuongPreview.transform.position);
        }
    }

    // Hàm gọi từ nút Mũi tên UI
    public void XoayNha(float goc)
    {
        gocXoayHienTai += goc;
    }

    // Hàm gọi từ nút "V"
    public void XacNhanXayDung()
    {
        if (!viTriHopLe || !dangOCheDoXay) 
        {
            Debug.Log("Vị trí không hợp lệ!");
            return;
        }

        bool duTien = GameManager.Instance.ThayDoiTaiNguyen(-congTrinhDangChon.giaGo, -congTrinhDangChon.giaVang);
        if (duTien)
        {
            // Xây thật tại vị trí và góc xoay của Preview
            GameObject nhaMoi = Instantiate(congTrinhDangChon.prefabGoc, doiTuongPreview.transform.position, Quaternion.Euler(0, gocXoayHienTai, 0));
            SetLayerRecursive(nhaMoi, LayerMask.NameToLayer("Obstacle"));

            HouseLogic logic = nhaMoi.GetComponent<HouseLogic>();
            if (logic == null) logic = nhaMoi.AddComponent<HouseLogic>();
            logic.soChoOThem = congTrinhDangChon.tangDanSoToiDa;

            // Xây xong thì thoát
            HuyCheDoXay();
        }
        else
        {
            Debug.Log("Không đủ tiền!");
        }
    }

    // Hàm gọi từ nút "X"
    public void HuyCheDoXay()
    {
        dangOCheDoXay = false;
        congTrinhDangChon = null;
        if (doiTuongPreview != null) Destroy(doiTuongPreview);

        // Trả lại Camera chính
        DoiCamera(false);

        // Ẩn UI điều khiển
        if (uiControls != null) uiControls.HienThiPanel(false);
    }

    private void KiemTraViTriXayDung(Vector3 center)
    {
        BoxCollider boxCol = congTrinhDangChon.prefabGoc.GetComponent<BoxCollider>();
        Vector3 size = (boxCol != null) ? boxCol.size : Vector3.one;

        // Quét va chạm (Lưu ý phải xoay hộp quét theo góc xoay của nhà)
        Collider[] vaCham = Physics.OverlapBox(center, size / 2 * 0.9f, Quaternion.Euler(0, gocXoayHienTai, 0), layerVatCan);

        viTriHopLe = (vaCham.Length == 0);
        DoiMauPreview(viTriHopLe ? matXanh : matDo);
    }
    
    private void DoiMauPreview(Material matMoi)
    {
        Renderer[] renderers = doiTuongPreview.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.material = matMoi;
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, newLayer);
    }

    // Hàm chính để kiểm tra (Gồm cả chuột và đa điểm cảm ứng)
    private bool IsPointerOverUI()
    {
        // 1. Kiểm tra vị trí chuột (Cho PC/Editor)
        if (IsPointerOverUIObject(Input.mousePosition)) return true;

        // 2. Kiểm tra tất cả các ngón tay (Cho Mobile)
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (IsPointerOverUIObject(Input.GetTouch(i).position)) return true;
            }
        }
        return false;
    }

    // Hàm phụ: Tự bắn tia Raycast vào hệ thống UI để kiểm tra va chạm
    private bool IsPointerOverUIObject(Vector2 screenPos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;
        
        List<RaycastResult> results = new List<RaycastResult>();
        
        // Bắn tia kiểm tra xem tại vị trí màn hình này có trúng cái UI nào không
        EventSystem.current.RaycastAll(eventData, results);
        
        return results.Count > 0;
    }
}