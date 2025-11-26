using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Island/Building Data")]
public class BuildingData : ScriptableObject
{
    public string tenCongTrinh;
    public GameObject prefabGoc; // Prefab ngôi nhà thật
    public GameObject prefabHienThi; // (Optional) Hình bóng mờ để xem trước
    
    [Header("--- Chi Phí ---")]
    public int giaGo;
    public int giaVang;

    [Header("--- Tác Dụng ---")]
    public int tangDanSoToiDa = 0; // Nhà dân thì tăng cái này
}