using UnityEngine;
using System.Collections;

public class FirePotionProjectile : MonoBehaviour
{
    [Header("Arc Settings")]
    public float travelTime = 0.5f;
    public float arcHeight = 2f;
    public float targetHeightOffset = 1f;
    public bool followMovingTarget = false;

    [Header("Burn Settings")]
    public float burnDuration = 3f;
    public float burnDamage = 3f;
    public float burnTickInterval = 1f;

    public float impactRadius = 1.5f;
    public LayerMask playerLayer;

    [Header("VFX")]
    public GameObject hitVFX;

    private Transform target;

    private Vector3 startPos;
    private Vector3 lockedTargetPos;
    private float timer;
    private bool isLaunched;

    public void SetTarget(Transform newTarget, Vector3 spawnPos)
    {
        target = newTarget;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = spawnPos;
        startPos = spawnPos;
        lockedTargetPos = GetTargetPoint(target);
        timer = 0f;
        isLaunched = true;
    }

    private void Update()
    {
        if (!isLaunched) return;

        Vector3 currentTargetPos = followMovingTarget && target != null
            ? GetTargetPoint(target)
            : lockedTargetPos;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / travelTime);

        Vector3 pos = Vector3.Lerp(startPos, currentTargetPos, t);
        pos.y += arcHeight * 4f * t * (1f - t);
        transform.position = pos;

        float nextT = Mathf.Clamp01((timer + 0.02f) / travelTime);
        Vector3 nextPos = Vector3.Lerp(startPos, currentTargetPos, nextT);
        nextPos.y += arcHeight * 4f * nextT * (1f - nextT);

        Vector3 dir = nextPos - transform.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        if (t >= 1f)
        {
            Explode(currentTargetPos);
        }
    }

    private Vector3 GetTargetPoint(Transform t)
    {
        Collider col = t.GetComponentInChildren<Collider>();
        if (col != null)
        {
            return col.bounds.center + Vector3.up * 0.2f;
        }

        return t.position + Vector3.up * targetHeightOffset;
    }

    private void Explode(Vector3 impactPos)
    {
        if (hitVFX != null)
        {
            Instantiate(hitVFX, impactPos, Quaternion.identity);
        }

        Collider[] hits = Physics.OverlapSphere(impactPos, impactRadius, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerController player = hits[i].GetComponent<PlayerController>();
            if (player == null)
            {
                player = hits[i].GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                BurnController burn = player.GetComponent<BurnController>();
                if (burn == null)
                {
                    burn = player.gameObject.AddComponent<BurnController>();
                }

                burn.ApplyBurn(burnDuration, burnDamage, burnTickInterval);
            }
        }

        Destroy(gameObject);
    }
}