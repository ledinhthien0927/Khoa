using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea(2, 6)]
    public string text;

    [Header("Camera")]
    public Transform focusTarget;

    [Header("UI")]
    public bool showAccept;

    [Header("Animation")]
    public DialogueActor actor;
    public string animationTrigger;
}
