using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }
    [SerializeField] private LayerMask layerVatCan;

    [Header("--- Visual ---")]
    [SerializeField] private Material matXanh; // Material trong suốt màu xanh
    [SerializeField] private Material matDo;   // Material trong suốt màu đỏ

    [Header("--- Cấu Hình ---")]
    [SerializeField] private LayerMask layerMatDat; // Layer để nhận diện mặt đất (Ground)
    [SerializeField] private Material vatLieuXayDungHople; // Màu xanh (khi đặt được)
    [SerializeField] private Material vatLieuXayDungSai;   // Màu đỏ (khi không đủ tiền)

    // Trạng thái nội bộ
    private BuildingData congTrinhDangChon;
    private GameObject doiTuongPreview; // Cái bóng mờ đang đi theo chuột
    private bool dangOCheDoXay = false;
    private bool viTriHopLe = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (!dangOCheDoXay || congTrinhDangChon == null) return;

        XuLyPreview();
        XuLyInputDatNha();
        XuLyHuyXay();
    }

    // 1. Hàm gọi từ nút bấm UI để bắt đầu xây
    public void ChonCongTrinhDeXay(BuildingData data)
    {
        if (dangOCheDoXay) HuyCheDoXay(); // Reset nếu đang chọn cái khác

        dangOCheDoXay = true;
        congTrinhDangChon = data;

        // Tạo bóng mờ
        doiTuongPreview = Instantiate(data.prefabGoc);
        
        // 1. Tắt tất cả script logic trên bóng mờ (để nó không chạy lung tung)
        foreach (var script in doiTuongPreview.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;

        // 2. Tắt Collider vật lý (để chuột không bị Raycast trúng chính nó)
        foreach (var coll in doiTuongPreview.GetComponentsInChildren<Collider>())
            coll.enabled = false;
    }

    // 2. Di chuyển bóng mờ theo chuột
    private void XuLyPreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, layerMatDat))
        {
            // Di chuyển bóng mờ
            doiTuongPreview.transform.position = hit.point;
            doiTuongPreview.SetActive(true);

            // --- KIỂM TRA VA CHẠM (MỚI) ---
            KiemTraViTriXayDung(hit.point);
        }
        else
        {
            doiTuongPreview.SetActive(false); // Ra khỏi đất thì ẩn đi
        }
    }
    private void KiemTraViTriXayDung(Vector3 center)
    {
        // Lấy kích thước của ngôi nhà (Dựa vào Collider có sẵn trong Prefab gốc)
        // Lưu ý: Ta lấy BoxCollider từ Prefab gốc trong Data, chứ không phải từ doiTuongPreview (vì ta đã tắt collider của nó rồi)
        BoxCollider boxCol = congTrinhDangChon.prefabGoc.GetComponent<BoxCollider>();
        
        Vector3 size = Vector3.one; // Mặc định 1x1x1 nếu không tìm thấy collider
        if (boxCol != null) size = boxCol.size;

        // Bắn một cái hộp ảo để kiểm tra va chạm
        // size / 2 vì hàm OverlapBox dùng HalfExtents (bán kính)
        Collider[] vaCham = Physics.OverlapBox(center + new Vector3(0, size.y/2, 0), size / 2, Quaternion.identity, layerVatCan);

        // Nếu va chạm > 0 tức là trúng vật cản -> Không hợp lệ
        viTriHopLe = (vaCham.Length == 0);

        // Đổi màu
        DoiMauPreview(viTriHopLe ? matXanh : matDo);
    }

    private void DoiMauPreview(Material matMoi)
    {
        // Lấy tất cả Renderer trong bóng mờ để đổi màu
        Renderer[] renderers = doiTuongPreview.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = matMoi;
        }
    }

    // 3. Click chuột để đặt
    private void XuLyInputDatNha()
    {
        // MỚI: Thêm điều kiện && viTriHopLe
        if (Input.GetMouseButtonDown(0) && doiTuongPreview.activeSelf && viTriHopLe)
        {
            bool duTien = GameManager.Instance.ThayDoiTaiNguyen(-congTrinhDangChon.giaGo, -congTrinhDangChon.giaVang);

            if (duTien)
            {
                // Xây thật
                GameObject nhaMoi = Instantiate(congTrinhDangChon.prefabGoc, doiTuongPreview.transform.position, Quaternion.identity);
                
                // Set Layer cho nhà mới thành "Obstacle" hoặc "Building" để nhà sau không xây đè lên
                SetLayerRecursive(nhaMoi, LayerMask.NameToLayer("Obstacle"));

                HouseLogic logic = nhaMoi.GetComponent<HouseLogic>();
                if (logic == null) logic = nhaMoi.AddComponent<HouseLogic>();
                logic.soChoOThem = congTrinhDangChon.tangDanSoToiDa;

                // Xây xong thì thoát chế độ xây luôn (hoặc giữ nguyên tùy bạn)
                HuyCheDoXay(); 
            }
            else
            {
                Debug.Log("Không đủ tiền!");
            }
        }
    }

        private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    private void XuLyHuyXay()
    {
        // Bấm chuột phải để hủy xây
        if (Input.GetMouseButtonDown(1))
        {
            HuyCheDoXay();
        }
    }

    private void HuyCheDoXay()
    {
        dangOCheDoXay = false;
        congTrinhDangChon = null;
        if (doiTuongPreview != null) Destroy(doiTuongPreview);
    }
}