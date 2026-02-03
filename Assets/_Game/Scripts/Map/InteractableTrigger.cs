using UnityEngine;

public class InteractableTrigger : MonoBehaviour
{
    [Header("UI Settings")]
    public string interactPrompt = "UI_Prompt"; // Tên object UI con (Canvas/Sprite)
    public float heightOffset = 2.0f;           // Độ cao của UI so với vật thể (nếu cần chỉnh code)
    
    // --- BIẾN NỘI BỘ ---
    private GameObject promptObj;
    private Camera mainCam;
    private Transform myTransform; // Cache transform để tối ưu

    void Start()
    {
        myTransform = transform;
        mainCam = Camera.main;

        // Tự tìm cái UI con tên là UI_Prompt
        Transform t = transform.Find(interactPrompt);
        if (t != null)
        {
            promptObj = t.gameObject;
            promptObj.SetActive(false); // Ẩn đi lúc đầu
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy UI con tên '{interactPrompt}' trong {gameObject.name}");
        }
    }

    // Dùng LateUpdate để xoay UI sau khi Camera đã di chuyển xong (tránh bị giật hình)
    void LateUpdate()
    {
        // Chỉ xử lý xoay khi UI đang hiện
        if (promptObj != null && promptObj.activeSelf)
        {
            if (mainCam == null) mainCam = Camera.main;

            // --- KỸ THUẬT BILLBOARD ---
            // Cách làm UI luôn đối diện Camera chuẩn nhất:
            // Copy Rotation của Camera gán cho UI.
            // Điều này đảm bảo UI luôn song song với màn hình.
            promptObj.transform.rotation = mainCam.transform.rotation;
            
            // (Tùy chọn) Nếu bạn muốn UI luôn nằm trên đầu vật thể một chút:
            // promptObj.transform.position = myTransform.position + Vector3.up * heightOffset;
        }
    }

    // --- LOGIC TRIGGER (Giữ nguyên tối ưu) ---
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (promptObj) promptObj.SetActive(true);
            
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetCurrentInteractable(this.gameObject);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (promptObj) promptObj.SetActive(false);

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.ClearInteractable();
            }
        }
    }
}