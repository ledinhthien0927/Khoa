using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI")]
    public GameObject panel;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;

    public Button skipBtn;
    public Button acceptBtn;

    public TextMeshProUGUI spaceHint;

    [Header("Typewriter")]
    public float speed = 0.03f;

    DialogueLine[] lines;
    int index;

    bool typing;
    bool lockInput;

    Coroutine typingCo;

    System.Action onFinish;
    System.Action onAccept;

    // ===== SỬA Ở ĐÂY =====
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

        skipBtn.gameObject.SetActive(false);
        acceptBtn.gameObject.SetActive(false);
        spaceHint.gameObject.SetActive(false);
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

    // ================= SHOW =================

    public void Show(
        DialogueLine[] data,
        NPCController npc = null,
        System.Action finish = null,
        System.Action accept = null)
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
    }

    // ================= LINE =================

    void ShowLine()
    {
        DialogueLine line = lines[index];

        nameText.text = line.speaker;

        // Camera focus
        if (line.focusTarget != null &&
            DialogueCamera.Instance != null)
        {
            DialogueCamera.Instance.Focus(line.focusTarget);
        }

        // Animation
        if (line.actor != null)
        {
            line.actor.Play(line.animationTrigger);
            line.actor.SetTalking(true);
        }

        // Typewriter
        if (typingCo != null)
            StopCoroutine(typingCo);

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

        DialogueLine line = lines[index];

        if (line.actor != null)
            line.actor.SetTalking(false);
    }

    // ================= NEXT =================

    public void Next()
    {
        // Skip typing
        if (typing)
        {
            StopCoroutine(typingCo);

            contentText.text = lines[index].text;
            typing = false;

            DialogueLine line = lines[index];

            if (line.actor != null)
                line.actor.SetTalking(false);

            return;
        }

        // Last line
        if (index >= lines.Length - 1)
        {
            UpdateUI();
            return;
        }

        index++;

        ShowLine();
        UpdateUI();
    }

    // ================= UI =================

    void UpdateUI()
    {
        DialogueLine line = lines[index];

        bool isLast = index == lines.Length - 1;

        lockInput = false;

        skipBtn.gameObject.SetActive(false);
        acceptBtn.gameObject.SetActive(false);
        spaceHint.gameObject.SetActive(false);

        // Accept button
        if (line.showAccept)
        {
            lockInput = true;

            acceptBtn.gameObject.SetActive(true);
            return;
        }

        // Last → Skip
        if (isLast)
        {
            skipBtn.gameObject.SetActive(true);
        }
        else
        {
            spaceHint.gameObject.SetActive(true);
        }
    }

    // ================= BUTTON =================

    public void Accept()
    {
        onAccept?.Invoke();
        Close();
    }

    public void Skip()
    {
        Close();
    }

    // ================= CLOSE =================

    void Close()
    {
        panel.SetActive(false);

        skipBtn.gameObject.SetActive(false);
        acceptBtn.gameObject.SetActive(false);
        spaceHint.gameObject.SetActive(false);

        lockInput = false;

        if (typingCo != null)
            StopCoroutine(typingCo);

        if (DialogueCamera.Instance != null)
            DialogueCamera.Instance.ResetCam();

        // Báo cho NPC kết thúc
        if (currentNPC != null)
            currentNPC.OnDialogueFinished();

        currentNPC = null;

        onFinish?.Invoke();
    }
}
