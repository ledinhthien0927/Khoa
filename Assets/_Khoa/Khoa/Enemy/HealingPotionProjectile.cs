using UnityEngine;

public class HealingPotionProjectile : MonoBehaviour
{
    [Header("Arc Settings")]
    public float travelTime = 0.5f;
    public float arcHeight = 2f;

    [Tooltip("Offset thêm vào ði?m ðích cu?i cùng.")]
    public float targetHeightOffset = 0.15f;

    [Tooltip("N?u b?t, b?nh s? bám theo target ðang di chuy?n. N?u t?t, b?nh bay t?i ði?m ð? khóa lúc ném.")]
    public bool followMovingTarget = false;

    public GameObject hitVFX;

    private MonsterController target;
    private float healAmount;

    private Vector3 startPos;
    private Vector3 lockedTargetPos;
    private float timer;
    private bool isLaunched;

    public void SetTarget(MonsterController newTarget, float newHealAmount)
    {
        target = newTarget;
        healAmount = newHealAmount;

        if (target == null || target.isDead)
        {
            Destroy(gameObject);
            return;
        }

        startPos = transform.position;
        lockedTargetPos = GetTargetPoint(target);
        timer = 0f;
        isLaunched = true;
    }

    private void Update()
    {
        if (!isLaunched) return;

        if (target == null || target.isDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 currentTargetPos = followMovingTarget ? GetTargetPoint(target) : lockedTargetPos;

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
            ApplyHeal();

            if (hitVFX != null)
            {
                Instantiate(hitVFX, currentTargetPos, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }

    private Vector3 GetTargetPoint(MonsterController monster)
    {
        if (monster == null) return transform.position;

        Collider col = monster.GetComponentInChildren<Collider>();
        if (col != null)
        {
            Vector3 p = col.bounds.center;
            p.y = col.bounds.min.y + targetHeightOffset;
            return p;
        }

        return monster.transform.position + Vector3.up * targetHeightOffset;
    }

    private void ApplyHeal()
    {
        if (target == null || target.isDead || target.data == null) return;

        target.currentHealth += healAmount;

        if (target.currentHealth > target.data.maxHealth)
        {
            target.currentHealth = target.data.maxHealth;
        }

        if (target.healthSlider != null)
        {
            target.healthSlider.value = target.currentHealth;
        }
    }
}