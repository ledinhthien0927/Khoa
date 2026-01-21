using UnityEngine;

public class VillageInteractprompt : MonoBehaviour
{
    [Header("References")]
    public GameObject talkPrompt;
    public VillageChiefNPC npcDialogue;

    [Header("Effect")]
    public float blinkSpeed = 4f;
    public float blinkScale = 0.05f;

    bool playerInRange;
    Vector3 originalScale;
    float blinkTime;

    void Start()
    {
        talkPrompt.SetActive(false);
        originalScale = talkPrompt.transform.localScale;
    }

    void Update()
    {
        // 🔴 Nếu đang mở hộp thoại → ẩn prompt
        if (DialogueUI.Instance != null &&
            DialogueUI.Instance.panel.activeSelf)
        {
            talkPrompt.SetActive(false);
            return;
        }

        // 🟢 Player trong vùng → cho phép nhấp nháy + bấm E
        if (playerInRange)
        {
            BlinkEffect();

            if (Input.GetKeyDown(KeyCode.E))
            {
                talkPrompt.SetActive(false);
                npcDialogue.Interact();
            }
        }
    }

    void BlinkEffect()
    {
        blinkTime += Time.deltaTime * blinkSpeed;
        float scaleOffset = Mathf.Sin(blinkTime) * blinkScale;
        talkPrompt.transform.localScale = originalScale * (1f + scaleOffset);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            blinkTime = 0f;
            talkPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            talkPrompt.SetActive(false);
            talkPrompt.transform.localScale = originalScale;
        }
    }
}
