using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip typeSound;
    public Button acceptButton; 

    DialogueLine[] lines;
    int index;
    string npcName;
    System.Action onFinish;

    bool isTyping;
    string fullText;
    Coroutine typing;
    
    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
         if (acceptButton != null)
        acceptButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!panel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                StopCoroutine(typing);
                dialogueText.text = fullText;
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    // =========================
    // MULTI LINE
    // =========================
   public void Show(string name, DialogueLine[] dialogueLines, System.Action finishCallback = null)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogError("DialogueUI.Show() nhận dialogueLines NULL hoặc rỗng");
            return;
        }

        npcName = name;
        lines = dialogueLines;
        index = 0;
        onFinish = finishCallback;

        foreach (var l in lines)
            l.eventPlayed = false;

        panel.SetActive(true);
        Time.timeScale = 0;

        ShowLine();
    }

    // =========================
    // SINGLE LINE
    // =========================
   public void ShowSingle(string name, DialogueLine line)
{
    if (line == null)
    {
        Debug.LogError("ShowSingle() nhận DialogueLine NULL");
        return;
    }

    Show(name, new DialogueLine[] { line }, null);
}

   void ShowLine()
{
    if (lines == null)
    {
        Debug.LogError("ShowLine(): lines = NULL");
        return;
    }

    if (index < 0 || index >= lines.Length)
    {
        Debug.LogError($"ShowLine(): index {index} vượt quá giới hạn dialogue");
        return;
    }

    if (lines[index] == null)
    {
        Debug.LogError($"DialogueLine tại index {index} bị NULL");
        return;
    }

    npcNameText.text = npcName;
    fullText = lines[index].text;

    PlayEvent(lines[index]);

    if (typing != null)
        StopCoroutine(typing);

    typing = StartCoroutine(TypeText());
}


    void NextLine()
    {
        index++;

        if (index >= lines.Length)
        {
            Hide();
            onFinish?.Invoke();
            return;
        }

        ShowLine();
    }

    IEnumerator TypeText()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in fullText)
        {
            dialogueText.text += c;
            if (typeSound) audioSource.PlayOneShot(typeSound);
            yield return new WaitForSecondsRealtime(0.03f);
        }

        isTyping = false;
    }

   void PlayEvent(DialogueLine line)
{
    if (line.eventPlayed) return;

    NPCController npc = NPCController.Current;
    if (npc == null) return;

    switch (line.dialogueEvent)
    {
        case DialogueEvent.PanToMalricFlag:
            DialogueCamera.Instance.PanTo(npc.malricIsland);
            break;

        case DialogueEvent.ZoomToBrokenHammer:
            DialogueCamera.Instance.Focus(npc.brokenHammer);
            break;

        case DialogueEvent.FocusOverShoulder:
            DialogueCamera.Instance.FocusOverShoulder(npc.npcFace);
            break;
    }

    line.eventPlayed = true;
}

    public void ShowAcceptButton(System.Action onAccept)
    {
        acceptButton.gameObject.SetActive(true);
        acceptButton.onClick.RemoveAllListeners();

        acceptButton.onClick.AddListener(() =>
        {
            acceptButton.gameObject.SetActive(false);
            Hide();
            onAccept?.Invoke();
        });
    }
   public void Hide()
{
    panel.SetActive(false);

    if (acceptButton != null)
        acceptButton.gameObject.SetActive(false);

    Time.timeScale = 1;
    DialogueCamera.Instance.ResetCam();
}

}
