using UnityEngine;

/// <summary>
/// Base class cho tất cả NPC
/// </summary>
public abstract class NPCController : MonoBehaviour
{
    // ================= INFO =================

    [Header("NPC Info")]
    public string npcName = "NPC";

    // ================= INTERACT =================

    [Header("Interact")]
    public float interactDistance = 2f;
    public KeyCode interactKey = KeyCode.E;

    // ================= UI =================

    [Header("UI Hint")]
    public GameObject interactHint;

    // ================= ANIMATION =================

    [Header("Animation")]
    public DialogueActor actor;

    protected Animator anim;

    // ================= RUNTIME =================

    protected Transform player;

    protected bool isInteracting;
    protected bool canInteract = false;

    // =====================================================
    // START
    // =====================================================

    protected virtual void Start()
    {
        // Tìm player
        GameObject p =
            GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;

        anim = GetComponent<Animator>();

        if (interactHint != null)
            interactHint.SetActive(false);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    protected virtual void Update()
    {
        if (!canInteract) return;
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

    // =====================================================
    // INTERACT
    // =====================================================

    protected virtual void StartInteract()
    {
        isInteracting = true;

        LookAtPlayer();

        PlayTalk();

        OnStartInteract();

        Interact();
    }

    protected virtual void EndInteract()
    {
        isInteracting = false;

        PlayIdle();

        OnEndInteract();
    }

    // =====================================================
    // OVERRIDE
    // =====================================================

    public abstract void Interact();

    protected virtual void OnStartInteract() { }

    protected virtual void OnEndInteract() { }

    // =====================================================
    // ANIMATION
    // =====================================================

    protected void PlayTalk()
    {
        if (actor != null)
            actor.Play("Talk");

        if (anim != null)
            anim.SetBool("IsTalking", true);
    }

    protected void PlayIdle()
    {
        if (actor != null)
            actor.Play("Idle");

        if (anim != null)
            anim.SetBool("IsTalking", false);
    }

    // =====================================================
    // LOOK AT PLAYER
    // =====================================================

    protected void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dir =
            player.position - transform.position;

        dir.y = 0;

        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation =
                Quaternion.LookRotation(dir);
        }
    }

    // =====================================================
    // DIALOG CALLBACK
    // =====================================================

    public virtual void OnDialogueFinished()
    {
        EndInteract();
    }

    // =====================================================
    // LOCK / UNLOCK
    // =====================================================

    public void Lock()
    {
        canInteract = false;

        if (interactHint != null)
            interactHint.SetActive(false);
    }

    public void Unlock()
    {
        canInteract = true;
    }
}
