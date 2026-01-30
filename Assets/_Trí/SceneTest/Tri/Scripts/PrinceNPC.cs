using UnityEngine;

public class PrinceNPC : NPCController
{
    [Header("Sword Quest")]
    public DialogueLine[] intro;
    public DialogueLine[] quest;
    public DialogueLine[] doing;
    public DialogueLine[] finish;

    [Header("Ship Quest")]
    public DialogueLine[] shipIntro;
    public DialogueLine[] shipDoing;
    public DialogueLine[] shipFinish;

    public override void Interact()
    {
        if (DialogueUI.Instance == null) return;
        if (QuestManager.Instance == null) return;

        var qm = QuestManager.Instance;

        switch (qm.princeState)
        {
            // ============ FIRST MEET ============
            case PrinceQuestState.None:

                DialogueUI.Instance.Show(
                    intro,
                    this,
                    () =>
                    {
                        qm.SetPrince(PrinceQuestState.IntroDone);
                    });

                break;

            // ============ ACCEPT SWORD QUEST ============
            case PrinceQuestState.IntroDone:

                DialogueUI.Instance.Show(
                    quest,
                    this,
                    () =>
                    {
                        qm.SetPrince(PrinceQuestState.Accepted);
                    });

                break;

            // ============ DOING SWORD ============
            case PrinceQuestState.Accepted:

                DialogueUI.Instance.Show(
                    doing,
                    this);

                break;

            // ============ FINISH SWORD ============
            case PrinceQuestState.Completed:

                DialogueUI.Instance.Show(
                    finish,
                    this,
                    () =>
                    {
                        qm.SetPrince(PrinceQuestState.ShipQuest);
                    });

                break;

            // ============ ACCEPT SHIP ============
            case PrinceQuestState.ShipQuest:

                DialogueUI.Instance.Show(
                    shipIntro,
                    this,
                    () =>
                    {
                        qm.SetPrince(PrinceQuestState.ShipDoing);
                    });

                break;

            // ============ DOING SHIP ============
            case PrinceQuestState.ShipDoing:

                DialogueUI.Instance.Show(
                    shipDoing,
                    this);

                break;

            // ============ FINISH SHIP ============
            case PrinceQuestState.ShipDone:

                DialogueUI.Instance.Show(
                    shipFinish,
                    this,
                    () =>
                    {
                        qm.SetPrince(
                            PrinceQuestState.ShipDoneForever);
                    });

                break;

            // ============ END ============
            case PrinceQuestState.ShipDoneForever:

                Debug.Log("Prince Quest Finished");

                break;

            default:

                Debug.LogWarning(
                    "PrinceNPC: Unknown state "
                    + qm.princeState);

                break;
        }
    }
}
