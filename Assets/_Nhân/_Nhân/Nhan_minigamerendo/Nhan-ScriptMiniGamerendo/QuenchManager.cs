using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;
using System.Collections.Generic; 

public class QuenchManager : MonoBehaviour
{
    public static QuenchManager Instance;

    [Header("Connection")]
    public ShowcaseResult showcaseManager; 
    
    [Header("--- RESULT PREFABS ---")]
    public GameObject swordResultPrefab;   
    public GameObject hammerResultPrefab;  
    
    private GameObject currentFinishedItemPrefab; 

    [Header("UI - Main")]
    public GameObject quenchCanvas;
    public GameObject backgroundCanvas; 
    
    [Header("--- TUTORIAL (HƯỚNG DẪN) ---")]
    public GameObject tutorialPanel;        
    public Button closeTutorialButton;      

    [Header("UI - Gameplay Bar")]
    public GameObject quenchPanel;      
    public RectTransform barBG;         
    public RectTransform targetZone;    
    public RectTransform perfectLine;   
    public RectTransform cursor;        
    
    [Header("UI - SPRITES & TEXT")]
    public TextMeshProUGUI resultText; 

    [Header("Visuals & Audio")]
    public GameObject mainCamera;       
    public GameObject quenchCamera;     
    public GameObject tongsAndItem;     
    public Animator tongsAnimator;      
    public Renderer ingotRenderer;      
    public ParticleSystem steamEffect;  
    public AudioSource hissAudio;       
    public AudioSource hitAudio;        
    public AudioSource missAudio;
    public Button confirmButton; 

    [Header("--- WEAPON OBJECTS (Gắn vào Kẹp) ---")]
    public GameObject swordObjectOnTongs;  
    public GameObject hammerObjectOnTongs; 
    
    // --- [SỬA ĐỔI MỚI] HỆ THỐNG MÀU SẮC KHI TÔI RÈN ---
    [Header("--- QUENCHING COLORS ---")]
    public Color hotColor = new Color(1f, 0.35f, 0f); // Màu đỏ cam (đang nóng)
    public Color coolColor = new Color(0.6f, 0.6f, 0.6f); // Màu xám (thép nguội)
    public int requiredQuenches = 3; // Số lần nhúng nước cần thiết để nguội hẳn
    private int currentQuenchCount = 0; // Đếm số lần nhúng thành công
    // --------------------------------------------------

    [Header("Settings")]
    public float moveSpeed = 500f;      

    private bool movingRight = true;
    private bool isGameActive = false; 
    private bool readyToQuench = false; 
    private float barWidth;

    void Awake() { Instance = this; }

    void Start()
    {
        if (quenchPanel != null) quenchPanel.SetActive(false);
        if (quenchCamera != null) quenchCamera.SetActive(false);
        if (tongsAndItem != null) tongsAndItem.SetActive(false);
        if (barBG != null) barWidth = barBG.rect.width;
        
        if (confirmButton != null) {
            confirmButton.gameObject.SetActive(false); 
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (closeTutorialButton != null) closeTutorialButton.onClick.AddListener(CloseTutorial);

        if (swordObjectOnTongs != null) swordObjectOnTongs.SetActive(false);
        if (hammerObjectOnTongs != null) hammerObjectOnTongs.SetActive(false);
    }

    public void StartQuenchSequence()
    {
        if (mainCamera != null) mainCamera.SetActive(false);         
        if (quenchCamera != null) quenchCamera.SetActive(true);
        if (quenchCanvas != null) quenchCanvas.SetActive(true);
        if (backgroundCanvas != null) backgroundCanvas.SetActive(true);
        if (tongsAndItem != null) tongsAndItem.SetActive(true);

        // 1. Tắt hết visual trước
        if (swordObjectOnTongs != null) swordObjectOnTongs.SetActive(false);
        if (hammerObjectOnTongs != null) hammerObjectOnTongs.SetActive(false);

        // 2. Kiểm tra loại vũ khí & Setup màu Nóng
        currentQuenchCount = 0;
        if (HeatingManager.CurrentWeaponType == WeaponType.Sword)
        {
            if (swordObjectOnTongs != null) swordObjectOnTongs.SetActive(true);
            currentFinishedItemPrefab = swordResultPrefab;
            SetWeaponColor(swordObjectOnTongs, hotColor);
        }
        else if (HeatingManager.CurrentWeaponType == WeaponType.Hammer)
        {
            if (hammerObjectOnTongs != null) hammerObjectOnTongs.SetActive(true);
            currentFinishedItemPrefab = hammerResultPrefab;
            SetWeaponColor(hammerObjectOnTongs, hotColor);
        }

        if (resultText != null) {
            resultText.text = "NHẤN SPACE!";
            resultText.color = Color.white;
        }
        
        RandomizeTargetZone();

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            readyToQuench = false; 
        }
        else
        {
            readyToQuench = true; 
        }
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        readyToQuench = true; 
    }

    void Update()
    {
        if (!readyToQuench && !isGameActive) return;

        if (readyToQuench) {
            if (Input.GetKeyDown(KeyCode.Space)) {
                readyToQuench = false; 
                if (tongsAnimator != null) tongsAnimator.SetTrigger("Dig");
                StartActualGame();
            }
            return; 
        }

        if (!isGameActive) return;

        MoveCursor();

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (tongsAnimator != null) tongsAnimator.SetTrigger("Dig"); 
            CheckHit();
        }
    }

    void MoveCursor() 
    { 
        if (cursor == null) return;
        float limitX = (barWidth / 2) - (cursor.rect.width / 2); 
        float currentX = cursor.anchoredPosition.x; 
        
        if (movingRight) { 
            currentX += moveSpeed * Time.deltaTime; 
            if (currentX >= limitX) movingRight = false; 
        } else { 
            currentX -= moveSpeed * Time.deltaTime; 
            if (currentX <= -limitX) movingRight = true; 
        } 
        cursor.anchoredPosition = new Vector2(currentX, 0); 
    }

    void RandomizeTargetZone()
    {
        if (targetZone == null) return;
        float safeMargin = targetZone.rect.width / 2 + 50f;
        float range = (barWidth / 2) - safeMargin;
        float randomX = Random.Range(-range, range);
        targetZone.anchoredPosition = new Vector2(randomX, 0);
        if (perfectLine != null) perfectLine.anchoredPosition = Vector2.zero;
    }

    void CheckHit()
    {
        isGameActive = false; 
        float distance = Mathf.Abs(cursor.anchoredPosition.x - targetZone.anchoredPosition.x);
        float goodThreshold = targetZone.rect.width / 2;

        if (distance <= goodThreshold) {
            // HIT -> Đổi màu nhạt dần
            if (hitAudio != null) hitAudio.Play();
            currentQuenchCount++;
            UpdateWeaponCooling();
            if (resultText != null) { resultText.text = "GOOD!"; resultText.color = Color.green; }
        } else {
            // MISS -> Không đổi màu, phải làm lại
            if (missAudio != null) missAudio.Play();
            if (resultText != null) { resultText.text = "MISS!"; resultText.color = Color.red; }
        }

        PlayEffects();

        if (currentQuenchCount >= requiredQuenches) {
            StartCoroutine(WinAnimationSequence());
        } else {
            StartCoroutine(ContinueGameDelay());
        }
    }

    // --- [SỬA ĐỔI MỚI] HÀM CẬP NHẬT MÀU SẮC ---
    void UpdateWeaponCooling()
    {
        float progress = (float)currentQuenchCount / requiredQuenches;
        Color currentColor = Color.Lerp(hotColor, coolColor, progress);

        if (HeatingManager.CurrentWeaponType == WeaponType.Sword) {
            SetWeaponColor(swordObjectOnTongs, currentColor);
        } else if (HeatingManager.CurrentWeaponType == WeaponType.Hammer) {
            SetWeaponColor(hammerObjectOnTongs, currentColor);
        }
    }

    void SetWeaponColor(GameObject weaponObj, Color color)
    {
        if (weaponObj == null) return;
        Renderer[] renderers = weaponObj.GetComponentsInChildren<Renderer>(true);
        foreach(var r in renderers) {
            r.material.color = color;
        }
    }
    // ------------------------------------------

    IEnumerator WinAnimationSequence() 
    { 
        isGameActive = false; 
        if (resultText != null) {
            resultText.text = "HOÀN THÀNH!";
            resultText.color = Color.yellow;
        }

        yield return new WaitForSeconds(1.5f); 

        if (quenchPanel != null) quenchPanel.SetActive(false); 
        if (tongsAndItem != null) tongsAndItem.SetActive(false);
        if (confirmButton != null) confirmButton.gameObject.SetActive(true);
    }

    public void OnConfirmClicked()
    {
        if (quenchCanvas != null) quenchCanvas.SetActive(false);
        if (backgroundCanvas != null) backgroundCanvas.SetActive(false);
        
        if (showcaseManager != null) 
        {
            // Bỏ tính chỉ số -> Truyền toàn bộ 0 vào Showcase
            showcaseManager.ShowResult(
                currentFinishedItemPrefab, 
                0, 
                0, 
                0, 
                0  
            ); 
        }
    }

    void PlayEffects() { if(hissAudio) hissAudio.PlayOneShot(hissAudio.clip); StartCoroutine(PlaySteamDelayed()); }
    IEnumerator PlaySteamDelayed() { yield return new WaitForSeconds(0.5f); if(steamEffect) steamEffect.Play(); }
    
    IEnumerator ContinueGameDelay() {
        yield return new WaitForSeconds(1.0f);
        RandomizeTargetZone(); 
        isGameActive = true;
        if (resultText != null) { resultText.text = "NHẤN SPACE!"; resultText.color = Color.white; }
    }
    
    void StartActualGame() { isGameActive = true; if (quenchPanel != null) quenchPanel.SetActive(true); }
}