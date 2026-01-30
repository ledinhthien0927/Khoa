using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("References")]
    public NPCController npc;
    public GameObject hint;

    bool inRange;

    void Start()
    {
        if (hint != null)
            hint.SetActive(false);
    }

    void Update()
    {
        // Không cho bấm khi đang thoại
        if (!inRange) return;

        if (DialogueUI.Instance != null &&
            DialogueUI.Instance.IsShowing)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Ẩn hint khi bắt đầu nói
            if (hint != null)
                hint.SetActive(false);

            npc.Interact();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        inRange = true;

        // Chỉ hiện khi KHÔNG đang thoại
        if (DialogueUI.Instance == null ||
            !DialogueUI.Instance.IsShowing)
        {
            if (hint != null)
                hint.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        inRange = false;

        if (hint != null)
            hint.SetActive(false);
    }

    // Gọi khi đóng thoại
    public void ShowHintAgain()
    {
        if (inRange && hint != null)
            hint.SetActive(true);
    }
}
