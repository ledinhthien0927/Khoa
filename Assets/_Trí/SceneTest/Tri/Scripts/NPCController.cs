using UnityEngine;

public abstract class NPCController : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "NPC";

    [Header("Interact")]
    public float interactDistance = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Hint")]
    public GameObject interactHint;

    [Header("Animation")]
    public DialogueActor actor;

    protected Transform player;

    protected bool isInteracting;

    protected virtual void Start()
    {
        GameObject p =
            GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;

        if (interactHint != null)
            interactHint.SetActive(false);
    }

    protected virtual void Update()
    {
        if (player == null) return;

        if (DialogueUI.Instance != null &&
            DialogueUI.Instance.IsShowing)
            return;

        float dist = Vector3.Distance(
            transform.position,
            player.position);

        bool canTalk = dist <= interactDistance;

        if (interactHint != null)
            interactHint.SetActive(canTalk && !isInteracting);

        if (canTalk &&
            !isInteracting &&
            Input.GetKeyDown(interactKey))
        {
            StartInteract();
        }
    }

    protected virtual void StartInteract()
    {
        isInteracting = true;

        LookAtPlayer();

        if (actor != null)
            actor.Play("Talk");

        Interact();
    }

    protected virtual void EndInteract()
    {
        isInteracting = false;

        if (actor != null)
            actor.Play("Idle");
    }

    protected virtual void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dir =
            player.position - transform.position;

        dir.y = 0;

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation =
                Quaternion.LookRotation(dir);
    }

    // ⭐ QUAN TRỌNG
    // DialogueUI gọi khi đóng
    public virtual void OnDialogueFinished()
    {
        EndInteract();
    }

    public abstract void Interact();
}
