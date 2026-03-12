using UnityEngine;
using System.Collections;

public class BurnController : MonoBehaviour
{
    private Coroutine burnRoutine;

    public void ApplyBurn(float duration, float damagePerTick, float tickInterval)
    {
        if (burnRoutine != null)
        {
            StopCoroutine(burnRoutine);
        }

        burnRoutine = StartCoroutine(BurnRoutine(duration, damagePerTick, tickInterval));
    }

    private IEnumerator BurnRoutine(float duration, float damagePerTick, float tickInterval)
    {
        float timer = 0f;

        PlayerController player = GetComponent<PlayerController>();
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }

        while (timer < duration)
        {
            if (player != null)
            {
                DamageInfo dmg = new DamageInfo();
                dmg.amount = damagePerTick;
                dmg.hitPoint = player.transform.position;

                player.TakeDamage(dmg);
            }

            yield return new WaitForSeconds(tickInterval);
            timer += tickInterval;
        }

        burnRoutine = null;
    }
}