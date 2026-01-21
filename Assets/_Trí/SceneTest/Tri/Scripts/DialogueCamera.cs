using UnityEngine;
using System.Collections;

public class DialogueCamera : MonoBehaviour
{
    public static DialogueCamera Instance;

    Camera cam;
    Vector3 startPos;
    Quaternion startRot;
    float startFOV;

    Coroutine routine;

    void Awake()
    {
        Instance = this;

        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("DialogueCamera: No Main Camera found!");
            return;
        }

        startPos = cam.transform.position;
        startRot = cam.transform.rotation;
        startFOV = cam.fieldOfView;
    }

    void StopRoutine()
    {
        if (routine != null)
            StopCoroutine(routine);
    }

    public void Focus(Transform target)
    {
        StopRoutine();
        routine = StartCoroutine(FocusRoutine(target));
    }

    public void PanTo(Transform target)
    {
        StopRoutine();
        routine = StartCoroutine(PanRoutine(target));
    }

    public void FocusOverShoulder(Transform npc)
    {
        StopRoutine();
        routine = StartCoroutine(OverShoulderRoutine(npc));
    }

    public void ResetCam()
    {
        StopRoutine();
        routine = StartCoroutine(ResetRoutine());
    }

   IEnumerator FocusRoutine(Transform target)
{
    if (target == null) yield break;

    float t = 0;
    while (t < 1)
    {
        t += Time.unscaledDeltaTime * 2f;

        Vector3 dir = target.position - cam.transform.position;

        if (dir.sqrMagnitude > 0.001f)
        {
            cam.transform.rotation = Quaternion.Lerp(
                cam.transform.rotation,
                Quaternion.LookRotation(dir),
                t
            );
        }

        cam.transform.position = Vector3.Lerp(
            cam.transform.position,
            target.position,
            t
        );

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 35, t);
        yield return null;
    }
}


    IEnumerator PanRoutine(Transform target)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 1.5f;
            cam.transform.rotation = Quaternion.Lerp(
                cam.transform.rotation,
                Quaternion.LookRotation(target.position - cam.transform.position),
                t
            );
            yield return null;
        }
    }

    IEnumerator OverShoulderRoutine(Transform npc)
    {
        Vector3 targetPos =
            npc.position - npc.forward * 3.5f + Vector3.up * 1.6f;

        Quaternion targetRot =
            Quaternion.LookRotation(npc.position + Vector3.up * 1.5f - targetPos);

        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 2f;
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, t);
            cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, targetRot, t);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 45, t);
            yield return null;
        }
    }

    IEnumerator ResetRoutine()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 2f;
            cam.transform.position = Vector3.Lerp(cam.transform.position, startPos, t);
            cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, startRot, t);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, startFOV, t);
            yield return null;
        }
    }
}
