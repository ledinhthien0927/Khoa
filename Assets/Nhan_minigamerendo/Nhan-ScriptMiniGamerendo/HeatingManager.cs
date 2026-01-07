using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class HeatingManager : MonoBehaviour
{
    public static HeatingManager Instance;

    [Header("UI References")]
    public GameObject heatingCanvas; 
    public GameObject smithingCanvas; // Để tắt đi
    
    [Header("UI - Minigame")]
    public Slider tempSlider;        // Thanh Nhiệt độ (Lửa) - Cái cũ
    public Slider masterySlider;     // THANH MỚI: Độ chín của vật phẩm
    public TextMeshProUGUI timeText; // Hiện thời gian trôi qua
    public TextMeshProUGUI rankText; // Hiện xếp hạng khi xong

    [Header("Cinematic & Visuals")]
    public GameObject heatingCamera;   
    public Renderer ingotRenderer;   
    public Gradient heatColorGradient; // Màu sắt thay đổi theo nhiệt độ
    public ParticleSystem fireEffect;
    public AudioSource sizzleAudio;

    [Header("Cơ Chế Lửa (Temperature)")]
    public float heatSpeed = 0.5f;       // Tốc độ tăng nhiệt
    public float coolSpeed = 0.3f;       // Tốc độ giảm nhiệt
    public float minTempToProcess = 0.3f;// Lửa trên 30% mới bắt đầu có tác dụng

    [Header("Cơ Chế Tiến Độ (Mastery)")]
    public float baseProgressSpeed = 0.2f; // Tốc độ lấp đầy thanh tiến độ cơ bản

    [Header("Hệ Thống Xếp Hạng (Thời Gian)")]
    public float timeLimit = 90f; // 1p30s là giới hạn phế thải
    // Các mốc thời gian (giây)
    public float rankPerfect = 25f; // Dưới 25s
    public float rankGood = 35f;    // Dưới 35s
    public float rankNormal = 60f;  // Dưới 60s (1p)

    // Biến nội bộ
    private float currentTemp = 0f;      // Nhiệt độ hiện tại (0.0 - 1.0)
    private float currentProgress = 0f;  // Tiến độ nung (0.0 - 1.0)
    private float elapsedTime = 0f;      // Thời gian đã trôi qua
    private bool isHeatingActive = false;
    private bool isGameOver = false;

    void Awake() { Instance = this; }

    public void StartHeatingPhase()
    {
        isHeatingActive = true;
        isGameOver = false;
        
        currentTemp = 0f;
        currentProgress = 0f;
        elapsedTime = 0f;

        // Setup UI ban đầu
        if (heatingCanvas != null) heatingCanvas.SetActive(true);
        if (tempSlider != null) tempSlider.value = 0f;
        if (masterySlider != null) masterySlider.value = 0f;
        if (rankText != null) rankText.text = "";
        
        // Setup Camera
        if (heatingCamera != null) heatingCamera.SetActive(true);
        
        // Âm thanh
        if (sizzleAudio != null) { sizzleAudio.volume = 0; sizzleAudio.Play(); }
    }

    void Update()
    {
        if (!isHeatingActive || isGameOver) return;

        // 1. Tính thời gian
        elapsedTime += Time.deltaTime;
        UpdateTimerUI();

        // Kiểm tra quá thời gian (Phế thải)
        if (elapsedTime > timeLimit)
        {
            FailGame("QUÁ LÂU! SẮT ĐÃ HỎNG (SCRAP)");
            return;
        }

        // 2. Điều khiển Nhiệt độ (Lửa)
        if (Input.GetMouseButton(0)) 
        {
            currentTemp += heatSpeed * Time.deltaTime;
            if (fireEffect != null && !fireEffect.isPlaying) fireEffect.Play();
        } 
        else 
        {
            currentTemp -= coolSpeed * Time.deltaTime;
            if (fireEffect != null) fireEffect.Stop();
        }
        currentTemp = Mathf.Clamp01(currentTemp);

        // 3. Xử lý "Lửa 100% là Hỏng"
        if (currentTemp >= 1.0f)
        {
            FailGame("QUÁ NHIỆT! PHÔI TAN CHẢY!");
            return;
        }

        // 4. Tính Tiến Độ (Mastery)
        // Chỉ tăng khi nhiệt độ > 30% (0.3)
        if (currentTemp > minTempToProcess)
        {
            // Công thức: Tốc độ nung tỉ lệ thuận với độ nóng
            // Lửa càng to (gần 0.9), nung càng lẹ. Lửa nhỏ (0.35), nung siêu chậm.
            float bonusSpeed = currentTemp * 2.0f; // Hệ số thưởng
            currentProgress += baseProgressSpeed * bonusSpeed * Time.deltaTime;
        }

        // 5. Kiểm tra Hoàn Thành
        if (currentProgress >= 1.0f)
        {
            FinishHeating();
        }

        UpdateVisuals();
    }

    void UpdateTimerUI()
    {
        if (timeText != null)
        {
            // Đổi màu text dựa trên thời gian để cảnh báo
            if (elapsedTime < rankPerfect) timeText.color = Color.cyan;
            else if (elapsedTime < rankGood) timeText.color = Color.green;
            else if (elapsedTime < rankNormal) timeText.color = Color.yellow;
            else timeText.color = Color.red;

            timeText.text = $"Time: {elapsedTime:F1}s";
        }
    }

    void UpdateVisuals()
    {
        if (tempSlider != null) tempSlider.value = currentTemp;
        if (masterySlider != null) masterySlider.value = currentProgress;

        // Đổi màu thanh sắt
        if (ingotRenderer != null) 
        {
            ingotRenderer.material.color = heatColorGradient.Evaluate(currentTemp);
            ingotRenderer.material.SetColor("_EmissionColor", heatColorGradient.Evaluate(currentTemp) * 2f); 
        }

        // Âm thanh
        if (sizzleAudio != null)
        {
            sizzleAudio.pitch = 0.8f + currentTemp * 0.5f; 
            sizzleAudio.volume = currentTemp;     
        }
    }

    void FinishHeating()
    {
        isHeatingActive = false;
        string grade = "";
        Color gradeColor = Color.white;

        // Logic Xếp Hạng
        if (elapsedTime <= rankPerfect) // < 25s
        {
            grade = "HUYỀN THOẠI (LEGENDARY)!";
            gradeColor = Color.cyan;
            Debug.Log("Rank: Perfect");
        }
        else if (elapsedTime <= rankGood) // < 35s
        {
            grade = "TỐT (GOOD)";
            gradeColor = Color.green;
            Debug.Log("Rank: Good");
        }
        else if (elapsedTime <= rankNormal) // < 60s
        {
            grade = "TRUNG BÌNH (NORMAL)";
            gradeColor = Color.yellow;
            Debug.Log("Rank: Normal");
        }
        else // < 90s (Vì >90s đã bị Fail ở Update rồi)
        {
            grade = "TỆ (BAD)";
            gradeColor = new Color(1f, 0.5f, 0f); // Cam đậm
            Debug.Log("Rank: Bad");
        }

        if (rankText != null)
        {
            rankText.text = grade;
            rankText.color = gradeColor;
        }

        // Dừng hiệu ứng
        if (fireEffect != null) fireEffect.Stop();
        if (sizzleAudio != null) sizzleAudio.Stop();

        // Chờ 2 giây để người chơi nhìn thấy Rank rồi mới chuyển cảnh
        Invoke("GoToSmithing", 2.0f);
    }

    void GoToSmithing()
    {
        if (heatingCanvas != null) heatingCanvas.SetActive(false);
        if (SmithingManager.Instance != null) SmithingManager.Instance.StartSmithingPhase();
    }

    void FailGame(string reason)
    {
        isGameOver = true;
        if (rankText != null) 
        {
            rankText.text = reason;
            rankText.color = Color.red;
        }
        
        Debug.Log("GAME OVER: " + reason);
        // Reset lại sau 2s
        Invoke("StartHeatingPhase", 2.0f);
    }
}