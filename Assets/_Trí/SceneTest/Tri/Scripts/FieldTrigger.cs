using System.Collections;
using UnityEngine;

public class FieldTrigger : MonoBehaviour
{
    public FarmerNPC farmerNPC;
    public Transform farmerFocus;     // camera focus (optional)
   public DialogueActor farmerActor;

    bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // Farmer nổi giận
        farmerNPC.TriggerAngry();

        // Tạo thoại
        DialogueLine[] lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = "Nông dân",
                text = "Này! Dẫm lên hết rau của tôi rồi!",
                focusTarget = farmerFocus,
               actor = farmerActor,
                animationTrigger = "Angry",
                showAccept = false
            }
        };

        // Show dialogue
        DialogueUI.Instance.Show(
            lines,
            farmerNPC   // cực kỳ quan trọng → để gọi OnDialogueFinished()
        );
        StartCoroutine(ResetTrigger());

        IEnumerator ResetTrigger()
        {
            yield return new WaitForSeconds(10f);
            triggered = false;
        }
    }
}