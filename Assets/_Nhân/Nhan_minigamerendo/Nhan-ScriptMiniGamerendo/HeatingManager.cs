using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class HeatingManager : MonoBehaviour
{
    public static HeatingManager Instance;

    [Header("UI References - Main")]
    public GameObject heatingCanvas; 
    public GameObject otherCanvasToHide; 
    
    [Header("--- TUTORIAL SYSTEM ---")]
    public GameObject tutorialPanel;        
    public GameObject[] tutorialPages;      
    public Button nextBtn;                  
    public Button prevBtn;                  
    public Button closeBtn;                 
    public Button helpBtn;                  
    
    [Header("--- PHASE BUTTONS ---")]
    public Button dropIngotButton;          
    public Button startPhaseButton;         
    
    [Header("UI - Controls (Gameplay)")]
    public HoldButton heatButtonScript;     
    public GameObject gameplayUIContainer;  
    
    [Header("Effects")]
    public float soundMultiplier = 1.0f; 
    public float minSoundVolume = 0.2f;
    
    [Header("UI - Indicators")]
    public Slider tempSlider;        
    public Slider masterySlider;     
    public TextMeshProUGUI timeText; 
    public TextMeshProUGUI rankText; 
    public static bool IsLegendary = false;
    
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
    public ParticleSystem completionVFX; 

    public GameObject rawIngotObject;   
    public Renderer ingotRenderer;      
    
    [Header("Effects - Fire & Heat")]
    public Gradient heatColorGradient; 
    public ParticleSystem fireEffect;
    public AudioSource sizzleAudio;

    // --- [MỚI] BIẾN CHỈNH ĐỘ TO NHỎ CỦA LỬA ---
    [Header("--- FIRE SCALE SETTINGS ---")]
    public float minFireScale = 0.5f; // Kích thước lửa khi nhiệt độ thấp/mới bấm
    public float maxFireScale = 3.0f; // Kích thước lửa khi nhiệt độ cao (Max)
    // ---------------------------------------------

    [Header("CƠ CHẾ LỬA THEO VÙNG")]
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
    public float rankPerfect = 25f;  
    public float rankGood = 35f;     
    public float rankNormal = 60f;   

    // Các trạng thái game
    public enum GameState { 
        IntroTutorial,      // Xem Page 1
        ReadyToDrop,        // Chờ bấm Bỏ Phôi
        Dropping,           // Cinematic
        PostDropTutorial,   // Xem Page 2 (Sau khi bỏ phôi)
        ReadyToStartHeat,   // Chờ bấm Nung
        HeatTutorial,       // Xem Page 3 (Sau khi bấm Nung)
        Playing,            // Đang chơi
        Finished 
    }
    private GameState currentState;

    private float currentTemp = 0f;      
    private float currentProgress = 0f;  
    private float elapsedTime = 0f;
    private bool timerStarted = false;   
    
    private int currentPageIndex = 0;
    
    // Biến kiểm soát chế độ xem Tutorial
    private bool isSinglePageMode = false; // True = Chỉ xem 1 trang, False = Xem tự do (nút Help)

    // Biến cờ (Flag) để nhớ đã xem chưa
    private bool hasSeenIntro = false;
    private bool hasSeenPostDrop = false;
    private bool hasSeenHeatGuide = false;

    void Awake() { Instance = this; }

    IEnumerator Start()
    {
        // 1. Gán sự kiện
        if (dropIngotButton != null) dropIngotButton.onClick.AddListener(() => StartCoroutine(DropIngotSequence()));
        if (startPhaseButton != null) startPhaseButton.onClick.AddListener(OnStartPhaseClicked);
        
        if (nextBtn != null) nextBtn.onClick.AddListener(OnNextPage);
        if (prevBtn != null) prevBtn.onClick.AddListener(OnPrevPage);
        if (closeBtn != null) closeBtn.onClick.AddListener(OnCloseTutorial);
        if (helpBtn != null) helpBtn.onClick.AddListener(OnOpenTutorialManual);

        // 2. Setup ban đầu
        if (playerModelObject != null) playerModelObject.SetActive(false);
        if (fallingItemsVFX != null) fallingItemsVFX.Stop();
        if (completionVFX != null) completionVFX.Stop(); 

        yield return null; 
        
        StartHeatingPhase();
    }

    public void StartHeatingPhase()
    {
        // --- Setup Canvas ---
        if (heatingCanvas != null) heatingCanvas.SetActive(true);
        if (otherCanvasToHide != null) otherCanvasToHide.SetActive(false);
        if (SmithingManager.Instance != null && SmithingManager.Instance.smithingCanvas != null)
            SmithingManager.Instance.smithingCanvas.SetActive(false);

        // Reset Gameplay
        currentTemp = 0f;
        currentProgress = 0f;
        elapsedTime = 0f;
        timerStarted = false;
        
        if (tempSlider != null) tempSlider.value = 0f;
        if (masterySlider != null) masterySlider.value = 0f;
        if (rankText != null) { rankText.text = ""; rankText.color = Color.white; }
        if (timeText != null) timeText.text = "Time: 0.0s"; 

        if (mainCamera != null) mainCamera.SetActive(true);
        if (furnaceCamera != null) furnaceCamera.SetActive(false); 
        if (rawIngotObject != null) rawIngotObject.SetActive(false); 
        if (sizzleAudio != null) { sizzleAudio.volume = 0; sizzleAudio.Play(); }

        // [MỚI] Reset lửa về nhỏ nhất khi bắt đầu
        if (fireEffect != null) fireEffect.transform.localScale = Vector3.one * minFireScale;

        // Ẩn hết UI Gameplay
        if (dropIngotButton != null) dropIngotButton.gameObject.SetActive(false);
        if (startPhaseButton != null) startPhaseButton.gameObject.SetActive(false);
        if (gameplayUIContainer != null) gameplayUIContainer.SetActive(false);
        if (heatButtonScript != null) heatButtonScript.gameObject.SetActive(false);
        if (helpBtn != null) helpBtn.gameObject.SetActive(false); 

        // --- GIAI ĐOẠN 1: HIỆN PAGE 1 (INTRO) ---
        currentState = GameState.IntroTutorial;
        ShowSpecificTutorialPage(0); // Index 0 = Page 1
    }

    // =========================================================
    //              TUTORIAL LOGIC
    // =========================================================

    void ShowSpecificTutorialPage(int pageIndex)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        
        currentPageIndex = pageIndex;
        if (currentPageIndex >= tutorialPages.Length) currentPageIndex = tutorialPages.Length - 1;
        if (currentPageIndex < 0) currentPageIndex = 0;

        isSinglePageMode = true; 
        UpdateTutorialUI();

        if (helpBtn != null) helpBtn.gameObject.SetActive(false);
    }

    void OnOpenTutorialManual()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        currentPageIndex = 0; 
        isSinglePageMode = false; 
        UpdateTutorialUI();
    }

    void OnCloseTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        switch (currentState)
        {
            case GameState.IntroTutorial:
                hasSeenIntro = true;
                currentState = GameState.ReadyToDrop;
                if (dropIngotButton != null) dropIngotButton.gameObject.SetActive(true);
                if (helpBtn != null) helpBtn.gameObject.SetActive(true);
                break;

            case GameState.PostDropTutorial:
                hasSeenPostDrop = true;
                currentState = GameState.ReadyToStartHeat;
                if (startPhaseButton != null) startPhaseButton.gameObject.SetActive(true);
                if (helpBtn != null) helpBtn.gameObject.SetActive(true);
                break;

            case GameState.HeatTutorial:
                hasSeenHeatGuide = true;
                StartGameplay();
                if (helpBtn != null) helpBtn.gameObject.SetActive(true);
                break;
        }
    }

    void UpdateTutorialUI()
    {
        for (int i = 0; i < tutorialPages.Length; i++)
        {
            if (tutorialPages[i] != null) tutorialPages[i].SetActive(i == currentPageIndex);
        }

        if (isSinglePageMode)
        {
            if (nextBtn != null) nextBtn.gameObject.SetActive(false);
            if (prevBtn != null) prevBtn.gameObject.SetActive(false);
        }
        else
        {
            if (nextBtn != null) 
            {
                nextBtn.gameObject.SetActive(true);
                nextBtn.interactable = (currentPageIndex < tutorialPages.Length - 1);
            }
            if (prevBtn != null) 
            {
                prevBtn.gameObject.SetActive(true);
                prevBtn.interactable = (currentPageIndex > 0);
            }
        }
    }

    void OnNextPage()
    {
        if (isSinglePageMode) return; 
        if (currentPageIndex < tutorialPages.Length - 1) { currentPageIndex++; UpdateTutorialUI(); }
    }

    void OnPrevPage()
    {
        if (isSinglePageMode) return; 
        if (currentPageIndex > 0) { currentPageIndex--; UpdateTutorialUI(); }
    }

    // =========================================================
    //              FLOW & GAMEPLAY
    // =========================================================

    IEnumerator DropIngotSequence()
    {
        currentState = GameState.Dropping;
        if (dropIngotButton != null) dropIngotButton.gameObject.SetActive(false);
        if (helpBtn != null) helpBtn.gameObject.SetActive(false); 

        // Cinematic
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

        if (!hasSeenPostDrop)
        {
            currentState = GameState.PostDropTutorial;
            ShowSpecificTutorialPage(1); 
        }
        else
        {
            currentState = GameState.ReadyToStartHeat;
            if (startPhaseButton != null) startPhaseButton.gameObject.SetActive(true);
            if (helpBtn != null) helpBtn.gameObject.SetActive(true);
        }
    }

    void OnStartPhaseClicked()
    {
        if (startPhaseButton != null) startPhaseButton.gameObject.SetActive(false);

        if (!hasSeenHeatGuide)
        {
            currentState = GameState.HeatTutorial;
            ShowSpecificTutorialPage(2); 
        }
        else
        {
            StartGameplay();
        }
    }

    void StartGameplay()
    {
        currentState = GameState.Playing;
        if (gameplayUIContainer != null) gameplayUIContainer.SetActive(true);
        if (heatButtonScript != null) heatButtonScript.gameObject.SetActive(true);
    }

    void Update()
    {
        if (currentState != GameState.Playing) return;

        if (timerStarted)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
            if (elapsedTime > timeLimit) { FailGame("QUÁ LÂU! SẮT ĐÃ HỎNG!"); return; }
        }

        bool isHolding = (heatButtonScript != null && heatButtonScript.isPressed);

        if (isHolding) 
        {
            if (!timerStarted) timerStarted = true; 
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

        if (currentTemp > minTempToProcess)
        {
            float bonusSpeed = currentTemp * 2.5f; 
            currentProgress += baseProgressSpeed * bonusSpeed * Time.deltaTime;
        }

        if (currentProgress >= 1.0f) FinishHeating();

        UpdateVisuals();
    }

    float GetCurrentHeatSpeed()
    {
        if (currentTemp > 0.8f) return speedHigh;
        if (currentTemp > 0.5f) return speedMedium;
        return speedLow;
    }

    void UpdateTimerUI() { if (timeText != null) timeText.text = $"Time: {elapsedTime:F1}s"; }
    
    void UpdateVisuals() 
    { 
        if (tempSlider != null) tempSlider.value = currentTemp; 
        if (masterySlider != null) masterySlider.value = currentProgress; 
        if (ingotRenderer != null) {
            ingotRenderer.material.color = heatColorGradient.Evaluate(currentTemp);
            ingotRenderer.material.SetColor("_EmissionColor", heatColorGradient.Evaluate(currentTemp) * 2f); 
        }
        if (sizzleAudio != null) {
            sizzleAudio.pitch = 0.8f + currentTemp * 0.5f; 
            float newVol = (minSoundVolume + currentTemp) * soundMultiplier;
            sizzleAudio.volume = Mathf.Clamp01(newVol);     
        }

        // --- [MỚI] XỬ LÝ SIZE LỬA ---
        if (fireEffect != null)
        {
            // Lerp từ MinScale đến MaxScale dựa trên currentTemp (0.0 -> 1.0)
            float targetScale = Mathf.Lerp(minFireScale, maxFireScale, currentTemp);
            fireEffect.transform.localScale = Vector3.one * targetScale;
        }
        // ---------------------------
    }

    void FinishHeating()
    {
        currentState = GameState.Finished;
        if (gameplayUIContainer != null) gameplayUIContainer.SetActive(false);
        if (heatButtonScript != null) heatButtonScript.gameObject.SetActive(false);

        string grade = "";
        Color gradeColor = Color.white;

        if (elapsedTime <= rankPerfect) { grade = "HUYỀN THOẠI (LEGENDARY)!"; gradeColor = Color.cyan;IsLegendary = true; }
        else if (elapsedTime <= rankGood) { grade = "TỐT (GOOD)"; gradeColor = Color.green;IsLegendary = false; }
        else if (elapsedTime <= rankNormal) { grade = "TRUNG BÌNH (NORMAL)"; gradeColor = Color.yellow; IsLegendary = false;}
        else { grade = "TỆ (BAD)"; gradeColor = new Color(1f, 0.5f, 0f); IsLegendary = false;}

        if (rankText != null) { rankText.text = grade; rankText.color = gradeColor; }
        if (completionVFX != null) {
            if (rawIngotObject != null) completionVFX.transform.position = rawIngotObject.transform.position;
            completionVFX.Play();
        }
        if (fireEffect != null) fireEffect.Stop();
        if (sizzleAudio != null) sizzleAudio.Stop();
        Debug.Log($"Nung xong! Thời gian: {elapsedTime:F1}s - Rank: {grade}");
        Invoke("GoToSmithing", 2.5f);
    }

    void GoToSmithing()
    {
        if (heatingCanvas != null) heatingCanvas.SetActive(false);
        if (SmithingManager.Instance != null) SmithingManager.Instance.StartSmithingPhase();
        else Debug.LogError("LỖI: Không tìm thấy SmithingManager!");
    }

    void FailGame(string reason)
    {
        currentState = GameState.Finished;
        if (rankText != null) { rankText.text = reason; rankText.color = Color.red; }
        Debug.Log("Thất bại: " + reason);
        Invoke("StartHeatingPhase", 2.5f); 
    }
}