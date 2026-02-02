using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MiniGame3GameManager : MonoBehaviour
{
    // === SINGLETON ===
    public static MiniGame3GameManager Instance { get; private set; }
    
    // === UI REFERENCES ===
    [Header("Energy Bars")]
    [SerializeField] private Image lightBar;
    [SerializeField] private Image darkBar;
    [SerializeField] private TMP_Text lightPercentText;
    [SerializeField] private TMP_Text darkPercentText;
    
    [Header("Game Info")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text gameStateText;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject targetCircle;
    [SerializeField] private Button startButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Transform targetArea;
    
    [Header("Game Settings")]
    [SerializeField] private float darkEnergyGrowthRate = 0.6f; // % mỗi giây
    [SerializeField] private float lightEnergyClickGain = 10f;  // % mỗi click
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float gameDuration = 60f;
    
    [Header("Target Settings")]
    [SerializeField] private float minSpawnTime = 0.8f;
    [SerializeField] private float maxSpawnTime = 1.5f;
    [SerializeField] private float targetLifetime = 1.2f;
    
    // === GAME VARIABLES ===
    private float lightEnergy = 50f;
    private float darkEnergy = 50f;
    private int score = 0;
    private float currentTime = 0f;
    private bool isGameActive = false;
    private Coroutine targetCoroutine;
    private Coroutine darkGrowthCoroutine;
    private GameObject currentTarget;
    
    // === INITIALIZATION ===
    private void Awake()
    {
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject); //  GIỮ LẠI KHI LOAD SCENE
    }
    else
    {
        Destroy(gameObject); //  Tránh bị nhân đôi
    }
    }

    
    private void Start()
    {
        SetupUI();
        SetupButtonListeners();
        ResetGame();
    }
    
    private void SetupUI()
    {
        // Ẩn các elements không cần thiết ban đầu
        restartButton.gameObject.SetActive(false);
        if (targetCircle != null)
            targetCircle.SetActive(false);
        
        // Cập nhật UI ban đầu
        UpdateEnergyBars();
        gameStateText.text = "Nhấn BẮT ĐẦU để chơi!";
        scoreText.text = "ĐIỂM: 0";
        timerText.text = "THỜI GIAN: 60s";
    }
    
    private void SetupButtonListeners()
    {
        startButton.onClick.AddListener(StartGame);
        restartButton.onClick.AddListener(RestartGame);
    }
    
    private void ResetGame()
    {
        lightEnergy = 50f;
        darkEnergy = 50f;
        score = 0;
        currentTime = gameDuration;
        
        UpdateEnergyBars();
    }
    
    // === GAME CONTROL ===
    public void StartGame()
    {
        ResetGame();
        isGameActive = true;
        
        // UI changes
        startButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        gameStateText.text = "Chiến đấu! Đừng để Hắc ám đẩy lùi!";
        
        // Bắt đầu spawn target
        if (targetCoroutine != null)
            StopCoroutine(targetCoroutine);
        targetCoroutine = StartCoroutine(TargetSpawnRoutine());
        
        // Bắt đầu tăng Hắc ám
        if (darkGrowthCoroutine != null)
            StopCoroutine(darkGrowthCoroutine);
        darkGrowthCoroutine = StartCoroutine(DarkEnergyGrowthRoutine());
    }
    
    // === ENERGY SYSTEM ===
    private IEnumerator DarkEnergyGrowthRoutine()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(1f);
            
            if (!isGameActive) yield break;
            
            // Hắc ám tự động tăng
            darkEnergy += darkEnergyGrowthRate;
            darkEnergy = Mathf.Clamp(darkEnergy, 0f, maxEnergy);
            
            // Ánh sáng bị đẩy lùi (tổng luôn = 100%)
            lightEnergy = maxEnergy - darkEnergy;
            
            UpdateEnergyBars();
            CheckGameOver();
        }
    }
    
    public void OnTargetClicked()
    {
        if (!isGameActive) return;
        
        // Tăng Ánh sáng
        lightEnergy += lightEnergyClickGain;
        lightEnergy = Mathf.Clamp(lightEnergy, 0f, maxEnergy);
        
        // Giảm Hắc ám tương ứng
        darkEnergy = maxEnergy - lightEnergy;
        
        // Tăng điểm
        score += 10;
        
        // Hiệu ứng
        PlayClickEffect();
        DestroyCurrentTarget();
        
        UpdateEnergyBars();
        CheckGameOver();
    }
    
    private void PlayClickEffect()
    {
        // Hiệu ứng đơn giản - có thể thêm particle sau
        if (targetCircle != null)
        {
            StartCoroutine(ClickAnimation());
        }
    }
    
    private IEnumerator ClickAnimation()
    {
        RectTransform rect = targetCircle.GetComponent<RectTransform>();
        Vector3 originalScale = rect.localScale;
        
        // Phóng to
        rect.localScale = originalScale * 1.3f;
        yield return new WaitForSeconds(0.1f);
        
        // Thu nhỏ
        rect.localScale = originalScale;
    }
    
    private void DestroyCurrentTarget()
    {
        if (currentTarget != null)
        {
            Destroy(currentTarget);
            currentTarget = null;
        }
    }
    
    // === TARGET SYSTEM ===
    private IEnumerator TargetSpawnRoutine()
    {
        while (isGameActive)
        {
            // Random thời gian chờ
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
            
            if (!isGameActive) yield break;
            
            SpawnTarget();
        }
    }
    
    private void SpawnTarget()
    {
        // Xóa target cũ
        DestroyCurrentTarget();
        
        // Tạo target mới
        if (targetCircle != null && targetArea != null)
        {
            currentTarget = Instantiate(targetCircle, targetArea);
            currentTarget.SetActive(true);
            
            // Random vị trí trong target area
            RectTransform areaRect = targetArea.GetComponent<RectTransform>();
            RectTransform targetRect = currentTarget.GetComponent<RectTransform>();
            
            float areaWidth = areaRect.rect.width;
            float areaHeight = areaRect.rect.height;
            
            float randomX = Random.Range(-areaWidth / 2 + 50, areaWidth / 2 - 50);
            float randomY = Random.Range(-areaHeight / 2 + 50, areaHeight / 2 - 50);
            
            targetRect.anchoredPosition = new Vector2(randomX, randomY);
            
            // Thiết lập thời gian sống
            StartCoroutine(TargetLifetimeRoutine());
        }
    }
    
    private IEnumerator TargetLifetimeRoutine()
    {
        yield return new WaitForSeconds(targetLifetime);
        
        if (currentTarget != null && isGameActive)
        {
            // Target biến mất mà không được click
            OnTargetMissed();
            DestroyCurrentTarget();
        }
    }
    
    private void OnTargetMissed()
    {
        if (!isGameActive) return;
        
        // Phạt khi bỏ lỡ target
        darkEnergy += 5f;
        darkEnergy = Mathf.Clamp(darkEnergy, 0f, maxEnergy);
        
        lightEnergy = maxEnergy - darkEnergy;
        
        // Giảm điểm
        score = Mathf.Max(0, score - 5);
        
        UpdateEnergyBars();
        CheckGameOver();
    }
    
    // === UI UPDATE ===
    private void UpdateEnergyBars()
    {
        // Cập nhật fill amount
        lightBar.fillAmount = lightEnergy / maxEnergy;
        darkBar.fillAmount = darkEnergy / maxEnergy;
        
        // Cập nhật text phần trăm
        lightPercentText.text = $"{lightEnergy:F0}%";
        darkPercentText.text = $"{darkEnergy:F0}%";
        
        // Cập nhật điểm
        scoreText.text = $"ĐIỂM: {score}";
        
        // Cập nhật thời gian
        timerText.text = $"THỜI GIAN: {Mathf.CeilToInt(currentTime)}s";
        
        // Đổi màu thanh dựa trên áp lực
        UpdateBarColors();
    }
    
    private void UpdateBarColors()
    {
        // Thanh Ánh sáng
        if (lightEnergy > 70f)
            lightBar.color = new Color(1f, 1f, 0.8f); // Vàng sáng
        else if (lightEnergy > 30f)
            lightBar.color = Color.yellow;
        else
            lightBar.color = new Color(1f, 0.8f, 0f); // Cam vàng
        
        // Thanh Hắc ám
        if (darkEnergy > 70f)
            darkBar.color = new Color(0.2f, 0f, 0f); // Đỏ đậm
        else if (darkEnergy > 30f)
            darkBar.color = Color.red;
        else
            darkBar.color = new Color(1f, 0.5f, 0.5f); // Đỏ nhạt
    }
    
    // === GAME OVER CHECK ===
    private void CheckGameOver()
    {
        if (!isGameActive) return;
        
        if (lightEnergy <= 0f)
        {
            GameOver(false); // Thua
        }
        else if (darkEnergy <= 0f)
        {
            GameOver(true); // Thắng
        }
    }
    
    private void GameOver(bool isVictory)
    {
        isGameActive = false;
        
        // Dừng các coroutine
        if (targetCoroutine != null)
        {
            StopCoroutine(targetCoroutine);
            targetCoroutine = null;
        }
        
        if (darkGrowthCoroutine != null)
        {
            StopCoroutine(darkGrowthCoroutine);
            darkGrowthCoroutine = null;
        }
        
        DestroyCurrentTarget();
        
        // Hiển thị kết quả
        if (isVictory)
        {
            gameStateText.text = "CHIẾN THẮNG! Ánh sáng đã toàn thắng!";
            gameStateText.color = Color.green;
        }
        else
        {
            gameStateText.text = "THẤT BẠI! Hắc ám đã chiếm lĩnh!";
            gameStateText.color = Color.red;
        }
        
        restartButton.gameObject.SetActive(true);
    }
    
    // === UPDATE LOOP ===
    private void Update()
    {
        if (isGameActive)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                if (lightEnergy > darkEnergy)
                    GameOver(true);
                else
                    GameOver(false);
            }
        }
    }
    
    public void RestartGame()
    {
        StartGame();
    }
}