using UnityEngine;
using TMPro;
using System.Collections;

public class ShowcaseResult : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject resultPanel;
    public TextMeshProUGUI damageText; 
    public TextMeshProUGUI rankText;   
    public TextMeshProUGUI bonusText;  

    // --- MỚI: Biến Canvas/Panel chứa nút bấm (Play Again, Home...) ---
    public GameObject endGameCanvas; 
    // ----------------------------------------------------------------

    [Header("Setup")]
    public GameObject mainCamera;
    public GameObject showcaseCamera;
    public Transform itemSpot;
    private GameObject spawnedItem;

    private const float LEGENDARY_THRESHOLD = 90f; 

    // Đảm bảo lúc game bắt đầu thì tắt cái EndGameCanvas đi
    void Start()
    {
        if (endGameCanvas != null) endGameCanvas.SetActive(false);
    }

    public void ShowResult(GameObject itemPrefab, float baseDamageFromQuench, float hammerScore, float heatScore, float quenchScore)
    {
        // 1. Reset UI & Camera
        if (resultPanel != null) resultPanel.SetActive(false);
        if (bonusText != null) bonusText.gameObject.SetActive(false);
        if (damageText != null) damageText.text = "";

        // --- MỚI: Ẩn Canvas kết thúc (đề phòng nó đang bật) ---
        if (endGameCanvas != null) endGameCanvas.SetActive(false);
        // -----------------------------------------------------
        
        if (mainCamera != null) mainCamera.SetActive(false);
        if (showcaseCamera != null) showcaseCamera.SetActive(true);

        // 2. Spawn vũ khí
        if (spawnedItem != null) Destroy(spawnedItem);
        if (itemSpot != null && itemPrefab != null) 
            spawnedItem = Instantiate(itemPrefab, itemSpot.position, itemSpot.rotation);

        // 3. Tính toán Dame & Rank
        float finalDamage = baseDamageFromQuench; 
        bool isBonusActive = false;

        if (hammerScore >= LEGENDARY_THRESHOLD || heatScore >= LEGENDARY_THRESHOLD)
        {
            float bonusAmount = baseDamageFromQuench * 0.1f; 
            finalDamage += bonusAmount; 
            isBonusActive = true;
        }

        float averageSkillScore = (hammerScore + heatScore + quenchScore) / 3f;

        // 4. Chạy Animation
        StartCoroutine(RunShowcaseSequence(finalDamage, averageSkillScore, isBonusActive));
    }

    IEnumerator RunShowcaseSequence(float damageVal, float rankScore, bool hasBonus)
    {
        yield return new WaitForSeconds(0.5f);
        if (resultPanel != null) resultPanel.SetActive(true);

        if (hasBonus && bonusText != null)
        {
            bonusText.gameObject.SetActive(true);
            bonusText.text = "+10% LEGENDARY BONUS";
        }

        yield return StartCoroutine(CountNumber(damageText, damageVal));

        yield return new WaitForSeconds(0.5f);

        if (rankText != null)
        {
            rankText.gameObject.SetActive(true);
            if (rankScore >= 90) rankText.text = "<color=orange>S</color>";
            else if (rankScore >= 70) rankText.text = "<color=green>A</color>";
            else rankText.text = "<color=white>B</color>";
        }

        // --- MỚI: Đợi 1 giây cho người chơi ngắm Rank rồi bật Canvas kết thúc ---
        yield return new WaitForSeconds(1.0f);
        
        if (endGameCanvas != null) 
        {
            endGameCanvas.SetActive(true);
            // Gợi ý: Có thể thêm âm thanh "Victory" hoặc tiếng nhạc kết thúc ở đây
        }
        // ----------------------------------------------------------------------
    }

    IEnumerator CountNumber(TextMeshProUGUI textRef, float target)
    {
        float current = 0;
        float duration = 1f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            current = Mathf.Lerp(0, target, elapsed / duration);
            textRef.text = Mathf.RoundToInt(current).ToString() + " DMG";
            yield return null;
        }
        textRef.text = Mathf.RoundToInt(target).ToString() + " DMG";
    }

    void Update()
    {
        if (spawnedItem != null) spawnedItem.transform.Rotate(Vector3.up * 10 * Time.deltaTime);
    }
}