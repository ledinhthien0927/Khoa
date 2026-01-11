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
    public GameObject finishedItemPrefab; 

    [Header("UI - Main")]
    public GameObject quenchCanvas;
    public GameObject backgroundCanvas; 
    
    [Header("UI - Gameplay Bar")]
    public GameObject quenchPanel;      
    public RectTransform barBG;         
    public RectTransform targetZone;    
    public RectTransform perfectLine;   
    public RectTransform cursor;        
    public TextMeshProUGUI rangePreviewText; 

    [Header("UI - Stat Panel (QUAN TRỌNG)")]
    public RectTransform statPanelRect; // Kéo cái bảng chứa 3 dòng chỉ số vào đây
    
    [Header("UI - REWARD BUTTONS")]
    public GameObject[] rewardButtons; 
    public TextMeshProUGUI[] statLines; 
    public Image[] skullImages; 
    public GameObject[] hoverFrames;
    public GameObject[] selectedFrames;

    [Header("UI - SPRITES & TEXT")]
    public Sprite skullNormalSprite;   
    public Sprite skullGlowingSprite;  
    public TextMeshProUGUI resultText; 
    public TextMeshProUGUI titleText;  

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
    public AudioSource bonusAudio; 
    public Button confirmButton; 

    [Header("Settings")]
    public float moveSpeed = 500f;      
    public int minBaseStat = 10;
    public int maxBaseStat = 50;
    
    [Tooltip("Độ phóng to: 1.5 là to gấp rưỡi")]
    public float panelScaleEnd = 1.5f; 
    [Tooltip("Thời gian bảng bay ra")]
    public float animDuration = 0.8f;

    private bool movingRight = true;
    private List<string> collectedStats = new List<string>(); 
    private bool isGameActive = false; 
    private bool readyToQuench = false; 
    private float barWidth;
    private bool isBonusActive = false; 
    
    // Lưu vị trí và scale ban đầu để reset khi chơi lại
    private Vector2 startPanelPos;
    private Vector3 startPanelScale; 
    
    private int picksAllowed = 1;     
    private int currentPicks = 0;     
    private bool[] isLineSelected = new bool[3]; 

    void Awake() { Instance = this; }

    void Start()
    {
        // Lưu lại vị trí ban đầu của bảng (Vị trí bạn đặt trong Editor)
        if (statPanelRect != null) 
        {
            startPanelPos = statPanelRect.anchoredPosition;
            startPanelScale = statPanelRect.localScale;
        }

        if (quenchPanel != null) quenchPanel.SetActive(false);
        if (quenchCamera != null) quenchCamera.SetActive(false);
        if (tongsAndItem != null) tongsAndItem.SetActive(false);
        if (barBG != null) barWidth = barBG.rect.width;
        if (confirmButton != null) {
            confirmButton.gameObject.SetActive(false); 
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        ResetUI();
    }

    public void StartQuenchSequence()
    {
        if (mainCamera != null) mainCamera.SetActive(false);         
        if (quenchCamera != null) quenchCamera.SetActive(true);
        if (quenchCanvas != null) quenchCanvas.SetActive(true);
        if (backgroundCanvas != null) backgroundCanvas.SetActive(true);
        if (tongsAndItem != null) tongsAndItem.SetActive(true);

        collectedStats.Clear();
        ResetUI();
        
        // Trả bảng về vị trí cũ và kích thước cũ (Reset)
        if (statPanelRect != null) {
            statPanelRect.anchoredPosition = startPanelPos;
            statPanelRect.localScale = Vector3.one; 
        }

        CheckLegendaryBonus();
        UpdateRangeText();

        if (resultText != null) {
            resultText.text = isBonusActive ? "<color=yellow>LEGENDARY BONUS!</color>" : "NHẤN SPACE!";
            if (isBonusActive && bonusAudio) bonusAudio.Play();
        }
        
        RandomizeTargetZone();
        readyToQuench = true;
    }

    void CheckLegendaryBonus()
    {
        if (HeatingManager.IsLegendary && SmithingManager.IsLegendary) isBonusActive = true;
        else isBonusActive = false;
    }

    void UpdateRangeText()
    {
        if (rangePreviewText != null) {
            int min = isBonusActive ? minBaseStat + 10 : minBaseStat;
            int max = isBonusActive ? maxBaseStat + 20 : maxBaseStat;
            rangePreviewText.text = $"Phạm vi: <color=yellow>{min} - {max}</color>";
        }
    }

    void Update()
    {
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
        float perfectThreshold = (perfectLine != null) ? perfectLine.rect.width / 2 + 10f : 15f; 
        float goodThreshold = targetZone.rect.width / 2;
        float accuracy = 1.0f - (distance / goodThreshold);
        accuracy = Mathf.Clamp01(accuracy);

        if (distance <= perfectThreshold) {
            Debug.Log("PERFECT!");
            if (resultText != null) { resultText.text = "PERFECT!"; resultText.color = Color.cyan; }
            if (hitAudio != null) hitAudio.Play();
            GenerateAndAddStat(accuracy); 
        } else if (distance <= goodThreshold) {
            Debug.Log("GOOD!");
            if (resultText != null) { resultText.text = "GOOD"; resultText.color = Color.green; }
            if (hitAudio != null) hitAudio.Play();
            GenerateAndAddStat(accuracy);
        } else {
            Debug.Log("MISS!");
            if (resultText != null) { resultText.text = "MISS!"; resultText.color = Color.red; }
            if (missAudio != null) missAudio.Play();
            RemoveLastStat();
        }

        PlayEffects();
        UpdateStatUI(); 

        if (collectedStats.Count >= 3) StartCoroutine(WinAnimationSequence());
        else StartCoroutine(ContinueGameDelay());
    }

    void GenerateAndAddStat(float accuracy)
    {
        string type = "Sát thương"; 
        int min = minBaseStat; int max = maxBaseStat;
        if (isBonusActive) { min += 15; max += 30; }
        
        float rawValue = Mathf.Lerp(min, max, accuracy);
        int value = Mathf.RoundToInt(rawValue);

        string colorHex = "#FFFFFF"; 
        if (value >= max * 0.9f) colorHex = "#FF0000"; 
        else if (value >= max * 0.7f) colorHex = "#A020F0"; 
        else if (value >= max * 0.4f) colorHex = "#00FFFF"; 
        
        string statString = $"<color={colorHex}>+{value} {type}</color>";
        if (collectedStats.Count < 3) collectedStats.Add(statString);
    }

    void RemoveLastStat() { if (collectedStats.Count > 0) collectedStats.RemoveAt(collectedStats.Count - 1); }

    void UpdateStatUI()
    {
        for (int i = 0; i < 3; i++)
        {
            if (statLines[i] != null) statLines[i].text = (i < collectedStats.Count) ? collectedStats[i] : "---";
            if (skullImages[i] != null)
            {
                skullImages[i].color = Color.white;
                skullImages[i].sprite = (i < collectedStats.Count) ? skullGlowingSprite : skullNormalSprite;
            }
        }
    }
    
    void ResetUI()
    {
        currentPicks = 0;
        for(int i=0; i<3; i++) isLineSelected[i] = false;
        for(int i=0; i<3; i++) {
            if (hoverFrames[i] != null) hoverFrames[i].SetActive(false);
            if (selectedFrames[i] != null) selectedFrames[i].SetActive(false);
        }
        UpdateStatUI();
    }

    IEnumerator WinAnimationSequence() 
    { 
        isGameActive = false; 
        if (resultText != null) resultText.text = "";
        yield return new WaitForSeconds(0.5f);

        if (quenchPanel != null) quenchPanel.SetActive(false);
        if (tongsAndItem != null) tongsAndItem.SetActive(false);

        // --- ANIMATION BAY RA GIỮA VÀ ZOOM ---
        if (statPanelRect != null)
        {
            float elapsed = 0;
            Vector2 startPos = statPanelRect.anchoredPosition;
            Vector3 startScale = statPanelRect.localScale;
            
            // Đích đến: Vector2.zero (Giữa màn hình - nếu neo đúng)
            Vector2 endPos = Vector2.zero; 
            Vector3 targetScale = Vector3.one * panelScaleEnd; // Phóng to

            while (elapsed < animDuration) {
                float t = elapsed / animDuration;
                t = t * t * (3f - 2f * t); // Smooth step

                statPanelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                statPanelRect.localScale = Vector3.Lerp(startScale, targetScale, t);

                elapsed += Time.deltaTime;
                yield return null;
            }
            
            statPanelRect.anchoredPosition = endPos;
            statPanelRect.localScale = targetScale;
        }
        // -----------------------------

        for (int i = 0; i < 3; i++) if(skullImages[i] != null) skullImages[i].sprite = skullNormalSprite;

        picksAllowed = isBonusActive ? 3 : 1; 
        if (titleText != null) titleText.text = $"BẠN ĐƯỢC CHỌN: {picksAllowed} DÒNG";

        if (confirmButton != null) confirmButton.gameObject.SetActive(true);

        foreach(var btn in rewardButtons) {
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if(cg == null) cg = btn.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }

    public void OnRewardHoverEnter(int index)
    {
        if (!isLineSelected[index] && index < hoverFrames.Length && hoverFrames[index] != null)
            hoverFrames[index].SetActive(true);
    }

    public void OnRewardHoverExit(int index)
    {
        if (index < hoverFrames.Length && hoverFrames[index] != null)
            hoverFrames[index].SetActive(false);
    }

    public void OnRewardClick(int index)
    {
        if (isLineSelected[index]) {
            isLineSelected[index] = false;
            currentPicks--;
        } else {
            if (currentPicks < picksAllowed) {
                isLineSelected[index] = true;
                currentPicks++;
            } else {
                return; 
            }
        }
        UpdateRewardVisuals(index);
    }

    void UpdateRewardVisuals(int index)
    {
        if (selectedFrames[index] != null) selectedFrames[index].SetActive(isLineSelected[index]);
        if (isLineSelected[index] && hoverFrames[index] != null) hoverFrames[index].SetActive(false);
        if (skullImages[index] != null) skullImages[index].sprite = isLineSelected[index] ? skullGlowingSprite : skullNormalSprite;
    }

    public void OnConfirmClicked()
    {
        if (quenchCanvas != null) quenchCanvas.SetActive(false);
        if (backgroundCanvas != null) backgroundCanvas.SetActive(false);
        if (showcaseManager != null) showcaseManager.ShowResult(finishedItemPrefab, 100, 100, 100); 
    }

    void PlayEffects() { if(hissAudio) hissAudio.PlayOneShot(hissAudio.clip); StartCoroutine(PlaySteamDelayed()); }
    IEnumerator PlaySteamDelayed() { yield return new WaitForSeconds(0.5f); if(steamEffect) steamEffect.Play(); }
    IEnumerator ContinueGameDelay() {
        yield return new WaitForSeconds(1.0f);
        RandomizeTargetZone(); 
        isGameActive = true;
        if (resultText != null) resultText.text = "...";
    }
    void StartActualGame() { isGameActive = true; if (quenchPanel != null) quenchPanel.SetActive(true); }
}