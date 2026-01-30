using UnityEngine;

public class VillageChiefNPC : NPCController
{
    [Header("Dialogues")]
    public DialogueLine[] intro;
    public DialogueLine[] quest;
    public DialogueLine[] warning;
    public DialogueLine[] cleared;
    public DialogueLine[] reward;
    public DialogueLine[] done;

    public override void Interact()
    {
        if (DialogueUI.Instance == null) return;
        if (QuestManager.Instance == null) return;

        var qm = QuestManager.Instance;

        switch (qm.villageState)
        {
            // ============ FIRST ============
            case VillageQuestState.None:

                DialogueUI.Instance.Show(
                    intro,
                    this,
                    () =>
                    {
                        qm.SetVillage(
                            VillageQuestState.Accepted);
                    });

                break;

            // ============ ACCEPT ============
            case VillageQuestState.Accepted:

                DialogueUI.Instance.Show(
                    quest,
                    this,
                    null,
                    AcceptQuest);

                break;

            // ============ WORKING ============
            case VillageQuestState.Working:

                DialogueUI.Instance.Show(
                    warning,
                    this);

                break;

            // ============ CLEARED ============
            case VillageQuestState.Cleared:

                DialogueUI.Instance.Show(
                    cleared,
                    this,
                    () =>
                    {
                        qm.SetVillage(
                            VillageQuestState.Rewarded);
                    });

                break;

            // ============ REWARD ============
            case VillageQuestState.Rewarded:

                DialogueUI.Instance.Show(
                    reward,
                    this,
                    () =>
                    {
                        qm.SetVillage(
                            VillageQuestState.Done);
                    });

                break;

            // ============ DONE ============
            case VillageQuestState.Done:

                DialogueUI.Instance.Show(
                    done,
                    this);

                break;

            default:

                Debug.LogWarning(
                    "VillageChief: Unknown state "
                    + qm.villageState);

                break;
        }
    }

    void AcceptQuest()
    {
        QuestManager.Instance
            .SetVillage(VillageQuestState.Working);
    }
}
