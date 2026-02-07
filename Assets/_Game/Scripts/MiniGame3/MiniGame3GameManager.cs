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

    [Header("Background Effects")]
    [SerializeField] private Image lightBackgroundGlow;
    [SerializeField] private Image darkBackgroundGlow;
    [SerializeField] private ParticleSystem lightParticles;
    [SerializeField] private ParticleSystem darkParticles;
    
    [Header("Game Settings")]
    [SerializeField] private float darkEnergyGrowthRate = 0.6f;
    [SerializeField] private float lightEnergyClickGain = 10f;
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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
        restartButton.gameObject.SetActive(false);
        if (targetCircle != null)
            targetCircle.SetActive(false);
        
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
        
        startButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        gameStateText.text = "Chiến đấu! Đừng để Hắc ám đẩy lùi!";
        
        // THÊM
        ResetBackgroundEffects();
        StartBackgroundEffects();
        
        if (targetCoroutine != null)
            StopCoroutine(targetCoroutine);
        targetCoroutine = StartCoroutine(TargetSpawnRoutine());
        
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
            
            darkEnergy += darkEnergyGrowthRate;
            darkEnergy = Mathf.Clamp(darkEnergy, 0f, maxEnergy);
            lightEnergy = maxEnergy - darkEnergy;
            
            UpdateEnergyBars();
            CheckGameOver();
        }
    }
    
    public void OnTargetClicked()
    {
        if (!isGameActive) return;
        
        lightEnergy += lightEnergyClickGain;
        lightEnergy = Mathf.Clamp(lightEnergy, 0f, maxEnergy);
        darkEnergy = maxEnergy - lightEnergy;
        
        score += 10;
        DestroyCurrentTarget();
        
        UpdateEnergyBars();
        CheckGameOver();
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
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
            
            if (!isGameActive) yield break;
            SpawnTarget();
        }
    }
    
    private void SpawnTarget()
    {
        DestroyCurrentTarget();
        
        if (targetCircle != null && targetArea != null)
        {
            currentTarget = Instantiate(targetCircle, targetArea);
            currentTarget.SetActive(true);
            
            RectTransform areaRect = targetArea.GetComponent<RectTransform>();
            RectTransform targetRect = currentTarget.GetComponent<RectTransform>();
            
            float randomX = Random.Range(-areaRect.rect.width / 2 + 50, areaRect.rect.width / 2 - 50);
            float randomY = Random.Range(-areaRect.rect.height / 2 + 50, areaRect.rect.height / 2 - 50);
            
            targetRect.anchoredPosition = new Vector2(randomX, randomY);
            StartCoroutine(TargetLifetimeRoutine());
        }
    }
    
    private IEnumerator TargetLifetimeRoutine()
    {
        yield return new WaitForSeconds(targetLifetime);
        
        if (currentTarget != null && isGameActive)
        {
            darkEnergy += 5f;
            darkEnergy = Mathf.Clamp(darkEnergy, 0f, maxEnergy);
            lightEnergy = maxEnergy - darkEnergy;
            score = Mathf.Max(0, score - 5);
            
            DestroyCurrentTarget();
            UpdateEnergyBars();
            CheckGameOver();
        }
    }
    
    // === UI UPDATE ===
    private void UpdateEnergyBars()
    {
        lightBar.fillAmount = lightEnergy / maxEnergy;
        darkBar.fillAmount = darkEnergy / maxEnergy;
        
        lightPercentText.text = $"{lightEnergy:F0}%";
        darkPercentText.text = $"{darkEnergy:F0}%";
        scoreText.text = $"ĐIỂM: {score}";
        timerText.text = $"THỜI GIAN: {Mathf.CeilToInt(currentTime)}s";
    }
    
    // === GAME OVER CHECK ===
    private void CheckGameOver()
    {
        if (!isGameActive) return;
        
        if (lightEnergy <= 0f)
            GameOver(false);
        else if (darkEnergy <= 0f)
            GameOver(true);
    }
    
    private void GameOver(bool isVictory)
    {
        isGameActive = false;
        
        if (targetCoroutine != null)
            StopCoroutine(targetCoroutine);
        
        if (darkGrowthCoroutine != null)
            StopCoroutine(darkGrowthCoroutine);
        
        DestroyCurrentTarget();
        
        // THÊM
        if (lightParticles != null)
            lightParticles.Stop();
        if (darkParticles != null)
            darkParticles.Stop();
        
        if (isVictory)
        {
            gameStateText.text = "CHIẾN THẮNG! Ánh sáng đã toàn thắng!";
            gameStateText.color = Color.green;
            StartCoroutine(VictoryEffect());
        }
        else
        {
            gameStateText.text = "THẤT BẠI! Hắc ám đã chiếm lĩnh!";
            gameStateText.color = Color.red;
            StartCoroutine(DefeatEffect());
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
                GameOver(lightEnergy > darkEnergy);
            }
            
            UpdateBackgroundEffects();
        }
    }
    
    public void RestartGame()
    {
        StartGame();
    }

    // ================= BACKGROUND EFFECTS =================

    private void UpdateBackgroundEffects()
    {
        float lightStrength = 0f;
        if (lightEnergy > 50f)
            lightStrength = (lightEnergy - 50f) / 50f;
        
        if (lightBackgroundGlow != null)
        {
            Color c = lightBackgroundGlow.color;
            c.a = lightStrength * 0.3f;
            lightBackgroundGlow.color = c;
        }
        
        if (lightParticles != null)
        {
            var emission = lightParticles.emission;
            emission.rateOverTime = lightStrength * 15f;
            
            var main = lightParticles.main;
            main.startColor = new Color(1f, 1f, 0.8f, lightStrength * 0.5f);
        }
        
        float darkStrength = 0f;
        if (darkEnergy > 50f)
            darkStrength = (darkEnergy - 50f) / 50f;
        
        if (darkBackgroundGlow != null)
        {
            Color c = darkBackgroundGlow.color;
            c.a = darkStrength * 0.3f;
            darkBackgroundGlow.color = c;
        }
        
        if (darkParticles != null)
        {
            var emission = darkParticles.emission;
            emission.rateOverTime = darkStrength * 15f;
            
            var main = darkParticles.main;
            main.startColor = new Color(1f, 0.3f, 0.3f, darkStrength * 0.5f);
        }
        
        if (lightEnergy > 80f)
            StartCoroutine(StrongLightEffect());
        else if (darkEnergy > 80f)
            StartCoroutine(StrongDarkEffect());
    }

    private IEnumerator StrongLightEffect()
    {
        float pulse = Mathf.Sin(Time.time * 5f) * 0.1f + 0.2f; ///- 5f: Tốc độ nhấp nháy (cao = nhanh)- 0.1f: Độ sâu nhấp nháy (cao = chênh lệch nhiều)- 0.2f: Độ sáng cơ bản (cao = sáng hơn)
        if (lightBackgroundGlow != null)
        {
            Color c = lightBackgroundGlow.color;
            c.a = pulse;
            lightBackgroundGlow.color = c;
        }
        yield return null;
    }

    private IEnumerator StrongDarkEffect()
    {
        float pulse = Mathf.Sin(Time.time * 5f) * 0.1f + 0.2f;
        if (darkBackgroundGlow != null)
        {
            Color c = darkBackgroundGlow.color;
            c.a = pulse;
            darkBackgroundGlow.color = c;
        }
        yield return null;
    }

    private void ResetBackgroundEffects()
    {
        if (lightBackgroundGlow != null)
        {
            Color c = lightBackgroundGlow.color;
            c.a = 0f;
            lightBackgroundGlow.color = c;
        }
        
        if (darkBackgroundGlow != null)
        {
            Color c = darkBackgroundGlow.color;
            c.a = 0f;
            darkBackgroundGlow.color = c;
        }
        
        if (lightParticles != null)
        {
            lightParticles.Stop();
            var emission = lightParticles.emission;
            emission.rateOverTime = 0f;
        }
        
        if (darkParticles != null)
        {
            darkParticles.Stop();
            var emission = darkParticles.emission;
            emission.rateOverTime = 0f;
        }
    }

    private void StartBackgroundEffects()
    {
        if (lightParticles != null)
            lightParticles.Play();
        if (darkParticles != null)
            darkParticles.Play();
    }

    private IEnumerator VictoryEffect()
    {
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            if (lightBackgroundGlow != null)
            {
                float pulse = Mathf.Sin(Time.time * 10f) * 0.2f + 0.3f;
                Color c = lightBackgroundGlow.color;
                c.a = pulse;
                lightBackgroundGlow.color = c;
            }
            yield return null;
        }
    }

    private IEnumerator DefeatEffect()
    {
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            if (darkBackgroundGlow != null)
            {
                Color c = darkBackgroundGlow.color;
                c.a = Mathf.Lerp(0f, 0.5f, elapsed / 2f);
                darkBackgroundGlow.color = c;
                
                RectTransform rect = darkBackgroundGlow.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(Mathf.Sin(Time.time * 20f) * 2f, 0);
            }
            yield return null;
        }
        
        if (darkBackgroundGlow != null)
            darkBackgroundGlow.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }
}
