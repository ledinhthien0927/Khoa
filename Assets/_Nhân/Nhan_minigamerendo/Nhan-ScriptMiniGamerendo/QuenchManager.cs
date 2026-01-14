using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;
using System.Collections.Generic; 
using System.Linq; 

public class QuenchManager : MonoBehaviour
{
    public static QuenchManager Instance;

    [Header("Connection")]
    public ShowcaseResult showcaseManager; 
    public GameObject finishedItemPrefab; 

    [Header("Previous Scores (Test Data)")]
    public float previousHammerScore = 95f; 
    public float previousHeatScore = 85f;   

    [Header("UI - Main")]
    public GameObject quenchCanvas;
    public GameObject backgroundCanvas; 
    
    [Header("--- TUTORIAL (HƯỚNG DẪN) ---")] // <--- MỚI
    public GameObject tutorialPanel;         // Kéo Panel hướng dẫn vào đây
    public Button closeTutorialButton;       // Kéo Button "Đã hiểu" vào đây

    [Header("UI - Gameplay Bar")]
    public GameObject quenchPanel;      
    public RectTransform barBG;         
    public RectTransform targetZone;    
    public RectTransform perfectLine;   
    public RectTransform cursor;        
    public TextMeshProUGUI rangePreviewText; 

    [Header("--- 1. FLOATING TEXT ---")]
    public TextMeshProUGUI floatingText; 
    public float floatSpeed = 100f;      
    public float floatDuration = 1.0f;   

    [Header("--- 2. SELECTION PANEL ---")]
    public GameObject selectionPanelObj;    
    public RectTransform selectionPanelRect; 
    
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
    public float panelScaleEnd = 1.5f; 
    public float animDuration = 0.8f;

    private bool movingRight = true;
    
    private List<string> collectedStats = new List<string>(); 
    private List<int> generatedValues = new List<int>();      
    private List<float> accuracyLog = new List<float>();      

    private bool isGameActive = false; 
    private bool readyToQuench = false; 
    private float barWidth;
    private bool isBonusActive = false; 
    
    private Vector2 startPanelPos;
    
    private int picksAllowed = 1;     
    private int currentPicks = 0;     
    private bool[] isLineSelected = new bool[3]; 

    void Awake() { Instance = this; }

    void Start()
    {
        if (selectionPanelRect != null) startPanelPos = selectionPanelRect.anchoredPosition;

        if (quenchPanel != null) quenchPanel.SetActive(false);
        if (quenchCamera != null) quenchCamera.SetActive(false);
        if (tongsAndItem != null) tongsAndItem.SetActive(false);
        if (barBG != null) barWidth = barBG.rect.width;
        
        if (confirmButton != null) {
            confirmButton.gameObject.SetActive(false); 
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        if (floatingText != null) floatingText.gameObject.SetActive(false);
        
        // --- MỚI: Ẩn Tutorial Panel khi game load ---
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        
        // --- MỚI: Gán sự kiện cho nút đóng ---
        if (closeTutorialButton != null) 
            closeTutorialButton.onClick.AddListener(CloseTutorial);

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
        generatedValues.Clear(); 
        accuracyLog.Clear();     
        
        ResetUI();
        
        if (selectionPanelObj != null) selectionPanelObj.SetActive(false);
        if (selectionPanelRect != null) {
            selectionPanelRect.anchoredPosition = startPanelPos; 
            selectionPanelRect.localScale = Vector3.one; 
        }

        CheckLegendaryBonus();
        UpdateRangeText();

        if (resultText != null) {
            resultText.text = isBonusActive ? "<color=yellow>LEGENDARY BONUS!</color>" : "NHẤN SPACE!";
            if (isBonusActive && bonusAudio) bonusAudio.Play();
        }
        
        RandomizeTargetZone();

        // --- MỚI: Logic hiển thị Tutorial ---
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            readyToQuench = false; // Chưa cho chơi, chờ tắt hướng dẫn
        }
        else
        {
            readyToQuench = true; // Không có hướng dẫn thì chơi luôn
        }
    }

    // --- MỚI: Hàm đóng Tutorial ---
    public void CloseTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        readyToQuench = true; // Lúc này mới cho phép bấm Space
    }
    // -----------------------------

    void CheckLegendaryBonus()
    {
        // Logic bonus cũ
        isBonusActive = false; 
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
        // Nếu chưa sẵn sàng (đang hiện Tutorial), không làm gì cả
        if (!readyToQuench && !isGameActive) return;

        // Trạng thái chờ bấm Space lần đầu để bắt đầu
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
            if (hitAudio != null) hitAudio.Play();
            GenerateAndAddStat(accuracy); 
        } else if (distance <= goodThreshold) {
            Debug.Log("GOOD!");
            if (hitAudio != null) hitAudio.Play();
            GenerateAndAddStat(accuracy);
        } else {
            Debug.Log("MISS!");
            if (resultText != null) { resultText.text = "MISS!"; resultText.color = Color.red; }
            if (missAudio != null) missAudio.Play();
            
            ShowFloatingText("MISS!", Color.red);
            RemoveLastStat();
        }

        PlayEffects();

        if (collectedStats.Count >= 3) StartCoroutine(WinAnimationSequence());
        else StartCoroutine(ContinueGameDelay());
    }

    void GenerateAndAddStat(float accuracy)
    {
        accuracyLog.Add(accuracy * 100f);

        string type = "Sát thương"; 
        int min = minBaseStat; int max = maxBaseStat;
        if (isBonusActive) { min += 15; max += 30; }
        
        float rawValue = Mathf.Lerp(min, max, accuracy);
        int value = Mathf.RoundToInt(rawValue);

        generatedValues.Add(value);

        string colorHex = "#FFFFFF"; 
        Color flashColor = Color.white;

        if (value >= max * 0.9f) { colorHex = "#FF0000"; flashColor = Color.red; }
        else if (value >= max * 0.7f) { colorHex = "#A020F0"; flashColor = new Color(0.6f, 0.2f, 0.9f); } 
        else if (value >= max * 0.4f) { colorHex = "#00FFFF"; flashColor = Color.cyan; }
        
        string statString = $"<color={colorHex}>+{value} {type}</color>";
        if (collectedStats.Count < 3) collectedStats.Add(statString);

        ShowFloatingText($"+{value} {type}", flashColor);
    }

    void ShowFloatingText(string content, Color color)
    {
        if (floatingText != null)
        {
            StopCoroutine("AnimateFloatingText"); 
            StartCoroutine(AnimateFloatingText(content, color));
        }
    }

    IEnumerator AnimateFloatingText(string content, Color color)
    {
        floatingText.gameObject.SetActive(true);
        floatingText.text = content;
        floatingText.color = color;
        
        if (cursor != null) 
            floatingText.rectTransform.anchoredPosition = new Vector2(cursor.anchoredPosition.x, 50f); 

        float elapsed = 0f;
        Vector2 startPos = floatingText.rectTransform.anchoredPosition;
        Color startColor = color;

        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / floatDuration;
            floatingText.rectTransform.anchoredPosition = startPos + new Vector2(0, floatSpeed * t);
            floatingText.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }
        floatingText.gameObject.SetActive(false);
    }

    void RemoveLastStat() 
    { 
        if (collectedStats.Count > 0) 
        {
            collectedStats.RemoveAt(collectedStats.Count - 1);
            if (generatedValues.Count > 0) generatedValues.RemoveAt(generatedValues.Count - 1);
            if (accuracyLog.Count > 0) accuracyLog.RemoveAt(accuracyLog.Count - 1);
        }
    }

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
    }

    IEnumerator WinAnimationSequence() 
    { 
        isGameActive = false; 
        if (resultText != null) resultText.text = "";
        UpdateStatUI();

        yield return new WaitForSeconds(0.8f); 

        if (quenchPanel != null) quenchPanel.SetActive(false); 
        if (tongsAndItem != null) tongsAndItem.SetActive(false);

        if (selectionPanelObj != null) 
        {
            selectionPanelObj.SetActive(true); 
            if (selectionPanelRect != null)
            {
                float elapsed = 0;
                Vector2 startPos = selectionPanelRect.anchoredPosition;
                Vector3 startScale = selectionPanelRect.localScale;
                Vector2 endPos = Vector2.zero; 
                Vector3 targetScale = Vector3.one * panelScaleEnd;

                while (elapsed < animDuration) {
                    float t = elapsed / animDuration;
                    t = t * t * (3f - 2f * t); 
                    selectionPanelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    selectionPanelRect.localScale = Vector3.Lerp(startScale, targetScale, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                selectionPanelRect.anchoredPosition = endPos;
                selectionPanelRect.localScale = targetScale;
            }
        }

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

    public void OnRewardHoverEnter(int index) { if (!isLineSelected[index] && index < hoverFrames.Length && hoverFrames[index] != null) hoverFrames[index].SetActive(true); }
    public void OnRewardHoverExit(int index) { if (index < hoverFrames.Length && hoverFrames[index] != null) hoverFrames[index].SetActive(false); }

    public void OnRewardClick(int index)
    {
        if (isLineSelected[index]) {
            isLineSelected[index] = false;
            currentPicks--;
        } else {
            if (currentPicks < picksAllowed) {
                isLineSelected[index] = true;
                currentPicks++;
            } else { return; }
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
        
        float finalBaseDamage = 0;
        for (int i = 0; i < 3; i++)
        {
            if (isLineSelected[i] && i < generatedValues.Count)
            {
                finalBaseDamage += generatedValues[i];
            }
        }
        if (finalBaseDamage == 0 && generatedValues.Count > 0) finalBaseDamage = generatedValues[0];

        float currentQuenchScore = (accuracyLog.Count > 0) ? accuracyLog.Average() : 0;

        if (showcaseManager != null) 
        {
            showcaseManager.ShowResult(
                finishedItemPrefab, 
                finalBaseDamage,      
                previousHammerScore,  
                previousHeatScore,    
                currentQuenchScore    
            ); 
        }
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