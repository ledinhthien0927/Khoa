using UnityEngine;

public class LockController3D : MonoBehaviour
{
    [Header("Rings Setup")]
    public RingBehavior3D outerRing;
    public RingBehavior3D middleRing;
    public RingBehavior3D innerRing;

    [Header("Light System")]
    public LineRenderer lightBeam;
    public Transform[] lightNodes; // Đặt 5 object: Source, Outer, Middle, Inner, Center

    private int activeRingIndex = 0; // 0: Ngoài, 1: Giữa, 2: Trong

    void Start()
    {
        lightBeam.useWorldSpace = true;
        UpdateLightPath3D();
    }

    void Update()
    {
        // Chặn input nếu hệ thống đang chạy animation xoay
        if (outerRing.IsRotating() || middleRing.IsRotating() || innerRing.IsRotating()) return;

        HandleSelection();
        HandleRotation();
    }

    void HandleSelection()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Space))
        {
            activeRingIndex = (activeRingIndex + 1) % 3;
            Debug.Log("Đang điều khiển vòng: " + (activeRingIndex == 0 ? "NGOÀI" : activeRingIndex == 1 ? "GIỮA" : "TRONG"));
        }
    }

    void HandleRotation()
    {
        int rotationDir = 0;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) rotationDir = 1;  
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) rotationDir = -1; 

        if (rotationDir != 0)
        {
            ApplyTheTwist(rotationDir);
            
            // Đợi animation xoay hoàn tất (0.3s) rồi mới bắn tia sáng kiểm tra
            Invoke("UpdateLightPath3D", 0.3f); 
        }
    }

    // ĐÂY LÀ TRÁI TIM CỦA LOGIC GAME
    void ApplyTheTwist(int dir)
    {
        switch (activeRingIndex)
        {
            case 0: 
                // Xoay Vòng Ngoài -> Chỉ Vòng Ngoài bị ảnh hưởng
                outerRing.RotateRing(dir);
                break;
                
            case 1: 
                // Xoay Vòng Giữa -> Vòng Giữa xoay, KÉO Vòng Ngoài xoay CÙNG CHIỀU
                middleRing.RotateRing(dir);
                outerRing.RotateRing(dir); 
                break;
                
            case 2: 
                // Xoay Vòng Trong -> Vòng Trong xoay, KÉO Vòng Giữa xoay NGƯỢC CHIỀU (-dir).
                // [CHUỖI DÂY CHUYỀN]: Vì Vòng Giữa vừa bị ép xoay (-dir), nó sẽ áp dụng quy tắc của nó là kéo Vòng Ngoài cùng chiều với nó -> Vòng Ngoài cũng xoay (-dir).
                innerRing.RotateRing(dir);
                middleRing.RotateRing(-dir); 
                outerRing.RotateRing(-dir); 
                break;
        }
    }

    void UpdateLightPath3D()
    {
        // Bắt đầu vẽ tia sáng từ Nguồn
        lightBeam.positionCount = 1;
        lightBeam.SetPosition(0, lightNodes[0].position);

        // Quy tắc: Nguồn phát ánh sáng Xanh Dương (Blue)
        LightColor currentColor = LightColor.Blue;

        // 1. Kiểm tra Vòng Ngoài (Cần đúng đá Blue để tiếp tục, và sẽ biến ánh sáng thành Red)
        if (outerRing.GetTopGemColor() == currentColor)
        {
            lightBeam.positionCount = 2;
            lightBeam.SetPosition(1, lightNodes[1].position);
            currentColor = LightColor.Red;
            
            // 2. Kiểm tra Vòng Giữa (Cần đá Red, biến thành Purple)
            if (middleRing.GetTopGemColor() == currentColor)
            {
                lightBeam.positionCount = 3;
                lightBeam.SetPosition(2, lightNodes[2].position);
                currentColor = LightColor.Purple;

                // 3. Kiểm tra Vòng Trong (Cần đá Purple, biến thành Yellow)
                if (innerRing.GetTopGemColor() == currentColor)
                {
                    lightBeam.positionCount = 4;
                    lightBeam.SetPosition(3, lightNodes[3].position);
                    currentColor = LightColor.Yellow;

                    // CHIẾN THẮNG: Tia sáng vàng chiếu vào tâm!
                    lightBeam.positionCount = 5;
                    lightBeam.SetPosition(4, lightNodes[4].position);
                    Debug.Log("BÙM! Ổ KHÓA ĐÃ MỞ!");
                    // Gọi hàm Play âm thanh chiến thắng, chạy animation mở cửa ở đây
                }
            }
        }
    }
}