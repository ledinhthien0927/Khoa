using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public interface IDamageable
{
    // Hàm nhận sát thương
    void NhanSatThuong(int luongSatThuong);
    
    // Kiểm tra xem đã chết chưa
    bool DaChet();
}
}
