using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class SmithingManager : MonoBehaviour
{
    public static SmithingManager Instance;

    [Header("UI References")]
    public GameObject smithingCanvas; 
    public GameObject heatingCanvas;  
    
    [Header("Cinematic & Camera")]
    public GameObject heatingCamera;    
    public GameObject smithingCamera;   

    [Header("Game Loop Settings")]
    public float totalTime = 45.0f;      // Tổng thời gian
    public int requiredPerfects = 5;     // Cần 5 lần chuẩn

    [Header("UI References")]
    public TextMeshProUGUI scoreText;    
    public TextMeshProUGUI countdownText; // <--- CÁI MỚI: Kéo Text to vào đây
    public TextMeshProUGUI rankText;      // <--- CÁI MỚI: Kéo Text hiện Rank vào đây
    public GameObject failPanel;         
    public GameObject winPanel; // (Có thể không cần nếu chuyển cảnh luôn)

    [Header("Physical References")]
    public Renderer ingotRenderer;       
    public BoxCollider spawnAreaCollider; 

    [Header("Pooling Settings")]
    public GameObject weakPointPrefab;   
    public int poolAmount = 10;          
    private List<GameObject> pooledObjects;

    [Header("Visuals")]
    public Color failColor = Color.black; 
    public GameObject hitEffectPrefab;   
    public AudioSource countAudio; // <--- CÁI MỚI: Âm thanh đếm

    // Biến nội bộ
    private float currentTime;
    private int currentPerfectCount = 0;
    private bool isGameActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 1. Setup Object Pooling
        pooledObjects = new List<GameObject>();
        for (int i = 0; i < poolAmount; i++)
        {
            GameObject obj = Instantiate(weakPointPrefab);
            obj.SetActive(false);
            pooledObjects.Add(obj);
        }

        // 2. Ẩn UI lúc đầu
        if (failPanel != null) failPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (scoreText != null) scoreText.text = ""; 
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (rankText != null) rankText.text = "";

        // 3. Tắt bộ Smithing, Gọi bộ Nung chạy trước
        if (smithingCanvas != null) smithingCanvas.SetActive(false);
        if (smithingCamera != null) smithingCamera.SetActive(false);

        if (HeatingManager.Instance != null)
        {
            HeatingManager.Instance.StartHeatingPhase();
        }
    }

    // --- HÀM BẮT ĐẦU (GỌI TỪ NUNG) ---
    public void StartSmithingPhase()
    {
        Debug.Log("--- CHUẨN BỊ ĐẬP ---");
        
        // 1. Chuyển Camera & UI
        if (heatingCanvas != null) heatingCanvas.SetActive(false);
        if (smithingCanvas != null) smithingCanvas.SetActive(true);
        if (heatingCamera != null) heatingCamera.SetActive(false);
        if (smithingCamera != null) smithingCamera.SetActive(true);

        // 2. Reset thông số nhưng CHƯA CHẠY GAME
        isGameActive = false; 
        currentTime = totalTime;
        currentPerfectCount = 0;
        
        UpdateScoreUI();

        // Reset màu thanh sắt
        if (ingotRenderer != null) 
            ingotRenderer.material.color = new Color(1f, 0.4f, 0f); 

        // 3. BẮT ĐẦU ĐẾM NGƯỢC
        StartCoroutine(CountdownRoutine());
    }

    // --- COROUTINE ĐẾM NGƯỢC ---
    IEnumerator CountdownRoutine()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            if(countAudio) countAudio.Play();
            yield return new WaitForSeconds(1.0f);

            countdownText.text = "2";
            if(countAudio) countAudio.Play();
            yield return new WaitForSeconds(1.0f);

            countdownText.text = "1";
            if(countAudio) countAudio.Play();
            yield return new WaitForSeconds(1.0f);

            countdownText.text = "ĐẬP!";
            if(countAudio) countAudio.Play();
            yield return new WaitForSeconds(0.5f);

            countdownText.gameObject.SetActive(false);
        }

        // Đếm xong mới cho chơi
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

        // Hết giờ -> Thua
        if (currentTime <= 0)
        {
            GameOver(false);
        }
    }

    // --- HỆ THỐNG POOLING (Giữ nguyên) ---
    public GameObject GetPooledObject()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (pooledObjects[i] == null) continue;
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }
        return null;
    }

    public void SpawnWeakPoint()
    {
        if (!isGameActive) return;

        GameObject wp = GetPooledObject();
        if (wp != null)
        {
            Vector3 center = spawnAreaCollider.center;
            Vector3 size = spawnAreaCollider.size;
            float randomX = Random.Range(center.x - size.x / 2, center.x + size.x / 2);
            float randomY = Random.Range(center.y - size.y / 2, center.y + size.y / 2);
            float randomZ = Random.Range(center.z - size.z / 2, center.z + size.z / 2);
            
            // Nhớ + Y lên một chút để không bị chìm
            Vector3 spawnPos = spawnAreaCollider.transform.TransformPoint(new Vector3(randomX, randomY, randomZ)) + new Vector3(0, 0.15f, 0);

            wp.transform.position = spawnPos;
            wp.transform.rotation = Quaternion.Euler(90, 0, 0);
            wp.SetActive(true);
        }
    }

    // --- XỬ LÝ VA CHẠM (PlayerSmithing gọi) ---
    public void SpawnHammerEffect(Vector3 position, int score)
    {
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, position, Quaternion.identity);
        CheckScore(score);
    }

    void CheckScore(int score)
    {
        if (!isGameActive) return;

        if (score >= 100)
        {
            currentPerfectCount++;
            Debug.Log($"PERFECT HIT! ({currentPerfectCount}/{requiredPerfects})");
            UpdateScoreUI();

            if (currentPerfectCount >= requiredPerfects)
            {
                GameOver(true);
            }
        }
        
        if (isGameActive) Invoke("SpawnWeakPoint", 1.0f);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"{currentPerfectCount} / {requiredPerfects}";
    }

    // --- KẾT THÚC ---
    void GameOver(bool isWin)
    {
        isGameActive = false;

        if (isWin)
        {
            // === TÍNH RANK ===
            string rank = "COMMON";
            Color rankColor = Color.gray;

            // Nếu hoàn thành mà thời gian còn thừa nhiều -> Rank cao
            if (currentTime >= totalTime * 0.6f) // Còn dư 60% thời gian
            {
                rank = "HUYỀN THOẠI";
                rankColor = Color.cyan;
            }
            else if (currentTime >= totalTime * 0.3f) // Còn dư 30% thời gian
            {
                rank = "SỬ THI";
                rankColor = Color.magenta;
            }
            
            // Hiện Rank lên màn hình
            if (rankText != null)
            {
                rankText.text = rank;
                rankText.color = rankColor;
                rankText.gameObject.SetActive(true);
            }

            Debug.Log("WIN - Rank: " + rank);
            
            // Chuyển cảnh sau 2 giây
            Invoke("GoToQuench", 2.0f);
        }
        else
        {
            Debug.Log("LOSE");
            if (ingotRenderer != null) ingotRenderer.material.color = failColor;
            if (failPanel != null) failPanel.SetActive(true);
            if (rankText != null) 
            {
                rankText.text = "PHẾ PHẨM!";
                rankText.color = Color.red;
                rankText.gameObject.SetActive(true);
            }
            
            Invoke("RestartHeating", 2.0f);
        }
    }
    
    void GoToQuench()
    {
        if (smithingCanvas != null) smithingCanvas.SetActive(false);
        if (QuenchManager.Instance != null) QuenchManager.Instance.StartQuenchSequence();
    }

    void RestartHeating()
    {
        if (HeatingManager.Instance != null) HeatingManager.Instance.StartHeatingPhase();
        if (failPanel != null) failPanel.SetActive(false);
        if (rankText != null) rankText.text = "";
    }
}