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
    public GameObject heatingCanvas;  
    public GameObject heatingCamera;    
    public GameObject smithingCamera;   
    
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

    [Header("--- CƠ CHẾ BÓC VỎ (CUBE) ---")]
    public Transform rawIronBlock;      
    private Vector3 initialBlockScale;    
    
    [Header("--- VISUALS & FX ---")]
    public Renderer blockRenderer;        
    public BoxCollider spawnAreaCollider; 
    public ParticleSystem sparkEffect;    
    public ParticleSystem completionVFX; 
    public AudioSource countAudio; 

    // --- MỚI: BIẾN MÀU SẮC ĐỂ BẠN TỰ CHỈNH ---
    public Color normalBlockColor = new Color(0.3f, 0.3f, 0.3f); // Mặc định là Xám Đậm
    public Color failBlockColor = Color.black; // Màu khi thua (cháy đen)
    // -----------------------------------------

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

        if (failPanel != null) failPanel.SetActive(false);
        if (scoreText != null) scoreText.text = ""; 
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (rankText != null) rankText.text = "";
        if (completionVFX != null) completionVFX.Stop();

        if (smithingCanvas != null) smithingCanvas.SetActive(false);
        if (smithingCamera != null) smithingCamera.SetActive(false);

        if (HeatingManager.Instance != null) HeatingManager.Instance.StartHeatingPhase();
    }

    public void StartSmithingPhase()
    {
        if (playerObject != null) playerObject.SetActive(true);

        if (heatingCanvas != null) heatingCanvas.SetActive(false);
        if (smithingCanvas != null) smithingCanvas.SetActive(true);
        if (heatingCamera != null) heatingCamera.SetActive(false);
        if (smithingCamera != null) smithingCamera.SetActive(true);

        isGameActive = false; 
        currentTime = totalTime;
        currentPerfectCount = 0;
        
        UpdateScoreUI();

        if (rawIronBlock != null)
        {
            rawIronBlock.gameObject.SetActive(true);
            rawIronBlock.localScale = initialBlockScale; 
        }

        // --- ÁP DỤNG MÀU MỚI BẠN CHỌN ---
        if (blockRenderer != null) 
            blockRenderer.material.color = normalBlockColor; 

        StartCoroutine(CountdownRoutine());
    }

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
            if (currentTime >= totalTime * 0.6f) { rank = "HUYỀN THOẠI"; c = Color.cyan; }
            else if (currentTime >= totalTime * 0.3f) { rank = "SỬ THI"; c = Color.magenta; }
            
            if (rankText != null) { rankText.text = rank; rankText.color = c; rankText.gameObject.SetActive(true); }
            if (completionVFX != null) completionVFX.Play();

            Debug.Log("WIN - Rank: " + rank);
            Invoke("GoToQuench", 2.0f);
        }
        else
        {
            Debug.Log("LOSE");
            // Đổi thành màu Fail bạn chọn
            if (blockRenderer != null) blockRenderer.material.color = failBlockColor; 
            if (failPanel != null) failPanel.SetActive(true);
            Invoke("RestartHeating", 2.0f);
        }
    }
    
    void GoToQuench() { if (smithingCanvas != null) smithingCanvas.SetActive(false); if (QuenchManager.Instance != null) QuenchManager.Instance.StartQuenchSequence(); }
    void RestartHeating() { if (HeatingManager.Instance != null) HeatingManager.Instance.StartHeatingPhase(); if (failPanel != null) failPanel.SetActive(false); }
}