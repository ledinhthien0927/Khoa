using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class QuenchManager : MonoBehaviour
{
    public static QuenchManager Instance;

    [Header("Connection")]
    public ShowcaseResult showcaseManager; 
    
    [Header("Item Data")]
    // Kéo Prefab vật phẩm hoàn chỉnh vào đây (Ví dụ: Prefab cây kiếm đẹp)
    public GameObject finishedItemPrefab; 

    public float hammerScore = 85f; 
    public float heatScore = 90f;

    [Header("UI References")]
    public GameObject quenchCanvas;
    
    [Header("Cinematic & Camera")]
    public GameObject mainCamera;    
    public GameObject quenchCamera;  
    public GameObject tongsAndItem;  
    public Animator tongsAnimator;   
    public TextMeshProUGUI countdownText; 
    
    // --- QUAN TRỌNG: Cần bật lại cái này để hiện Rank lúc chờ 2s ---
    public TextMeshProUGUI localRankText; 

    [Header("UI References - Minigame")]
    public RectTransform barBG;       
    public RectTransform cursor;      
    public RectTransform targetZone;  
    public GameObject quenchPanel;    
    public AudioSource hissAudio;
    public AudioSource countAudio;    

    [Header("Game Settings")]
    public float moveSpeed = 300f;    
    public int requiredSuccess = 3;   

    [Header("Visuals")]
    public Renderer ingotRenderer;    
    public ParticleSystem steamEffect;

    // Biến nội bộ
    private bool movingRight = true;
    private int currentSuccess = 0;
    private int currentScore = 100; 
    private bool isGameActive = false; 
    private bool readyToQuench = false; 
    private Color hotColor = Color.red; 
    private Color coolColor = Color.black; 

    void Awake() { Instance = this; }

    void Start()
    {
        if (quenchPanel != null) quenchPanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (localRankText != null) localRankText.gameObject.SetActive(false); // Ẩn rank text ban đầu
        
        if (quenchCamera != null) quenchCamera.SetActive(false);
        if (tongsAndItem != null) tongsAndItem.SetActive(false);
    }

    // ... (Giữ nguyên phần StartQuenchSequence, Update, MoveCursor, CheckHit ...)

    public void StartQuenchSequence()
    {
        // ... (Giữ nguyên code cũ của bạn) ...
        if (mainCamera != null) mainCamera.SetActive(false);         
        if (quenchCamera != null) quenchCamera.SetActive(true);
        if (quenchCanvas != null) quenchCanvas.SetActive(true);
        if (tongsAndItem != null) tongsAndItem.SetActive(true);
        if (ingotRenderer != null) ingotRenderer.material.color = hotColor;
        readyToQuench = true;
        currentScore = 100; 
    }

    void Update()
    {
        // ... (Giữ nguyên code cũ của bạn) ...
        // GIAI ĐOẠN 1: CHỜ SPACE ĐẦU TIÊN
        if (readyToQuench) {
            if (Input.GetKeyDown(KeyCode.Space)) {
                readyToQuench = false; 
                if (tongsAnimator != null) tongsAnimator.SetTrigger("Dig");
                StartCoroutine(CountdownRoutine());
            }
            return; 
        }

        // GIAI ĐOẠN 2: GAMEPLAY
        if (!isGameActive) return;

        MoveCursor();

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (tongsAnimator != null) tongsAnimator.SetTrigger("Dig"); 
            CheckHit();
        }
    }

    IEnumerator CountdownRoutine()
    {
        // ... (Giữ nguyên code cũ của bạn) ...
        yield return new WaitForSeconds(1.0f);
        if (countdownText != null) {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "3"; yield return new WaitForSeconds(1.0f);
            countdownText.text = "2"; yield return new WaitForSeconds(1.0f);
            countdownText.text = "1"; yield return new WaitForSeconds(1.0f);
            countdownText.gameObject.SetActive(false);
        }
        StartActualGame();
    }

    void StartActualGame()
    {
        // ... (Giữ nguyên code cũ của bạn) ...
        isGameActive = true;
        currentSuccess = 0;
        if (quenchPanel != null) quenchPanel.SetActive(true);
    }
    
    void MoveCursor() { /*... Giữ nguyên ...*/ float limitX = (barBG.rect.width / 2) - (cursor.rect.width / 2); float currentX = cursor.anchoredPosition.x; if (movingRight) { currentX += moveSpeed * Time.deltaTime; if (currentX >= limitX) movingRight = false; } else { currentX -= moveSpeed * Time.deltaTime; if (currentX <= -limitX) movingRight = true; } cursor.anchoredPosition = new Vector2(currentX, 0); }

    void CheckHit()
    {
        float dist = Mathf.Abs(cursor.anchoredPosition.x - targetZone.anchoredPosition.x);
        
        if (dist <= targetZone.rect.width / 2) 
        {
            currentSuccess++;
            PlayEffects(); 
            
            if (ingotRenderer != null) {
                 float p = (float)currentSuccess / requiredSuccess;
                 ingotRenderer.material.color = Color.Lerp(hotColor, coolColor, p);
            }

            // --- SỬA Ở ĐÂY: Gọi Coroutine thay vì hàm void ---
            if (currentSuccess >= requiredSuccess) StartCoroutine(WinGameSequence());
            else moveSpeed += 50f;
        }
        else
        {
            currentScore -= 10; 
            Debug.Log("Miss! Điểm còn: " + currentScore);
        }
    }

    void PlayEffects() { if(hissAudio) hissAudio.PlayOneShot(hissAudio.clip); StartCoroutine(PlaySteamDelayed()); }
    IEnumerator PlaySteamDelayed() { yield return new WaitForSeconds(1f); if(steamEffect) steamEffect.Play(); }

    // --- LOGIC MỚI: Xử lý delay và swap item ---
    IEnumerator WinGameSequence() 
    { 
        isGameActive = false; 

        // 1. Tính toán điểm trung bình để hiện Rank tạm
        float avgScore = (hammerScore + heatScore + currentScore) / 3f;
        string rankString = "B";
        Color rankColor = Color.white;

        if (avgScore >= 90) { rankString = "S"; rankColor = Color.yellow; } // Orange/Gold
        else if (avgScore >= 70) { rankString = "A"; rankColor = Color.green; }

        // 2. Hiện Rank lên UI của Quench để người chơi thấy
        if (localRankText != null)
        {
            localRankText.text = rankString;
            localRankText.color = rankColor;
            localRankText.gameObject.SetActive(true);
            
            // Có thể thêm hiệu ứng scale text ở đây cho đẹp
        }

        // 3. CHỜ 2 GIÂY (Theo yêu cầu)
        yield return new WaitForSeconds(2.0f);

        // --- Bắt đầu chuyển cảnh ---

        // 4. Dọn dẹp hiện trường Quench (Ẩn kìm, ẩn phôi sắt)
        if (quenchCanvas != null) quenchCanvas.SetActive(false); 
        if (tongsAndItem != null) tongsAndItem.SetActive(false); 
        
        // Lưu ý: Ta không dùng ingotRenderer (phôi sắt) nữa vì sẽ spawn item mới
        
        // 5. GỌI SHOWCASE
        if (showcaseManager != null)
        {
            // Truyền: Prefab thành phẩm, Các điểm số
            showcaseManager.ShowResult(finishedItemPrefab, hammerScore, heatScore, (float)currentScore);
        }
    }
}