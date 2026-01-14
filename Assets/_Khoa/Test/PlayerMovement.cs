using UnityEngine;
// Thêm dòng này để dùng hệ thống mới
using UnityEngine.InputSystem; 

public class PlayerMovement : MonoBehaviour
{
    public float speed = 7f;
    private CharacterController controller;
    private Vector2 moveInput; // Lưu giá trị từ phím WASD

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Hàm này sẽ được gọi mỗi khi bạn nhấn phím (Cần thêm component Player Input)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        // Chuyển Vector2 (x, y) từ phím thành Vector3 (x, 0, z) trong không gian 3D
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        
        // Di chuyển
        controller.Move(move * speed * Time.deltaTime);

        // Trọng lực cơ bản
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }
}