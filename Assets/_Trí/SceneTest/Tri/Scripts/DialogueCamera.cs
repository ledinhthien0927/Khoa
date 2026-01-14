using UnityEngine;
using System.Collections;

public class DialogueCamera : MonoBehaviour
{
    public static DialogueCamera Instance;

    public float zoomFOV = 35f;
    public float moveSpeed = 6f;
    public float rotateSpeed = 8f;

    Camera cam;
    Vector3 startPos;
    Quaternion startRot;
    float startFOV;

    Coroutine camRoutine;
    CameraFollow follow;

    void Awake()
    {
        Instance = this;

        cam = Camera.main;
        follow = cam.GetComponent<CameraFollow>();

        startPos = cam.transform.position;
        startRot = cam.transform.rotation;
        startFOV = cam.fieldOfView;
    }

    public void FocusOn(Transform focusPoint)
    {
        if (follow) follow.enabled = false;

        if (camRoutine != null)
            StopCoroutine(camRoutine);

        camRoutine = StartCoroutine(FocusRoutine(focusPoint));
    }

    public void ResetCamera()
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);

        camRoutine = StartCoroutine(ResetRoutine());
    }

    IEnumerator FocusRoutine(Transform target)
    {
        while (true)
        {
            // 1️⃣ Move camera
            cam.transform.position = Vector3.Lerp(
                cam.transform.position,
                target.position,
                Time.unscaledDeltaTime * moveSpeed
            );

            // 2️⃣ Rotate camera to LOOK AT NPC
            Vector3 lookDir = (target.position - cam.transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(lookDir);

            cam.transform.rotation = Quaternion.Lerp(
                cam.transform.rotation,
                lookRot,
                Time.unscaledDeltaTime * rotateSpeed
            );

            // 3️⃣ Zoom
            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                zoomFOV,
                Time.unscaledDeltaTime * moveSpeed
            );

            yield return null;
            Debug.DrawLine(
                cam.transform.position,
                target.position,
            Color.red
            );

        }
    }

    IEnumerator ResetRoutine()
    {
        while (true)
        {
            cam.transform.position = Vector3.Lerp(
                cam.transform.position,
                startPos,
                Time.unscaledDeltaTime * moveSpeed
            );

            cam.transform.rotation = Quaternion.Lerp(
                cam.transform.rotation,
                startRot,
                Time.unscaledDeltaTime * rotateSpeed
            );

            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                startFOV,
                Time.unscaledDeltaTime * moveSpeed
            );

            yield return null;
        }
    }
}
