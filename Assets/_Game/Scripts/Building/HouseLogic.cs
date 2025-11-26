using UnityEngine;

public class HouseLogic : MonoBehaviour
{
    // Biến này sẽ được BuildingManager gán vào khi xây xong
    [HideInInspector] public int soChoOThem = 0;

    private void Start()
    {
        // Báo cáo tăng giới hạn dân số
        if (soChoOThem > 0)
        {
            GameManager.Instance.CapNhatGioiHanDanSo(soChoOThem);
        }
    }

    private void OnDestroy()
    {
        // Khi nhà bị quái phá hủy -> Giảm giới hạn dân số
        if (GameManager.Instance != null && soChoOThem > 0)
        {
            GameManager.Instance.CapNhatGioiHanDanSo(-soChoOThem);
        }
    }
}