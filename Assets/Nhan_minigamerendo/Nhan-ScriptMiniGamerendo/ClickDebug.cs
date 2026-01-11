using UnityEngine;

public class ClickDebug : MonoBehaviour
{
    void Update()
    {
        // Số 0 = Chuột Trái
        if (Input.GetMouseButtonDown(0)) 
        {
            // Tạo một tia bắn từ Camera đến vị trí chuột
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Nếu bắn trúng cái gì đó (vật lý 3D)
            if (Physics.Raycast(ray, out hit))
            {
                // In tên kẻ bị bắn trúng ra Console
                Debug.Log("BẠN ĐANG CLICK VÀO: " + hit.collider.gameObject.name);
                
                // Vẽ tia màu đỏ trong Scene để dễ nhìn (giữ trong 2 giây)
                Debug.DrawLine(Camera.main.transform.position, hit.point, Color.red, 2.0f);
            }
            else
            {
                Debug.Log("Click vào hư không (Không trúng Collider nào)");
            }
        }
    }
}