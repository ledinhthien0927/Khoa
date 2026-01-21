using UnityEngine;

public class NPCController : MonoBehaviour
{
    public static NPCController Current;

    [Header("NPC Info")]
    public string npcName = "Hoàng Tử";
    public Transform npcFace;
    public Transform malricIsland;
    public Transform brokenHammer;

    [Header("Dialogue")]
    public DialogueLine[] introDialogue;
    public DialogueLine[] missionDialogue;
    public DialogueLine[] idleDialogue;
    public DialogueLine[] hammerCompleteDialogue;
    public DialogueLine[] shipMissionDialogue;
    
    


    public enum QuestPhase
    {
        Intro,
        HammerMission,
        HammerInProgress,
        HammerCompleted,
        ShipMission
    }

    QuestPhase phase = QuestPhase.Intro;
    bool isTalking;
    int lastIdle = -1;

    public void Interact()
    {
        Current = this;
        DialogueCamera.Instance.Focus(npcFace);
        switch (phase)
        {
            case QuestPhase.Intro: PlayIntro(); break;
            case QuestPhase.HammerMission: PlayMission(); break;
            case QuestPhase.HammerInProgress: PlayIdle(); break;
            case QuestPhase.HammerCompleted: PlayHammerComplete(); break;
            case QuestPhase.ShipMission: PlayShipMission(); break;
        }
    }

    void PlayIntro()
    {
        isTalking = true;
        DialogueUI.Instance.Show(npcName, introDialogue, () =>
        {
            phase = QuestPhase.HammerMission;
            isTalking = false;
        });
    }

    void PlayMission()
    {
        isTalking = true;
        DialogueUI.Instance.Show(npcName, missionDialogue, () =>
        {
            PlayerQuestManager.Instance.AcceptQuest(QuestID.HammerQuest);
            phase = QuestPhase.HammerInProgress;
            isTalking = false;
        });
    }

    void PlayIdle()
    {
        int r;
        do
        {
            r = Random.Range(0, idleDialogue.Length);
        }
        while (idleDialogue.Length > 1 && r == lastIdle);

        lastIdle = r;
        DialogueUI.Instance.ShowSingle(npcName, idleDialogue[r]);
    }

    public void OnHammerRepaired()
    {
        phase = QuestPhase.HammerCompleted;
    }

    void PlayHammerComplete()
    {
        isTalking = true;
        DialogueUI.Instance.Show(npcName, hammerCompleteDialogue, () =>
        {
            PlayerQuestManager.Instance.CompleteQuest(QuestID.HammerQuest);
            phase = QuestPhase.ShipMission;
            isTalking = false;
        });
    }

    void PlayShipMission()
    {
        isTalking = true;
        DialogueUI.Instance.Show(npcName, shipMissionDialogue, () =>
        {
            PlayerQuestManager.Instance.AcceptQuest(QuestID.ShipQuest);
            isTalking = false;
        });
    }
}
