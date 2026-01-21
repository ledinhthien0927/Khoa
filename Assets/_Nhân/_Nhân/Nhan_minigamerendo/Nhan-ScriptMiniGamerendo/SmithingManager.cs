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
    
    [Header("--- TUTORIAL (HƯỚNG DẪN) ---")]
    public GameObject tutorialPanel;        
    public Button closeTutorialButton;      

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

    // ==========================================================
    // [MỚI] THAM CHIẾU ĐẾN OBJECT CÓ SẴN TRÊN ĐE
    // Kéo thả Object Kiếm và Búa đã đặt sẵn trên Scene vào đây
    // ==========================================================
    [Header("--- WEAPON OBJECTS ---")]
    public GameObject swordObject;   // Object Kiếm
    public GameObject hammerObject;  // Object Búa
    // ==========================================================

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

    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        // Lưu lại kích thước ban đầu của khối sắt
        if (rawIronBlock != null) 
        {
            initialBlockScale = rawIronBlock.localScale;
        }

        // Tạo Pooling cho các điểm yếu (Weak Points)
        pooledObjects = new List<GameObject>();
        for (int i = 0; i < poolAmount; i++) 
        {
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

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        if (smithingCanvas != null) smithingCanvas.SetActive(false);
        if (smithingCamera != null) smithingCamera.SetActive(false);

        // [MỚI] Tắt cả 2 vũ khí đi khi game mới chạy để không bị lộ
        if (swordObject != null) swordObject.SetActive(false);
        if (hammerObject != null) hammerObject.SetActive(false);
    }

    // Hàm bắt đầu giai đoạn Rèn (được gọi từ HeatingManager)
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

        // ==========================================================
        // [MỚI] LOGIC BẬT/TẮT VŨ KHÍ
        // ==========================================================
        
        // 1. Tắt hết tất cả trước
        if (swordObject != null) swordObject.SetActive(false);
        if (hammerObject != null) hammerObject.SetActive(false);

        // 2. Chỉ bật cái được chọn bên màn hình Nung (HeatingManager)
        if (HeatingManager.CurrentWeaponType == WeaponType.Sword)
        {
            if (swordObject != null) swordObject.SetActive(true);
        }
        else if (HeatingManager.CurrentWeaponType == WeaponType.Hammer)
        {
            if (hammerObject != null) hammerObject.SetActive(true);
        }
        // ==========================================================

        // Reset thông số game
        isGameActive = false; 
        currentTime = totalTime;
        currentPerfectCount = 0;
        
        UpdateScoreUI();

        // Reset khối sắt bao bọc bên ngoài
        if (rawIronBlock != null)
        {
            rawIronBlock.gameObject.SetActive(true);
            rawIronBlock.localScale = initialBlockScale; 
        }

        if (blockRenderer != null) 
        {
            blockRenderer.material.color = normalBlockColor; 
        }

        // Kiểm tra Tutorial
        if (tutorialPanel != null)
        {
            OpenTutorial();
        }
        else
        {
            StartCoroutine(CountdownRoutine());
        }
    }

    // --- CÁC HÀM XỬ LÝ TUTORIAL ---
    void OpenTutorial() 
    { 
        tutorialPanel.SetActive(true); 
        if (closeTutorialButton != null) 
        { 
            closeTutorialButton.onClick.RemoveAllListeners(); 
            closeTutorialButton.onClick.AddListener(CloseTutorialAndStartGame); 
        } 
    }

    void CloseTutorialAndStartGame() 
    { 
        if (tutorialPanel != null) tutorialPanel.SetActive(false); 
        StartCoroutine(CountdownRoutine()); 
    }
    
    // --- ĐẾM NGƯỢC ---
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
            
            countdownText.text = "STRIKE!"; 
            if(countAudio) countAudio.Play(); 
            yield return new WaitForSeconds(0.5f);
            
            countdownText.gameObject.SetActive(false);
        }
        StartActualGame();
    }
    
    void StartActualGame() 
    { 
        isGameActive = true; 
        SpawnWeakPoint(); 
    }

    // --- VÒNG LẶP UPDATE ---
    void Update() 
    { 
        if (!isGameActive) return; 
        
        currentTime -= Time.deltaTime; 
        if (currentTime <= 0) 
        {
            GameOver(false);
        }
    }

    // --- SPAWN ĐIỂM YẾU ---
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

    // Xử lý khi người chơi không kịp bấm
    public void HandleTimeout() 
    { 
        if (!isGameActive) return; 
        CancelInvoke("SpawnWeakPoint"); 
        Invoke("SpawnWeakPoint", 0.5f); 
    }

    // --- XỬ LÝ KHI NGƯỜI CHƠI ĐẬP TRÚNG ---
    public void ProcessHit(Vector3 hitPos, Vector3 hitNormal, int score) 
    { 
        // Hiệu ứng tia lửa
        if (sparkEffect != null) 
        { 
            sparkEffect.transform.position = hitPos; 
            sparkEffect.transform.rotation = Quaternion.LookRotation(hitNormal); 
            sparkEffect.Play(); 
        }

        if (score >= 100) // Đập chuẩn (Perfect)
        { 
            currentPerfectCount++; 
            UpdateScoreUI(); 
            
            // Làm nhỏ khối sắt bao bên ngoài
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
        else // Đập chưa chuẩn
        { 
            Debug.Log("Chưa chuẩn! Cube giữ nguyên.");
            CancelInvoke("SpawnWeakPoint"); 
            Invoke("SpawnWeakPoint", 0.3f); 
        }
    }

    public GameObject GetPooledObject() 
    { 
        foreach (var obj in pooledObjects) 
        {
            if (obj != null && !obj.activeInHierarchy) return obj; 
        }
        return null; 
    }

    void UpdateScoreUI() 
    { 
        if (scoreText != null) scoreText.text = $"{currentPerfectCount} / {requiredPerfects}"; 
    }

    // --- KẾT THÚC GAME ---
    void GameOver(bool isWin) 
    { 
        isGameActive = false; 
        
        if (isWin) 
        { 
            // Tắt hẳn khối sắt bao bên ngoài để lộ vũ khí
            if (rawIronBlock != null) rawIronBlock.gameObject.SetActive(false); 
            
            string rank = "COMMON"; 
            Color c = Color.gray; 
            
            if (currentTime >= totalTime * 0.6f) 
            { 
                rank = "HUYỀN THOẠI"; 
                c = Color.cyan; 
                IsLegendary = true; 
            } 
            else if (currentTime >= totalTime * 0.3f) 
            { 
                rank = "SỬ THI"; 
                c = Color.magenta; 
                IsLegendary = false; 
            } 
            
            if (rankText != null) 
            { 
                rankText.text = rank; 
                rankText.color = c; 
                rankText.gameObject.SetActive(true); 
                IsLegendary = false;
            } 
            
            if (completionVFX != null) completionVFX.Play(); 
            
            Debug.Log("WIN - Rank: " + rank);
            Invoke("GoToQuench", 2.0f); 
        } 
        else // Thua
        { 
            Debug.Log("LOSE");
            if (blockRenderer != null) blockRenderer.material.color = failBlockColor; 
            if (failPanel != null) failPanel.SetActive(true); 
            
            Invoke("RestartHeating", 2.0f); 
        } 
    }
    
    // Chuyển cảnh
    void GoToQuench() 
    { 
        if (smithingCanvas != null) smithingCanvas.SetActive(false); 
        if (QuenchManager.Instance != null) QuenchManager.Instance.StartQuenchSequence(); 
    }

    void RestartHeating() 
    { 
        if (HeatingManager.Instance != null) HeatingManager.Instance.StartHeatingPhase(); 
        if (failPanel != null) failPanel.SetActive(false); 
    }
}