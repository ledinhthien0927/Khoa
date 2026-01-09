using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 2.0f;
    public float startScale = 1.5f;
    public float endScale = 0.2f;

    [Header("References")]
    public Transform visualTransform; 
    public SpriteRenderer targetSprite;
    
    [Header("Colors")]
    public Color startColor = new Color(0.8f, 1f, 1f, 1f);
    public Color perfectColor = Color.green;
    public Color lateColor = Color.yellow;
    
    private float timer;
    private bool isClicked = false;

    void OnEnable()
    {
        timer = 0;
        isClicked = false;
        
        if (visualTransform != null) visualTransform.localScale = Vector3.one * startScale;
        transform.localScale = Vector3.one; 

        if(targetSprite != null) targetSprite.color = startColor;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (isClicked) return;

        timer += Time.deltaTime;
        float progress = timer / lifetime;

        if (visualTransform != null)
        {
            float currentScale = Mathf.Lerp(startScale, endScale, progress);
            visualTransform.localScale = Vector3.one * currentScale;
        }

        UpdateColor(progress);

        // --- HẾT GIỜ (TIMEOUT) ---
        if (progress >= 1.0f) 
        {
            // Báo cho Manager biết là bị Timeout để spawn cái mới
            if (SmithingManager.Instance != null)
            {
                SmithingManager.Instance.HandleTimeout();
            }
            // Tự hủy
            gameObject.SetActive(false);
        }
    }

    void UpdateColor(float progress)
    {
        if (targetSprite == null) return;
        if (progress < 0.5f) targetSprite.color = Color.Lerp(startColor, perfectColor, progress / 0.5f);
        else if (progress < 0.75f) targetSprite.color = perfectColor;
        else targetSprite.color = Color.Lerp(perfectColor, lateColor, (progress - 0.75f) / 0.15f);
    }

    void OnMouseDown()
    {
        if (isClicked) return;
        isClicked = true;

        float progress = timer / lifetime;
        int score = 0; // Mặc định là trượt/kém
        
        // Vùng Perfect: 0.3 đến 0.8
        if (progress >= 0.3f && progress < 0.8f) 
        {
            score = 100; 
        }
        else 
        {
            score = 10; // Đánh trúng nhưng không chuẩn
        }

        // Bắn Raycast lấy hướng
        Vector3 hitNormal = Vector3.up;
        Vector3 hitPoint = transform.position;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Nếu dùng camera riêng:
        // Ray ray = SmithingManager.Instance.smithingCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            hitNormal = hit.normal;
            hitPoint = hit.point;
        }

        OnStrike(score, hitNormal);
    }

    void OnStrike(int score, Vector3 normal)
    {
        // Luôn gọi Player để thực hiện hành động đập
        // Kể cả điểm thấp cũng đập (để tạo hiệu ứng)
        if (PlayerSmithing.Instance != null)
        {
            PlayerSmithing.Instance.SmashAt(transform.position, normal, score);
        }

        gameObject.SetActive(false);
    }

    void OnMouseEnter() { if(targetSprite != null) targetSprite.color = Color.red; }
    void OnMouseExit() { if(targetSprite != null) targetSprite.color = Color.white; }
}