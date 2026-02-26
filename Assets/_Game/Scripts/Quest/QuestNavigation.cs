using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class QuestNavigation : MonoBehaviour
{
    public static QuestNavigation Instance;
    
    [Header("UI & Line")]
    public LineRenderer line;
    public TextMeshProUGUI statusText;
    
    [Header("Target Marker")]
    public GameObject arrowPrefab; // Kéo thả prefab mũi tên của bạn vào đây
    public float arrowHeightOffset = 2f; // Chiều cao mũi tên so với mặt đất
    public float bobbingSpeed = 5f; // Tốc độ nhấp nhô của mũi tên
    public float bobbingAmount = 0.2f; // Biên độ nhấp nhô

    private Transform target;
    private bool isNavigating;
    private GameObject currentArrow;

    void Awake() => Instance = this;

    void Start()
    {
        // Chỉnh chế độ Texture để có thể lặp (cần thiết cho nét đứt)
        line.textureMode = LineTextureMode.Tile;
    }

    public void ToggleNavigation(Transform newTarget, string questName)
    {
        if (isNavigating && target == newTarget) { StopNav(); return; }
        
        target = newTarget;
        isNavigating = true;
        line.enabled = true;
        statusText.gameObject.SetActive(true);
        statusText.text = "Đang dẫn đường: " + questName;

        // Bật hoặc tạo mũi tên tại điểm đến
        if (currentArrow == null && arrowPrefab != null)
        {
            currentArrow = Instantiate(arrowPrefab);
        }
        
        if (currentArrow != null)
        {
            currentArrow.SetActive(true);
        }
    }

    public void StopNav()
    {
        isNavigating = false;
        line.enabled = false;
        statusText.gameObject.SetActive(false);
        
        // Tắt mũi tên khi dừng chỉ đường
        if (currentArrow != null)
        {
            currentArrow.SetActive(false);
        }
    }

    void Update()
    {
        if (!isNavigating || target == null) return;

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path))
        {
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);
            
            // Cập nhật khoảng cách lặp (Tiling) để nét đứt không bị co giãn vỡ hạt
            UpdateLineMaterialTiling();
        }

        // Cập nhật vị trí và hiệu ứng cho mũi tên chỉ xuống
        if (currentArrow != null)
        {
            float bobbing = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount; 
            currentArrow.transform.position = target.position + Vector3.up * (arrowHeightOffset + bobbing);
        }
    }

    private void UpdateLineMaterialTiling()
    {
        if (line.positionCount < 2) return;

        // Tính tổng quãng đường để chia material cho đều
        float pathLength = 0f;
        for (int i = 1; i < line.positionCount; i++)
        {
            pathLength += Vector3.Distance(line.GetPosition(i - 1), line.GetPosition(i));
        }
        float tilingMultiplier = 7f;
        // Cập nhật Tiling của Material (Bạn có thể nhân pathLength với 1 hằng số để nét đứt thưa/dày hơn)
        line.material.SetTextureScale("_BaseMap", new Vector2(pathLength / tilingMultiplier, 1f));
    }
}