using UnityEngine;

public enum VillageScenePhase
{
    Scene1_NotStarted,
    Scene1_Accepted,
    Scene2_Returned,
    Scene3_Reward
}

public class VillageChiefNPC : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "Trưởng Làng";

    [Header("Scene 1 – Nhận nhiệm vụ")]
    public DialogueLine[] scene1Dialogue;
    public DialogueLine warningDialogue;

    [Header("Scene 2 – Chiến thắng + Tin dữ")]
    public DialogueLine[] scene2ThanksDialogue;
    public DialogueLine malricDialogue;
    public RefugeeNPC refugeeNPC;

    [Header("Scene 3 – Báu vật cổ xưa")]
    public DialogueLine[] scene3Dialogue;
    public DialogueLine finalBlessing;
    public AncientChest chest;

    public VillageScenePhase phase = VillageScenePhase.Scene1_NotStarted;
    bool isTalking;

    // =========================
    // INTERACT
    // =========================
    public void Interact()
    {
        if (isTalking) return;

        switch (phase)
        {
            case VillageScenePhase.Scene1_NotStarted:
                StartScene1();
                break;

            case VillageScenePhase.Scene1_Accepted:
                DialogueUI.Instance.ShowSingle(npcName, warningDialogue);
                break;

            case VillageScenePhase.Scene2_Returned:
                StartScene2();
                break;

            case VillageScenePhase.Scene3_Reward:
                StartScene3();
                break;
        }
    }

    // =========================
    // SCENE 1
    // =========================
  void StartScene1()
{
    DialogueUI.Instance.Show(npcName, scene1Dialogue, () =>
    {
        DialogueUI.Instance.ShowAcceptButton(() =>
        {
            PlayerQuestManager.Instance.AcceptQuest(QuestID.VillageQuest);

            phase = VillageScenePhase.Scene1_Accepted;

            DialogueUI.Instance.ShowSingle(
                npcName,
                warningDialogue
            );
        });
    });
}





    // =========================
    // SCENE 2
    // =========================
    public void OnVillagePurified()
    {
        phase = VillageScenePhase.Scene2_Returned;
    }

    void StartScene2()
    {
        isTalking = true;

        DialogueUI.Instance.Show(npcName, scene2ThanksDialogue, () =>
        {
            refugeeNPC.gameObject.SetActive(true);

            refugeeNPC.StartRefugeeDialogue(() =>
            {
                DialogueUI.Instance.ShowSingle(npcName, malricDialogue);
                phase = VillageScenePhase.Scene3_Reward;
                isTalking = false;
            });
        });
    }

    // =========================
    // SCENE 3
    // =========================
    void StartScene3()
    {
        isTalking = true;

        DialogueUI.Instance.Show(npcName, scene3Dialogue, () =>
        {
            chest.EnableChest();
            isTalking = false;
        });
    }

    public void OnChestOpened()
    {
        DialogueUI.Instance.ShowSingle(npcName, finalBlessing);
    }
}
