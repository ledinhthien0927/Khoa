using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class LobbyMenuController : MonoBehaviour
{
    [System.Serializable]
    public class MenuButton
    {
        public string name;             // Đặt tên cho dễ nhớ trong Inspector (VD: Continue)
        public Button mainButton;       // Kéo object cha (VD: Btn_Continue)
        public GameObject highlightObj; // Kéo object con (VD: Btn_Continue2)
        public UnityEvent onConfirm;    // Kéo thả chức năng (Load scene, Quit...) vào đây
    }

    // Danh sách các nút trong menu
    public List<MenuButton> menuButtons;

    // Biến lưu trữ nút nào đang được chọn hiện tại
    private MenuButton currentSelectedButton;

    void Start()
    {
        // 1. Setup sự kiện click cho từng nút
        foreach (var btn in menuButtons)
        {
            // Đăng ký hàm OnClick cho nút
            btn.mainButton.onClick.AddListener(() => HandleButtonClick(btn));
            
            // Tắt hết highlight ban đầu để tránh lỗi hiển thị
            if(btn.highlightObj != null) 
                btn.highlightObj.SetActive(false);
        }

        // 2. Mặc định chọn nút đầu tiên (Continue) khi bắt đầu
        if (menuButtons.Count > 0)
        {
            SelectButton(menuButtons[0]);
        }
    }

    // Hàm xử lý logic chính khi nhấn nút
    void HandleButtonClick(MenuButton clickedBtn)
    {
        // TRƯỜNG HỢP 1: Nút này ĐÃ được chọn từ trước -> Thực hiện chức năng
        if (currentSelectedButton == clickedBtn)
        {
            Debug.Log($"Thực hiện chức năng của: {clickedBtn.name}");
            clickedBtn.onConfirm.Invoke(); // Gọi các hàm đã gán trong Inspector
        }
        // TRƯỜNG HỢP 2: Nút này CHƯA được chọn -> Chọn nó (Bật highlight)
        else
        {
            SelectButton(clickedBtn);
        }
    }

    // Hàm chuyển đổi trạng thái highlight
    void SelectButton(MenuButton newBtn)
    {
        // Tắt highlight của nút cũ (nếu đang có nút được chọn)
        if (currentSelectedButton != null && currentSelectedButton.highlightObj != null)
        {
            currentSelectedButton.highlightObj.SetActive(false);
        }

        // Cập nhật nút mới
        currentSelectedButton = newBtn;

        // Bật highlight của nút mới lên
        if (currentSelectedButton.highlightObj != null)
        {
            currentSelectedButton.highlightObj.SetActive(true);
        }
    }
}