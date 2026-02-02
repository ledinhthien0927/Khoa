using UnityEngine;

public class PrinceNPC : NPCController
{
    [Header("1. Gặp Gỡ & Giao Sửa Kiếm")]
    public DialogueLine[] introDialogue;       
    public DialogueLine[] remindSwordDialogue; 

    [Header("2. Trả Nhiệm Vụ Kiếm & Giao Sửa Thuyền")]
    public DialogueLine[] finishSwordDialogue; 
    public DialogueLine[] remindShipDialogue;  

    [Header("3. Trả Nhiệm Vụ Thuyền & Giao Mảnh Vỡ")]
    public DialogueLine[] finishShipDialogue;      
    public DialogueLine[] remindFragmentsDialogue; 

    public override void Interact()
    {
        if (DialogueUI.Instance == null || QuestManager.Instance == null) return;

        var qm = QuestManager.Instance;
        var ui = DialogueUI.Instance;

        switch (qm.princeState)
        {
            // ============ GIAI ĐOẠN 1: GIAO SỬA KIẾM ============
            case PrinceQuestState.None:
                ui.Show(introDialogue, this, () => 
                {
                    // 1. Cập nhật trạng thái
                    qm.SetPrince(PrinceQuestState.IntroDone);
                    
                    // 2. [QUAN TRỌNG] Tự động bật dẫn đường đến LÒ RÈN
                    if(QuestUIManager.Instance) QuestUIManager.Instance.AutoClickMainQuest();
                });
                break;

            case PrinceQuestState.IntroDone:
            case PrinceQuestState.Accepted: 
                ui.Show(remindSwordDialogue, this);
                break;

            // ============ GIAI ĐOẠN 2: TRẢ KIẾM - GIAO THUYỀN ============
            case PrinceQuestState.Completed:
                DialogueUI.Instance.Show(finishSwordDialogue, this, () => 
                {
                    // Bước 1: Phải chuyển trạng thái sang ShipQuest TRƯỚC
                    // (Lúc này UI sẽ đổi chữ thành "Sửa thuyền" và mục tiêu thành Chiếc Thuyền)
                    qm.SetPrince(PrinceQuestState.ShipQuest);

                    // Bước 2: Sau đó mới gọi dẫn đường
                    // (Lúc này nó sẽ click vào nút Sửa thuyền)
                    if(QuestUIManager.Instance) QuestUIManager.Instance.AutoClickMainQuest();
                });
                break;

            case PrinceQuestState.ShipQuest:
            case PrinceQuestState.ShipDoing:
                ui.Show(remindShipDialogue, this);
                break;

            // ============ GIAI ĐOẠN 3: TRẢ THUYỀN - GIAO MẢNH VỠ ============
            case PrinceQuestState.ShipDone:
                ui.Show(finishShipDialogue, this, () => 
                {
                    // 1. Cập nhật trạng thái sang tìm mảnh vỡ
                    qm.SetPrince(PrinceQuestState.ShipDoneForever);

                    // 2. [QUAN TRỌNG] Tự động bật dẫn đường đến VÙNG MẢNH VỠ
                    if(QuestUIManager.Instance) QuestUIManager.Instance.AutoClickMainQuest();
                });
                break;

            case PrinceQuestState.ShipDoneForever:
                ui.Show(remindFragmentsDialogue, this);
                break;
        }
    }
}