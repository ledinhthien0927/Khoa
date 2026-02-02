using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; 

public class ShowcaseResult : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject resultPanel;
    public TextMeshProUGUI damageText; 
    public TextMeshProUGUI rankText;   
    public TextMeshProUGUI bonusText;  

    [Header("End Game UI")]
    public GameObject endGameCanvas; // Canvas chứa nút Play Again & Home

    [Header("Setup Cameras")]
    public GameObject mainCamera;
    public GameObject showcaseCamera;

    // --- [MỚI] VẬT PHẨM ĐẶT SẴN TRONG SCENE ---
    // Kéo thả object Kiếm và Búa đã trang trí đẹp đẽ vào đây
    [Header("--- SHOWCASE ITEMS ---")]
    public GameObject swordObject;   // Object Kiếm
    public GameObject hammerObject;  // Object Búa
    
    private GameObject currentActiveItem; // Biến để lưu cái nào đang hiện (để xoay)
    // -------------------------------------------

    private const float LEGENDARY_THRESHOLD = 90f; 

    void Start()
    {
        // Ẩn UI kết thúc khi mới vào game
        if (endGameCanvas != null) endGameCanvas.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        // Ẩn cả 2 vật phẩm lúc đầu
        if (swordObject != null) swordObject.SetActive(false);
        if (hammerObject != null) hammerObject.SetActive(false);
    }

    // Lưu ý: Vẫn giữ tham số 'itemPrefab' để code không bị lỗi khi QuenchManager gọi sang
    // Nhưng bên trong ta sẽ không dùng nó, mà dùng Object đặt sẵn.
    public void ShowResult(GameObject itemPrefab, float baseDamageFromQuench, float hammerScore, float heatScore, float quenchScore)
    {
        // 1. Reset UI & Camera
        if (resultPanel != null) resultPanel.SetActive(false);
        if (bonusText != null) bonusText.gameObject.SetActive(false);
        if (damageText != null) damageText.text = "";

        if (endGameCanvas != null) endGameCanvas.SetActive(false);
        
        if (mainCamera != null) mainCamera.SetActive(false);
        if (showcaseCamera != null) showcaseCamera.SetActive(true);

        // --- [MỚI] LOGIC BẬT/TẮT VẬT PHẨM THEO LOẠI ---
        
        // Tắt hết trước
        if (swordObject != null) swordObject.SetActive(false);
        if (hammerObject != null) hammerObject.SetActive(false);

        // Bật cái đúng loại
        if (HeatingManager.CurrentWeaponType == WeaponType.Sword)
        {
            if (swordObject != null) swordObject.SetActive(true);
            currentActiveItem = swordObject;
        }
        else if (HeatingManager.CurrentWeaponType == WeaponType.Hammer)
        {
            if (hammerObject != null) hammerObject.SetActive(true);
            currentActiveItem = hammerObject;
        }
        // -----------------------------------------------

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

        // 4. Chạy Animation hiển thị kết quả
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

        yield return new WaitForSeconds(1.5f);
        
        if (endGameCanvas != null) 
        {
            endGameCanvas.SetActive(true);
        }
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
        // [MỚI] Chỉ xoay cái đang hiện
        if (currentActiveItem != null) 
        {
            currentActiveItem.transform.Rotate(Vector3.up * 20 * Time.deltaTime);
        }
    }

    // ========================================================
    //              CÁC HÀM XỬ LÝ NÚT BẤM
    // ========================================================

    public void OnPlayAgainClicked()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void OnCompleteClicked()
    {
        Debug.Log("Hoàn thành rèn! Đang cập nhật nhiệm vụ...");

        // 1. CẬP NHẬT TRẠNG THÁI NHIỆM VỤ
        if (QuestManager.Instance != null)
        {
            if (HeatingManager.CurrentWeaponType == WeaponType.Sword)
            {
                // Rèn kiếm xong -> Báo nhiệm vụ Kiếm hoàn tất (Completed)
                QuestManager.Instance.SetPrince(PrinceQuestState.Completed);
            }
            else 
            {
                // Rèn khác xong -> Báo nhiệm vụ Thuyền hoàn tất (ShipDone)
                QuestManager.Instance.SetPrince(PrinceQuestState.ShipDone);
            }
        }

        // 2. [MỚI] TỰ ĐỘNG BẬT DẪN ĐƯỜNG VỀ HOÀNG TỬ
        // Lúc này QuestManager đã update UI sang mục tiêu "Hoàng tử"
        // Ta gọi hàm này để mũi tên tự hiện ra chỉ về hướng Hoàng tử
        if (QuestUIManager.Instance != null)
        {
            QuestUIManager.Instance.AutoClickMainQuest();
        }

        // 3. THOÁT CHẾ ĐỘ RÈN (Về lại góc nhìn nhân vật)
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.ExitSmithingMode();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // 4. HỦY UI MINIGAME
        Destroy(transform.root.gameObject);
    }
}