using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI References")]
    public GameObject panel; // Panel hội thoại

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;

    public Button skipBtn;
    public Button acceptBtn;

    public TextMeshProUGUI spaceHint;

    [Header("Settings")]
    public float speed = 0.03f;

    // --- BIẾN NỘI BỘ ---
    DialogueLine[] lines;
    int index;
    bool typing;
    bool lockInput;
    Coroutine typingCo;

    System.Action onFinish;
    System.Action onAccept;

    NPCController currentNPC;

    public bool IsShowing => panel.activeSelf;

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        panel.SetActive(false);
        ResetButtons();
    }

    // ================= INPUT =================

    void Update()
    {
        if (!panel.activeSelf) return;
        if (lockInput) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Next();
        }
    }

    // ================= SHOW (BẮT ĐẦU THOẠI) =================

    public void Show(DialogueLine[] data, NPCController npc = null, System.Action finish = null, System.Action accept = null)
    {
        if (data == null || data.Length == 0) return;

        lines = data;
        index = 0;
        currentNPC = npc;
        onFinish = finish;
        onAccept = accept;

        panel.SetActive(true); 

        ShowLine();
        UpdateUI();

        // 1. TẮT HUD NGƯỜI CHƠI (Máu, Stamina...)
        TogglePlayerHUD(false);

        // 2. BẬT CHUỘT
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 3. [MỚI] TẮT BẢNG NHIỆM VỤ (QUEST UI)
        if (QuestUIManager.Instance != null)
        {
            QuestUIManager.Instance.SetQuestUIVisible(false);
        }
    }

    // ================= LINE LOGIC =================

    void ShowLine()
    {
        DialogueLine line = lines[index];
        nameText.text = line.speaker;

        if (line.focusTarget != null && DialogueCamera.Instance != null)
        {
            DialogueCamera.Instance.Focus(line.focusTarget);
        }

        if (line.actor != null)
        {
            line.actor.Play(line.animationTrigger);
            line.actor.SetTalking(true);
        }

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeText(line.text));
    }

    IEnumerator TypeText(string s)
    {
        typing = true;
        contentText.text = "";
        foreach (char c in s)
        {
            contentText.text += c;
            yield return new WaitForSeconds(speed);
        }
        typing = false;

        if (lines[index].actor != null)
            lines[index].actor.SetTalking(false);
    }

    // ================= NEXT LINE =================

    public void Next()
    {
        if (typing)
        {
            StopCoroutine(typingCo);
            contentText.text = lines[index].text;
            typing = false;
            if (lines[index].actor != null) lines[index].actor.SetTalking(false);
            return;
        }

        if (index >= lines.Length - 1)
        {
            UpdateUI(); 
            return;
        }

        index++;
        ShowLine();
        UpdateUI();
    }

    // ================= UI STATE =================

    void UpdateUI()
    {
        DialogueLine line = lines[index];
        bool isLast = index == lines.Length - 1;

        lockInput = false;
        ResetButtons();

        if (line.showAccept)
        {
            lockInput = true;
            acceptBtn.gameObject.SetActive(true);
            return;
        }

        if (isLast)
        {
            skipBtn.gameObject.SetActive(true);
        }
        else
        {
            spaceHint.gameObject.SetActive(true);
        }
    }

    void ResetButtons()
    {
        skipBtn.gameObject.SetActive(false);
        acceptBtn.gameObject.SetActive(false);
        spaceHint.gameObject.SetActive(false);
    }

    // ================= BUTTON EVENTS =================

    public void Accept()
    {
        onAccept?.Invoke();
        Close();
    }

    public void Skip()
    {
        Close();
    }

    // ================= CLOSE (KẾT THÚC THOẠI) =================

    void Close()
    {
        panel.SetActive(false); 
        ResetButtons();
        lockInput = false;

        if (typingCo != null) StopCoroutine(typingCo);

        if (DialogueCamera.Instance != null)
            DialogueCamera.Instance.ResetCam();

        if (currentNPC != null)
        {
            currentNPC.OnDialogueFinished();
            currentNPC = null;
        }

        onFinish?.Invoke();

        // 1. HIỆN LẠI HUD NGƯỜI CHƠI
        TogglePlayerHUD(true);

        // 2. [MỚI] HIỆN LẠI BẢNG NHIỆM VỤ (QUEST UI)
        if (QuestUIManager.Instance != null)
        {
            QuestUIManager.Instance.SetQuestUIVisible(true);
        }

        // Lưu ý: Chuột sẽ được PlayerController tự động khóa lại khi HUD bật lên
    }

    // ================= TIỆN ÍCH =================

    void TogglePlayerHUD(bool show)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            if (show && player.IsTraveling) 
            {
                return; 
            }

            PlayerView view = player.GetView();
            if (view != null)
            {
                view.ToggleCombatUI(show); 
            }
        }
    }
}