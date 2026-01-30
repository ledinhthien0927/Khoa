using UnityEngine;
using System.Collections;

public class DialogueCamera : MonoBehaviour
{
    public static DialogueCamera Instance;

    [Header("Camera")]
    public Camera mainCam;
    public Camera dialogueCam;

    [Header("Move")]
    public float moveSpeed = 6f;
    public float focusDuration = 0.7f; // thời gian zoom vào

    [Header("Zoom")]
    public float normalFOV = 60f;
    public float closeFOV = 35f;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 1.6f, 2f);

    Transform target;
    Transform currentTarget; // ⭐ lưu NPC đang focus

    bool isFocusing = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        dialogueCam.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!target || isFocusing) return;

        Vector3 pos =
            target.position
            - target.forward * offset.z
            + Vector3.up * offset.y;

        dialogueCam.transform.position =
            Vector3.Lerp(
                dialogueCam.transform.position,
                pos,
                moveSpeed * Time.deltaTime
            );

        dialogueCam.transform.LookAt(
            target.position + Vector3.up * 1.5f
        );
    }

    // =========================
    // Focus NPC (SMOOTH)
    // =========================
   public void Focus(Transform npc)
{
    if (!npc) return;

    // Nếu vẫn là NPC cũ → không zoom lại
    if (currentTarget == npc)
    {
        target = npc;
        return;
    }

    // NPC mới → zoom
    currentTarget = npc;
    target = npc;

    mainCam.gameObject.SetActive(false);
    dialogueCam.gameObject.SetActive(true);

    StopAllCoroutines();
    StartCoroutine(FocusRoutine());
}

    IEnumerator FocusRoutine()
    {
        isFocusing = true;

        Vector3 startPos = dialogueCam.transform.position;
        Quaternion startRot = dialogueCam.transform.rotation;
        float startFOV = normalFOV;

        Vector3 targetPos =
            target.position
            - target.forward * offset.z
            + Vector3.up * offset.y;

        Quaternion targetRot =
            Quaternion.LookRotation(
                (target.position + Vector3.up * 1.5f) - targetPos
            );

        float timer = 0;

        dialogueCam.fieldOfView = normalFOV;

        while (timer < focusDuration)
        {
            float t = timer / focusDuration;

            dialogueCam.transform.position =
                Vector3.Lerp(startPos, targetPos, t);

            dialogueCam.transform.rotation =
                Quaternion.Slerp(startRot, targetRot, t);

            dialogueCam.fieldOfView =
                Mathf.Lerp(normalFOV, closeFOV, t);

            timer += Time.deltaTime;

            yield return null;
        }

        // Fix cuối
        dialogueCam.transform.position = targetPos;
        dialogueCam.transform.rotation = targetRot;
        dialogueCam.fieldOfView = closeFOV;

        isFocusing = false;
    }

    // =========================
    // Reset
    // =========================
    public void ResetCam()
    {
        StopAllCoroutines();

        target = null;
        currentTarget = null; // ⭐ reset NPC

        dialogueCam.gameObject.SetActive(false);
        mainCam.gameObject.SetActive(true);

        dialogueCam.fieldOfView = normalFOV;
    }

}
