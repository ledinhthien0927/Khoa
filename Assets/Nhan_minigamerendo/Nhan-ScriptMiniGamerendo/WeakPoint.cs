using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 2.0f;
    public float startScale = 1.5f;
    public float endScale = 0.2f;

    [Header("References")]
    // SỬA: Biến này dùng để tham chiếu đến thằng Con (Visual)
    public Transform visualTransform; 
    public SpriteRenderer targetSprite;
    // MÀU SẮC
    public Color startColor = new Color(0.8f, 1f, 1f, 1f);
    public Color perfectColor = Color.green;
    public Color lateColor = Color.yellow;
    public Color failColor = Color.red;

    private float timer;
    private bool isClicked = false;

    // SỬA: Dùng OnEnable để Reset khi tái sử dụng
    void OnEnable()
    {
        timer = 0;
        isClicked = false;
        
        // SỬA: Reset scale của thằng CON, chứ không phải thằng Cha
        if (visualTransform != null)
        {
            visualTransform.localScale = Vector3.one * startScale;
        }
        
        // Đảm bảo Reset Collider của thằng Cha về kích thước chuẩn (1)
        transform.localScale = Vector3.one; 

        if(targetSprite != null) targetSprite.color = startColor;
        gameObject.SetActive(true);
    }
   void Update()
    {
        if (isClicked) return;

        timer += Time.deltaTime;
        float progress = timer / lifetime;

        // --- SỬA QUAN TRỌNG NHẤT ---
        // Chỉ co nhỏ thằng Visual (Hình ảnh)
        // Collider nằm ở "gameObject" (Cha) nên nó sẽ KHÔNG bị co lại
        if (visualTransform != null)
        {
            float currentScale = Mathf.Lerp(startScale, endScale, progress);
            visualTransform.localScale = Vector3.one * currentScale;
        }

        UpdateColor(progress);

        if (progress >= 1.0f) OnStrike(0);
    }

    void UpdateColor(float progress)
    {
        if (targetSprite == null) return;

        // Logic đổi màu giữ nguyên, chỉ thay targetRing thành targetSprite
        // (Copy lại logic ngưỡng perfectStart, lateStart... của bạn vào đây)
        // Ví dụ rút gọn:
        if (progress < 0.5f) targetSprite.color = Color.Lerp(startColor, perfectColor, progress / 0.5f);
        else if (progress < 0.75f) targetSprite.color = perfectColor;
        else targetSprite.color = Color.Lerp(perfectColor, lateColor, (progress - 0.75f) / 0.15f);
    }

    // SỬA: Dùng hàm này để bắt click chuột trực tiếp trên vật thể 3D/2D
    // Yêu cầu vật thể phải có Collider (Xem Bước 2)
    void OnMouseDown()
    {
        Debug.Log("Đã bấm trúng!");
        if (isClicked) return;
        
        // Tính điểm ở đây (Copy logic tính điểm cũ vào)
        // Ví dụ:
        float progress = timer / lifetime;
        int score = 10;
        if (progress >= 0.3f && progress < 0.8f) // Ví dụ vùng Perfect
        {
            score = 100; // <--- PHẢI LÀ 100 THÌ MỚI ĐƯỢC TÍNH
        }
        else 
        {
            score = 10; // Cái này đập chơi thôi, không tính vào điều kiện thắng
        }

        OnStrike(score);
    }

  void OnStrike(int score)
    {
        if (score > 0)
        {
            // GỌI PLAYER THAY VÌ GỌI HAMMER CŨ
            if (PlayerSmithing.Instance != null)
            {
                // Truyền vị trí của vòng tròn vào để Player xoay tới đó
                PlayerSmithing.Instance.SmashAt(transform.position, score);
            }
        }
        else
        {
            Debug.Log("Miss!");
        }

        gameObject.SetActive(false);
    }
    void OnMouseEnter()
    {
        if(targetSprite != null) targetSprite.color = Color.red;
    }

    // Di chuột ra thì trả lại màu trắng
    void OnMouseExit()
    {
        if(targetSprite != null) targetSprite.color = Color.white;
    }
}