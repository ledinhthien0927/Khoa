using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class SmithingManager : MonoBehaviour
{
    public static SmithingManager Instance;

    [Header("--- UI & CAMERAS ---")]
    public GameObject smithingCanvas; 
    public GameObject otherCanvasToHide; 
    public GameObject heatingCanvas;  
    public GameObject heatingCamera;    
    public GameObject smithingCamera;   
    
    [Header("--- TUTORIAL (HƯỚNG DẪN) ---")] // <--- MỚI
    public GameObject tutorialPanel;         // Kéo Panel hướng dẫn vào đây
    public Button closeTutorialButton;       // Kéo Button tắt hướng dẫn vào đây

    [Header("--- PLAYER ---")]
    public GameObject playerObject; 

    [Header("--- GAME SETTINGS ---")]
    public float totalTime = 45.0f;      
    public int requiredPerfects = 5;     

    [Header("--- UI DISPLAY ---")]
    public TextMeshProUGUI scoreText;    
    public TextMeshProUGUI countdownText; 
    public TextMeshProUGUI rankText;      
    public GameObject failPanel;         
    public static bool IsLegendary = false;

    [Header("--- CƠ CHẾ BÓC VỎ (CUBE) ---")]
    public Transform rawIronBlock;      
    private Vector3 initialBlockScale;    
    
    [Header("--- VISUALS & FX ---")]
    public Renderer blockRenderer;        
    public BoxCollider spawnAreaCollider; 
    public ParticleSystem sparkEffect;    
    public ParticleSystem completionVFX; 
    public AudioSource countAudio; 

    public Color normalBlockColor = new Color(0.3f, 0.3f, 0.3f); 
    public Color failBlockColor = Color.black; 

    [Header("--- POOLING ---")]
    public GameObject weakPointPrefab;   
    public int poolAmount = 10;          
    private List<GameObject> pooledObjects;

    private float currentTime;
    private int currentPerfectCount = 0;
    private bool isGameActive = false;

    void Awake() { Instance = this; }

    void Start()
    {
        if (rawIronBlock != null) initialBlockScale = rawIronBlock.localScale;

        pooledObjects = new List<GameObject>();
        for (int i = 0; i < poolAmount; i++) {
            GameObject obj = Instantiate(weakPointPrefab);
            obj.SetActive(false);
            pooledObjects.Add(obj);
        }

        // Ẩn các UI không cần thiết ban đầu
        if (failPanel != null) failPanel.SetActive(false);
        if (scoreText != null) scoreText.text = ""; 
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (rankText != null) rankText.text = "";
        if (completionVFX != null) completionVFX.Stop();

        // --- MỚI: Ẩn Tutorial Panel khi game mới chạy ---
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        // -----------------------------------------------

        if (smithingCanvas != null) smithingCanvas.SetActive(false);
        if (smithingCamera != null) smithingCamera.SetActive(false);

        if (HeatingManager.Instance != null) HeatingManager.Instance.StartHeatingPhase();
    }

    public void StartSmithingPhase()
    {
        if (playerObject != null) playerObject.SetActive(true);

        // Bật/Tắt UI Canvas
        if (heatingCanvas != null) heatingCanvas.SetActive(false);
        if (smithingCanvas != null) smithingCanvas.SetActive(true);
        if (otherCanvasToHide != null) otherCanvasToHide.SetActive(false);

        // Chuyển Camera
        if (heatingCamera != null) heatingCamera.SetActive(false);
        if (smithingCamera != null) smithingCamera.SetActive(true);

        // Reset thông số
        isGameActive = false; 
        currentTime = totalTime;
        currentPerfectCount = 0;
        
        UpdateScoreUI();

        if (rawIronBlock != null)
        {
            rawIronBlock.gameObject.SetActive(true);
            rawIronBlock.localScale = initialBlockScale; 
        }

        if (blockRenderer != null) 
            blockRenderer.material.color = normalBlockColor; 

        // --- MỚI: Logic hiển thị Tutorial ---
        if (tutorialPanel != null)
        {
            OpenTutorial();
        }
        else
        {
            // Nếu không có Tutorial Panel thì chạy luôn
            StartCoroutine(CountdownRoutine());
        }
    }

    // --- MỚI: Hàm xử lý mở Tutorial ---
    void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
        
        // Đăng ký sự kiện click cho nút đóng (để chắc chắn không bị lỗi logic)
        if (closeTutorialButton != null)
        {
            closeTutorialButton.onClick.RemoveAllListeners(); // Xóa sự kiện cũ
            closeTutorialButton.onClick.AddListener(CloseTutorialAndStartGame);
        }
    }

    // --- MỚI: Hàm xử lý đóng Tutorial và Bắt đầu game ---
    void CloseTutorialAndStartGame()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        
        // Sau khi tắt bảng hướng dẫn mới bắt đầu đếm ngược
        StartCoroutine(CountdownRoutine());
    }
    // ------------------------------------------------

    IEnumerator CountdownRoutine()
    {
        if (countdownText != null) {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "3"; if(countAudio) countAudio.Play(); yield return new WaitForSeconds(1.0f);
            countdownText.text = "2"; if(countAudio) countAudio.Play(); yield return new WaitForSeconds(1.0f);
            countdownText.text = "1"; if(countAudio) countAudio.Play(); yield return new WaitForSeconds(1.0f);
            countdownText.text = "STRIKE!"; if(countAudio) countAudio.Play(); yield return new WaitForSeconds(0.5f);
            countdownText.gameObject.SetActive(false);
        }
        StartActualGame();
    }

    void StartActualGame()
    {
        isGameActive = true;
        SpawnWeakPoint();
    }

    void Update()
    {
        if (!isGameActive) return;
        currentTime -= Time.deltaTime;
        if (currentTime <= 0) GameOver(false);
    }

    public void SpawnWeakPoint()
    {
        if (!isGameActive) return;

        GameObject wp = GetPooledObject();
        if (wp != null)
        {
            Bounds bounds = spawnAreaCollider.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            float fixedZ = bounds.center.z; 

            wp.transform.position = new Vector3(randomX, randomY, fixedZ);
            wp.transform.rotation = Quaternion.Euler(-90, 0, 0); 
            wp.SetActive(true);
        }
    }

    public void HandleTimeout()
    {
        if (!isGameActive) return;
        CancelInvoke("SpawnWeakPoint");
        Invoke("SpawnWeakPoint", 0.5f);
    }

    public void ProcessHit(Vector3 hitPos, Vector3 hitNormal, int score)
    {
        if (sparkEffect != null)
        {
            sparkEffect.transform.position = hitPos;
            sparkEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
            sparkEffect.Play();
        }

        if (score >= 100)
        {
            currentPerfectCount++;
            UpdateScoreUI();

            if (rawIronBlock != null)
            {
                float progress = (float)currentPerfectCount / requiredPerfects;
                rawIronBlock.localScale = Vector3.Lerp(initialBlockScale, Vector3.zero, progress);
            }

            if (currentPerfectCount >= requiredPerfects)
            {
                GameOver(true);
            }
            else
            {
                CancelInvoke("SpawnWeakPoint");
                Invoke("SpawnWeakPoint", 0.5f);
            }
        }
        else
        {
            Debug.Log("Chưa chuẩn! Cube giữ nguyên.");
            CancelInvoke("SpawnWeakPoint");
            Invoke("SpawnWeakPoint", 0.3f); 
        }
    }

    public GameObject GetPooledObject()
    {
        foreach (var obj in pooledObjects) if (obj != null && !obj.activeInHierarchy) return obj;
        return null; 
    }

    void UpdateScoreUI() { if (scoreText != null) scoreText.text = $"{currentPerfectCount} / {requiredPerfects}"; }

    void GameOver(bool isWin)
    {
        isGameActive = false;

        if (isWin)
        {
            if (rawIronBlock != null) rawIronBlock.gameObject.SetActive(false);

            string rank = "COMMON"; Color c = Color.gray;
            if (currentTime >= totalTime * 0.6f) { rank = "HUYỀN THOẠI"; c = Color.cyan;IsLegendary = true; }
            else if (currentTime >= totalTime * 0.3f) { rank = "SỬ THI"; c = Color.magenta; IsLegendary = false;}
            
            if (rankText != null) { rankText.text = rank; rankText.color = c; rankText.gameObject.SetActive(true); IsLegendary = false;}
            if (completionVFX != null) completionVFX.Play();

            Debug.Log("WIN - Rank: " + rank);
            Invoke("GoToQuench", 2.0f);
        }
        else
        {
            Debug.Log("LOSE");
            if (blockRenderer != null) blockRenderer.material.color = failBlockColor; 
            if (failPanel != null) failPanel.SetActive(true);
            Invoke("RestartHeating", 2.0f);
        }
    }
    
    void GoToQuench() { if (smithingCanvas != null) smithingCanvas.SetActive(false); if (QuenchManager.Instance != null) QuenchManager.Instance.StartQuenchSequence(); }
    void RestartHeating() { if (HeatingManager.Instance != null) HeatingManager.Instance.StartHeatingPhase(); if (failPanel != null) failPanel.SetActive(false); }
}