using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class HeatingManager : MonoBehaviour
{
    public static HeatingManager Instance;

    [Header("UI References")]
    public GameObject heatingCanvas; 
    
    [Header("UI - Controls")]
    public HoldButton heatButtonScript; 
    public Button dropIngotButton;      
    public GameObject dropButtonObj;    
[Header("Effects")]
public float soundMultiplier = 1.0f; // MỚI: Chỉnh số này lên 2 hoặc 3 để to hơn
public float minSoundVolume = 0.2f;
    [Header("UI - Indicators")]
    public Slider tempSlider;        
    public Slider masterySlider;     
    public TextMeshProUGUI timeText; 
    public TextMeshProUGUI rankText; 

    [Header("Cinematic & Visuals")]
    public GameObject mainCamera;       
    public GameObject furnaceCamera;    
    public GameObject playerModelObject;
    public Animator playerAnim;         
    
    [Header("VFX & Audio")]
    public ParticleSystem fallingItemsVFX; 
    public Transform dropPoint;            
    public AudioSource dropAudioSource;    
    public AudioClip dropSoundClip;
    
    [Space(10)]
    // --- CÁI MỚI: Kéo VFX hoàn thành vào đây ---
    public ParticleSystem completionVFX; 
    // -------------------------------------------

    public GameObject rawIngotObject;   
    public Renderer ingotRenderer;      
    
    [Header("Effects")]
    public Gradient heatColorGradient; 
    public ParticleSystem fireEffect;
    public AudioSource sizzleAudio;

    [Header("CƠ CHẾ LỬA THEO VÙNG (HEAT ZONES)")]
    public float coolSpeed = 0.3f;          
    public float minTempToProcess = 0.3f;
    public float baseProgressSpeed = 0.2f; 

    [Space(10)]
    [Header("--- Cấu hình 3 Giai đoạn Lửa ---")]
    public float speedLow = 0.3f;  
    public float speedMedium = 0.6f; 
    public float speedHigh = 1.5f; 

    [Header("Time & Ranking Settings")]
    public float timeLimit = 90f; 
    // Các mốc thời gian để xét Rank
    public float rankPerfect = 25f;  // Dưới 25s là Huyền Thoại
    public float rankGood = 35f;     // Dưới 35s là Tốt
    public float rankNormal = 60f;   // Dưới 60s là Trung Bình

    // Biến trạng thái
    private float currentTemp = 0f;      
    private float currentProgress = 0f;  
    private float elapsedTime = 0f;
    
    private bool isHeatingActive = false;
    private bool isGameOver = false;
    private bool hasIngot = false;       
    private bool isCinematic = false;    
    private bool timerStarted = false;   

    void Awake() { Instance = this; }

    void Start()
    {
        if (dropIngotButton != null) 
            dropIngotButton.onClick.AddListener(() => StartCoroutine(DropIngotSequence()));
        
        if (playerModelObject != null) playerModelObject.SetActive(false);
        if (fallingItemsVFX != null) fallingItemsVFX.Stop();
        // Đảm bảo VFX hoàn thành tắt lúc đầu
        if (completionVFX != null) completionVFX.Stop(); 
    }

    public void StartHeatingPhase()
    {
        isHeatingActive = true;
        isGameOver = false;
        hasIngot = false;     
        isCinematic = false;
        timerStarted = false; 
        
        currentTemp = 0f;
        currentProgress = 0f;
        elapsedTime = 0f;

        if (heatingCanvas != null) heatingCanvas.SetActive(true);
        if (dropButtonObj != null) dropButtonObj.SetActive(true); 
        
        if (tempSlider != null) tempSlider.value = 0f;
        if (masterySlider != null) masterySlider.value = 0f;
        
        // Reset rank text
        if (rankText != null) {
            rankText.text = "";
            rankText.color = Color.white;
        }
        if (timeText != null) timeText.text = "Time: 0.0s"; 
        
        if (mainCamera != null) mainCamera.SetActive(true);
        if (furnaceCamera != null) furnaceCamera.SetActive(false); 
        if (rawIngotObject != null) rawIngotObject.SetActive(false); 
        
        if (SmithingManager.Instance != null && SmithingManager.Instance.smithingCanvas != null)
        {
            SmithingManager.Instance.smithingCanvas.SetActive(false);
        }
        
        if (sizzleAudio != null) { sizzleAudio.volume = 0; sizzleAudio.Play(); }
    }

    void Update()
    {
        if (!isHeatingActive || isGameOver || isCinematic) return;

        // Chỉ tính giờ khi biến timerStarted = true
        if (timerStarted)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
            if (elapsedTime > timeLimit) { FailGame("QUÁ LÂU! SẮT ĐÃ HỎNG!"); return; }
        }

        bool isHolding = (heatButtonScript != null && heatButtonScript.isPressed);

        if (isHolding) 
        {
            // Chỉ bắt đầu tính giờ nếu chưa bắt đầu VÀ ĐÃ CÓ PHÔI (hasIngot)
            if (!timerStarted && hasIngot) 
            {
                timerStarted = true; 
            }

            float currentSpeed = GetCurrentHeatSpeed();
            currentTemp += currentSpeed * Time.deltaTime;

            if (fireEffect != null && !fireEffect.isPlaying) fireEffect.Play();
        } 
        else 
        {
            currentTemp -= coolSpeed * Time.deltaTime;
            if (fireEffect != null) fireEffect.Stop();
        }
        currentTemp = Mathf.Clamp01(currentTemp);

        if (currentTemp >= 1.0f) { FailGame("QUÁ NHIỆT! PHÔI TAN CHẢY!"); return; }

        if (hasIngot && currentTemp > minTempToProcess)
        {
            float bonusSpeed = currentTemp * 2.5f; 
            currentProgress += baseProgressSpeed * bonusSpeed * Time.deltaTime;
        }

        if (currentProgress >= 1.0f) 
        {
            FinishHeating();
        }

        UpdateVisuals();
    }

    float GetCurrentHeatSpeed()
    {
        if (currentTemp > 0.8f) return speedHigh;
        if (currentTemp > 0.5f) return speedMedium;
        return speedLow;
    }

    IEnumerator DropIngotSequence()
    {
        isCinematic = true; 
        if (dropButtonObj != null) dropButtonObj.SetActive(false); 
        if (mainCamera != null) mainCamera.SetActive(false);
        if (furnaceCamera != null) furnaceCamera.SetActive(true);
        
        yield return new WaitForSeconds(0.5f); 
        
        if (playerModelObject != null) {
            playerModelObject.SetActive(true); 
            if (playerAnim != null) playerAnim.SetTrigger("DropIngot");
        }
        
        yield return new WaitForSeconds(0.5f);
        
        if (fallingItemsVFX != null) {
            if (dropPoint != null) fallingItemsVFX.transform.position = dropPoint.position;
            fallingItemsVFX.Play(); 
        }
        if (dropAudioSource != null && dropSoundClip != null) dropAudioSource.PlayOneShot(dropSoundClip);
        
        yield return new WaitForSeconds(3.0f);
        
        if (rawIngotObject != null) rawIngotObject.SetActive(true);
        if (playerModelObject != null) playerModelObject.SetActive(false);
        if (fallingItemsVFX != null) fallingItemsVFX.Stop(); 
        if (furnaceCamera != null) furnaceCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);
        
        hasIngot = true;     
        isCinematic = false; 
    }

    void UpdateTimerUI() { if (timeText != null) timeText.text = $"Time: {elapsedTime:F1}s"; }
    
    void UpdateVisuals() 
{ 
    if (tempSlider != null) tempSlider.value = currentTemp; 
    if (masterySlider != null) masterySlider.value = currentProgress; 

    if (ingotRenderer != null && hasIngot) {
        ingotRenderer.material.color = heatColorGradient.Evaluate(currentTemp);
        ingotRenderer.material.SetColor("_EmissionColor", heatColorGradient.Evaluate(currentTemp) * 2f); 
    }

    // --- SỬA ĐOẠN ÂM THANH NÀY ---
    if (sizzleAudio != null)
    {
        // Pitch (Độ cao): Sôi càng to tiếng càng rít
        sizzleAudio.pitch = 0.8f + currentTemp * 0.5f; 

        // Volume (Độ to): 
        // Công thức: (Nền tối thiểu + Nhiệt độ) * Hệ số nhân
        float newVol = (minSoundVolume + currentTemp) * soundMultiplier;

        // Đảm bảo không vượt quá 1.0 (Unity clamp volume từ 0 đến 1)
        sizzleAudio.volume = Mathf.Clamp01(newVol);     
    }
}

    // --- HÀM ĐƯỢC NÂNG CẤP ---
    void FinishHeating()
    {
        if (!isHeatingActive) return; 

        isHeatingActive = false; // Dừng game logic

        // 1. Tính toán Rank dựa trên thời gian
        string grade = "";
        Color gradeColor = Color.white;

        if (elapsedTime <= rankPerfect) { grade = "HUYỀN THOẠI (LEGENDARY)!"; gradeColor = Color.cyan; }
        else if (elapsedTime <= rankGood) { grade = "TỐT (GOOD)"; gradeColor = Color.green; }
        else if (elapsedTime <= rankNormal) { grade = "TRUNG BÌNH (NORMAL)"; gradeColor = Color.yellow; }
        else { grade = "TỆ (BAD)"; gradeColor = new Color(1f, 0.5f, 0f); } // Màu cam

        // 2. Hiện Rank lên UI
        if (rankText != null)
        {
            rankText.text = grade;
            rankText.color = gradeColor;
        }

        // 3. Chạy VFX hoàn thành
        if (completionVFX != null)
        {
            // Đặt vị trí VFX ngay tại cục phôi cho đẹp
            if (rawIngotObject != null) completionVFX.transform.position = rawIngotObject.transform.position;
            completionVFX.Play();
        }
        
        // 4. Dừng các hiệu ứng đang chạy
        if (fireEffect != null) fireEffect.Stop();
        if (sizzleAudio != null) sizzleAudio.Stop();

        Debug.Log($"Nung xong! Thời gian: {elapsedTime:F1}s - Rank: {grade}");

        // 5. Đợi 2.5 giây để ngắm Rank và VFX rồi mới chuyển cảnh
        Invoke("GoToSmithing", 2.5f);
    }
    // -------------------------

    void GoToSmithing()
    {
        if (heatingCanvas != null) heatingCanvas.SetActive(false);
        if (SmithingManager.Instance != null) SmithingManager.Instance.StartSmithingPhase();
        else Debug.LogError("LỖI: Không tìm thấy SmithingManager!");
    }

    void FailGame(string reason)
    {
        isGameOver = true;
        if (rankText != null) { rankText.text = reason; rankText.color = Color.red; }
        Debug.Log("Thất bại: " + reason);
        Invoke("StartHeatingPhase", 2.5f); // Tăng thời gian chờ khi thua lên xíu
    }
}