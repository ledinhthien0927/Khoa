using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class SummonerMonster : MonsterController
{
    [Header("Summon Settings")]
    public GameObject[] summonPrefabs;
    public int maxActiveSummons = 3;
    public float summonCooldown = 6f;
    public float summonCastTime = 1.5f;
    public float postSummonDelay = 0.5f;

    [Header("VFX Settings")]
    [Tooltip("Kéo Particle System gắn ở xương bàn tay vào đây")]
    public GameObject castVFX;

    [Tooltip("Kéo Prefab hình vòng tròn ma thuật vào đây")]
    public GameObject magicCirclePrefab;

    [Tooltip("Kéo Prefab hiệu ứng khói vào đây")]
    public GameObject smokeVFXPrefab;

    [Tooltip("Độ cao của vòng tròn phép thuật so với mặt đất")]
    public float magicCircleHeight = 1f;

    [Tooltip("Độ cao của hiệu ứng khói so với mặt đất")]
    public float smokeHeight = 0.1f;

    [Tooltip("Thời gian tồn tại của vòng tròn phép thuật")]
    public float magicCircleLifetime = 2f;

    [Tooltip("Thời gian tồn tại của hiệu ứng khói")]
    public float smokeLifetime = 2f;

    [Header("Flee Settings")]
    public float safeDistance = 6f;

    private float lastSummonTime = -999f;
    private bool isSummoning = false;

    private List<MonsterController> activeSummons = new List<MonsterController>();
    private Transform lockedTarget;

    protected override void Update()
    {
        if (targetPlayer != null)
        {
            lockedTarget = targetPlayer;
        }

        base.Update();

        if (isDead) return;

        if (lockedTarget != null)
        {
            float dist = Vector3.Distance(transform.position, lockedTarget.position);

            if (dist < 35f && (isReturning || isSearching || targetPlayer == null))
            {
                isReturning = false;
                isSearching = false;
                isAlerted = true;
                targetPlayer = lockedTarget;
                lastKnownPosition = lockedTarget.position;
            }
            else if (dist >= 35f)
            {
                lockedTarget = null;
            }
        }
    }

    public override void OnCombatBehavior(Transform player)
    {
        if (isHit || isDead || isSearching || isReturning)
        {
            StopSummoning();
            return;
        }

        if (isSummoning) return;

        activeSummons.RemoveAll(m => m == null || m.isDead);

        float distance = Vector3.Distance(transform.position, player.position);

        bool canSummonNow =
            Time.time >= lastSummonTime + summonCooldown &&
            activeSummons.Count < maxActiveSummons;

        // Ưu tiên triệu hồi:
        // Chỉ cần player đang trong attackRange là đứng cast luôn.
        // Chỉ lùi khi player áp quá sát.
        if (canSummonNow)
        {
            if (distance < safeDistance)
            {
                Vector3 dir = (transform.position - player.position).normalized;
                MoveToPosition(transform.position + dir * 3f, true);
                return;
            }

            if (distance <= data.attackRange)
            {
                StopMoving();
                RotateTowards(player.position);
                StartCoroutine(SummonRoutine());
                return;
            }

            MoveToPosition(player.position, true);
            return;
        }

        // Khi chưa thể summon thì di chuyển/combat như cũ
        if (distance > data.attackRange)
        {
            MoveToPosition(player.position, true);
        }
        else if (distance < safeDistance)
        {
            Vector3 dir = (transform.position - player.position).normalized;
            MoveToPosition(transform.position + dir * 3f, true);
        }
        else
        {
            StopMoving();
            RotateTowards(player.position);
        }
    }

    IEnumerator SummonRoutine()
    {
        isSummoning = true;
        lastSummonTime = Time.time;

        if (anim != null)
            anim.SetTrigger("summon");

        if (castVFX != null)
            castVFX.SetActive(true);

        yield return new WaitForSeconds(summonCastTime);

        if (isDead || !isSummoning)
        {
            if (castVFX != null)
                castVFX.SetActive(false);

            yield break;
        }

        if (summonPrefabs != null && summonPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, summonPrefabs.Length);
            GameObject prefabToSpawn = summonPrefabs[randomIndex];

            Vector3 spawnOffset = Random.insideUnitSphere * 3f;
            spawnOffset.y = 0f;
            Vector3 spawnPos = transform.position + spawnOffset;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                // Spawn vòng tròn phép thuật
                if (magicCirclePrefab != null)
                {
                    Vector3 circlePos = hit.position + Vector3.up * magicCircleHeight;
                    GameObject circle = Instantiate(magicCirclePrefab, circlePos, Quaternion.Euler(90f, 0f, 0f));
                    Destroy(circle, magicCircleLifetime);
                }

                // Spawn hiệu ứng khói
                if (smokeVFXPrefab != null)
                {
                    Vector3 smokePos = hit.position + Vector3.up * smokeHeight;
                    GameObject smoke = Instantiate(smokeVFXPrefab, smokePos, Quaternion.identity);
                    Destroy(smoke, smokeLifetime);
                }

                // Spawn quái được triệu hồi
                GameObject newSummonObj = Instantiate(prefabToSpawn, hit.position, Quaternion.identity);
                MonsterController newMonster = newSummonObj.GetComponent<MonsterController>();

                if (newMonster != null)
                {
                    newMonster.isAlerted = true;
                    newMonster.isTracking = true;

                    if (lockedTarget != null)
                        newMonster.lastKnownPosition = lockedTarget.position;

                    activeSummons.Add(newMonster);
                    Debug.Log($"<color=magenta>[SPAWNED]</color> {gameObject.name} đã triệu hồi {newMonster.gameObject.name}!");
                }
            }
        }

        yield return new WaitForSeconds(postSummonDelay);

        isSummoning = false;

        if (castVFX != null)
            castVFX.SetActive(false);
    }

    private void StopSummoning()
    {
        if (isSummoning)
        {
            isSummoning = false;
            lastSummonTime = -999f;

            if (castVFX != null)
                castVFX.SetActive(false);
        }
    }

    public override HitResult TakeDamage(DamageInfo info)
    {
        StopSummoning();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        return base.TakeDamage(info);
    }
}