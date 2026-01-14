using UnityEngine;


public enum DialoguePhase
{
    Intro,
    Mission,
    Complete
}

public class NPCController : MonoBehaviour
{
    public static NPCController Current;

    [Header("NPC Info")]
    public string npcName = "Hoàng Tử Valen";

    [Header("Dialogues")]
    public DialogueLine[] introDialogue;
    public DialogueLine[] missionDialogue;
    public string[] idleDialogue;
    public DialogueLine[] completeDialogue;

    DialoguePhase currentPhase;
    DialogueLine[] currentDialogue;
    int index;

    public QuestState questState = QuestState.NotStarted;
    public Transform cameraFocusPoint;
   
    

 public void Interact()
{
    Current = this;
    index = 0;

    // 🔥 FOCUS CAMERA 1 LẦN DUY NHẤT
    DialogueCamera.Instance.FocusOn(cameraFocusPoint);

    if (questState == QuestState.NotStarted)
    {
        currentPhase = DialoguePhase.Intro;
        currentDialogue = introDialogue;
        ShowLine();
    }
    else if (questState == QuestState.InProgress)
    {
        DialogueUI.Instance.Show(
            npcName,
            idleDialogue[Random.Range(0, idleDialogue.Length)],
            false,
            true
        );
    }
    else if (questState == QuestState.Completed)
    {
        currentPhase = DialoguePhase.Complete;
        currentDialogue = completeDialogue;
        ShowLine();
    }
}
    public void NextLine()
    {
        index++;

        if (index < currentDialogue.Length)
        {
            ShowLine();
            return;
        }

        // 🔁 CHUYỂN INTRO → MISSION
        if (currentPhase == DialoguePhase.Intro)
        {
            currentPhase = DialoguePhase.Mission;
            currentDialogue = missionDialogue;
            index = 0;
            ShowLine();
            return;
        }

        // 🎯 BẮT ĐẦU QUEST
        if (currentPhase == DialoguePhase.Mission)
        {
            questState = QuestState.InProgress;
            DialogueUI.Instance.Hide();
            Debug.Log("QUEST START: Repair Sacred Hammer");
            return;
        }

        // ✅ QUEST COMPLETE
        if (currentPhase == DialoguePhase.Complete)
        {
            DialogueUI.Instance.Hide();
            Debug.Log("QUEST COMPLETE");
        }
    }

    void ShowLine()
    {
        DialogueLine line = currentDialogue[index];
        bool isLast = index == currentDialogue.Length - 1;

        DialogueUI.Instance.Show(
            npcName,
            line.text,
            !isLast
        );

      
    }

    // Gọi khi mini-game sửa búa xong
    public void OnHammerRepaired()
    {
        questState = QuestState.Completed;
    }
}
