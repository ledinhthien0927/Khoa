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

    NPCController currentNPC;

    public bool IsShowing => panel != null && panel.activeSelf;

    // ================= INIT =================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        panel?.SetActive(false);

        skipBtn?.gameObject.SetActive(false);
        acceptBtn?.gameObject.SetActive(false);
        spaceHint?.gameObject.SetActive(false);
    }

    // ================= INPUT =================

    void Update()
    {
        if (!IsShowing) return;
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
        if (index < 0 || index >= lines.Length) return;

        DialogueLine line = lines[index];

        nameText.text = line.speaker;

        // Camera
        if (line.focusTarget != null &&
            DialogueCamera.Instance != null)
        {
            DialogueCamera.Instance.Focus(line.focusTarget);
        }

        // Animation
        if (line.actor != null &&
            !string.IsNullOrEmpty(line.animationTrigger))
        {
            line.actor.Play(line.animationTrigger);
            line.actor.SetTalking(true);
        }

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

        if (lines[index].actor != null)
            lines[index].actor.SetTalking(false);
    }

    // ================= NEXT =================

    public void Next()
    {
        if (lines == null) return;

        // Skip typing
        if (typing)
        {
            StopCoroutine(typingCo);

            contentText.text = lines[index].text;
            typing = false;

            if (lines[index].actor != null)
                lines[index].actor.SetTalking(false);

            return;
        }

        // Last
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

        bool last = index == lines.Length - 1;

        lockInput = false;

        skipBtn?.gameObject.SetActive(false);
        acceptBtn?.gameObject.SetActive(false);
        spaceHint?.gameObject.SetActive(false);

        if (line.showAccept)
        {
            lockInput = true;
            acceptBtn?.gameObject.SetActive(true);
            return;
        }

        if (last)
            skipBtn?.gameObject.SetActive(true);
        else
            spaceHint?.gameObject.SetActive(true);
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
        panel?.SetActive(false);

        skipBtn?.gameObject.SetActive(false);
        acceptBtn?.gameObject.SetActive(false);
        spaceHint?.gameObject.SetActive(false);

        lockInput = false;

        if (typingCo != null)
            StopCoroutine(typingCo);

        if (DialogueCamera.Instance != null)
            DialogueCamera.Instance.ResetCam();

        if (currentNPC != null)
            currentNPC.OnDialogueFinished();

        currentNPC = null;

        onFinish?.Invoke();
    }
}
