using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class DialogueUI : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
{
    public static DialogueUI Instance;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;
    public Button skipButton;

    [Header("Typewriter")]
    public float typingSpeed = 0.04f;
    public AudioSource typingSound;

    Coroutine typingCoroutine;
    string currentText;
    bool isTyping;
    bool isIdleDialogue;
    public TextMeshProUGUI text;
    public Color normalColor = Color.white;
    public Color pressedColor = Color.red;

    void Awake()
    {
        text.color = normalColor;
        Instance = this;
        panel.SetActive(false);
       
    }
       public void OnPointerDown(PointerEventData eventData)
    {
        text.color = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        text.color = normalColor;
    }
   
   void Update()
{
    if (!panel.activeSelf) return;

    if (Input.GetKeyDown(KeyCode.Space))
    {
        if (isTyping)
        {
            SkipTyping();
        }
        else
        {
            if (isIdleDialogue)
                Hide();        // ✅ IDLE → ĐÓNG LUÔN
            else
                Next();
        }
    }
}
  public void Show(string npcName, string text, bool showNext, bool idle = false)
{
    panel.SetActive(true);
    Time.timeScale = 0f;

    npcNameText.text = npcName;
    currentText = text;
    isIdleDialogue = idle;

    nextButton.gameObject.SetActive(showNext && !idle);
    skipButton.gameObject.SetActive(!showNext || idle);

    // 🔥 THÊM DÒNG NÀY
    if (NPCController.Current != null)
    {
        DialogueCamera.Instance.FocusOn(
            NPCController.Current.cameraFocusPoint
        );
    }
Debug.Log("Focus NPC: " + NPCController.Current.name);
    StartTyping();
}


    void StartTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText());
    }

   IEnumerator TypeText()
{
    isTyping = true;
    dialogueText.text = "";

    if (typingSound)
    {
        typingSound.loop = true;
        typingSound.Play();
    }

    foreach (char c in currentText)
    {
        dialogueText.text += c;
        yield return new WaitForSecondsRealtime(typingSpeed);
    }

    StopTypingSound();
}    public void Next()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        NPCController.Current?.NextLine();
    }

    public void Skip()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        Hide();
    }
    void StopTypingSound()
{
    if (typingSound && typingSound.isPlaying)
    {
        typingSound.loop = false;
        typingSound.Stop();
    }

    isTyping = false;
}

   void SkipTyping()
{
    if (typingCoroutine != null)
        StopCoroutine(typingCoroutine);

    dialogueText.text = currentText;
    StopTypingSound();
}


 public void Hide()
{
    panel.SetActive(false);
    Time.timeScale = 1f;
    isIdleDialogue = false;

    // 🔥 RESET CAMERA
    DialogueCamera.Instance.ResetCamera();

    NPCController.Current = null;
}

 
}
