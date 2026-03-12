using UnityEngine;
using System.Collections;

public class FreezeController : MonoBehaviour
{
    private PlayerController playerController;
    private Coroutine freezeRoutine;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }
    }

    public void Freeze(float duration)
    {
        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
        }

        freezeRoutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        if (playerController == null)
            yield break;

        playerController.enabled = false;

        yield return new WaitForSeconds(duration);

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        freezeRoutine = null;
    }
}