using UnityEngine;

public class RefugeeNPC : MonoBehaviour
{
    public string npcName = "Người Chạy Nạn";
    public DialogueLine[] refugeeDialogue;

    bool spoken;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void StartRefugeeDialogue(System.Action onFinish)
    {
        if (spoken) return;

        spoken = true;
        DialogueUI.Instance.Show(npcName, refugeeDialogue, onFinish);
    }
}
