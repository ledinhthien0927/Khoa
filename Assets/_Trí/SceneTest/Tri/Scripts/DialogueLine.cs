using UnityEngine;

[System.Serializable] // ⭐ BẮT BUỘC
public class DialogueLine
{
    [TextArea(2, 6)]
    public string text;

    public DialogueEvent dialogueEvent = DialogueEvent.None;

    [HideInInspector]
    public bool eventPlayed;
}
