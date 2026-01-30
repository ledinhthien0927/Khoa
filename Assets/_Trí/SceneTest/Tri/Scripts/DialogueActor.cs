using UnityEngine;

public class DialogueActor : MonoBehaviour
{
    Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Play(string trigger)
    {
        if (anim == null) return;
        if (string.IsNullOrEmpty(trigger)) return;

        anim.ResetTrigger(trigger);
        anim.SetTrigger(trigger);
    }

    public void SetTalking(bool value)
    {
        if (anim == null) return;
        anim.SetBool("isTalking", value);
    }
}
