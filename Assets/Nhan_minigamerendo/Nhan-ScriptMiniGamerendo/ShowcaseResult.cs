using UnityEngine;
using TMPro;
using System.Collections;

public class ShowcaseResult : MonoBehaviour
{
    [Header("Cameras")]
    public GameObject mainCamera;       // Camera Quench
    public GameObject showcaseCamera;   // Camera Showcase

    [Header("Item & Locations")]
    public Transform itemSpot;          // Vị trí đặt Item (Nhớ gán GameObject vào đây nha!)
    
    [Header("UI Components")]
    public GameObject resultPanel;      // Cái bảng Showcase Panel
    public TextMeshProUGUI hammerText;  
    public TextMeshProUGUI heatText;
    public TextMeshProUGUI quenchText;
    public TextMeshProUGUI rankText;

    private GameObject spawnedItem; 

    // --- MỚI: Đảm bảo khi game chạy là tắt hết mấy cái của Showcase đi ---
    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (showcaseCamera != null) showcaseCamera.SetActive(false);
        if (rankText != null) rankText.gameObject.SetActive(false);
    }

    public void ShowResult(GameObject itemPrefab, float s1, float s2, float s3)
    {
        // 1. Reset trạng thái UI để chắc chắn nó đang tắt
        if (resultPanel != null) resultPanel.SetActive(false);

        // 2. Tạo Item mới từ Prefab
        if (itemPrefab != null)
        {
            // Xóa item cũ nếu lỡ có
            if (spawnedItem != null) Destroy(spawnedItem);
            
            // Kiểm tra itemSpot để tránh lỗi null như lúc nãy
            if (itemSpot == null) 
            {
                Debug.LogError("Chưa gán Item Spot! Tạo tạm tại vị trí hiện tại.");
                GameObject temp = new GameObject("TempSpot");
                temp.transform.position = transform.position;
                itemSpot = temp.transform;
            }

            spawnedItem = Instantiate(itemPrefab, itemSpot.position, itemSpot.rotation);
        }

        // 3. Đổi Camera (BỤP! Lúc này mới qua cảnh Showcase)
        if(mainCamera != null) mainCamera.SetActive(false);
        if(showcaseCamera != null) showcaseCamera.SetActive(true);

        // 4. Bắt đầu quy trình hiện UI
        StartCoroutine(ShowUISequence(s1, s2, s3));
    }

    IEnumerator ShowUISequence(float hammerScore, float heatScore, float quenchScore)
    {
        // Đợi 0.5s để người chơi nhìn thấy vật phẩm xoay xoay một chút
        yield return new WaitForSeconds(0.5f);

        // --- LÚC NÀY MỚI BẬT SHOWCASE PANEL ---
        if (resultPanel != null) resultPanel.SetActive(true);
        
        // Chạy số
        StartCoroutine(CountScoreAnimation(hammerText, hammerScore));
        StartCoroutine(CountScoreAnimation(heatText, heatScore));
        StartCoroutine(CountScoreAnimation(quenchText, quenchScore));

        yield return new WaitForSeconds(1f);
        
        float avg = (hammerScore + heatScore + quenchScore) / 3f;
        ShowRank(avg);
    }

    IEnumerator CountScoreAnimation(TextMeshProUGUI textObj, float targetScore)
    {
        if (textObj == null) yield break;

        float current = 0;
        float duration = 1.0f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            current = Mathf.Lerp(0, targetScore, elapsed / duration);
            textObj.text = current.ToString("F0") + "%";
            yield return null;
        }
        textObj.text = targetScore.ToString("F0") + "%";
    }

    void ShowRank(float average)
    {
        if (rankText == null) return;

        rankText.gameObject.SetActive(true);
        if (average >= 90) rankText.text = "<color=orange>S</color>";
        else if (average >= 70) rankText.text = "<color=green>A</color>";
        else rankText.text = "<color=white>B</color>";
    }

    void Update()
    {
        if (spawnedItem != null)
        {
            spawnedItem.transform.Rotate(Vector3.up * 15 * Time.deltaTime);
        }
    }
}