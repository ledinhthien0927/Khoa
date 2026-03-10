using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SummonerMonster : MonsterController
{
    [Header("Summon Settings")]
    public GameObject[] summonPrefabs; 
    public int maxActiveSummons = 3;     
    public float summonCooldown = 6f;    
    public float summonCastTime = 1.5f;  
    public float postSummonDelay = 0.5f; 
    
    // --- [ĐÃ THÊM] KHAI BÁO VFX ---
    [Header("VFX Settings")]
    [Tooltip("Kéo Particle System gắn ở xương bàn tay vào đây")]
    public GameObject castVFX; 
    [Tooltip("Kéo Prefab hình vòng tròn ma thuật vào đây")]
    public GameObject magicCirclePrefab; 

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
        if (isHit || isDead || isSearching || isReturning) { StopSummoning(); return; }

        if (isSummoning) return;

        activeSummons.RemoveAll(m => m == null || m.isDead);

        float distance = Vector3.Distance(transform.position, player.position);

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
            
            if (Time.time >= lastSummonTime + summonCooldown && activeSummons.Count < maxActiveSummons)
            {
                StartCoroutine(SummonRoutine());
            }
        }
    }

    IEnumerator SummonRoutine()
    {
        isSummoning = true; 
        lastSummonTime = Time.time;

        if (anim != null) anim.SetTrigger("summon"); 

        // --- [ĐÃ THÊM] BẬT SÁNG BÀN TAY ---
        if (castVFX != null) castVFX.SetActive(true);

        yield return new WaitForSeconds(summonCastTime);

        if (isDead || !isSummoning) yield break;

        if (summonPrefabs != null && summonPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, summonPrefabs.Length);
            GameObject prefabToSpawn = summonPrefabs[randomIndex];

            Vector3 spawnOffset = Random.insideUnitSphere * 3f;
            spawnOffset.y = 0; 
            Vector3 spawnPos = transform.position + spawnOffset;

            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 4f, UnityEngine.AI.NavMesh.AllAreas))
            {
                // --- [ĐÃ THÊM] GỌI VÒNG TRÒN MA THUẬT ---
                if (magicCirclePrefab != null)
                {
                    // Nâng lên một chút để không bị lún xuống mặt đất
                    Vector3 circlePos = hit.position + Vector3.up * 0.05f; 
                    GameObject circle = Instantiate(magicCirclePrefab, circlePos, Quaternion.Euler(90f, 0f, 0f));
                    Destroy(circle, 2f); // Tự động xóa vòng tròn sau 2 giây
                }

                GameObject newSummonObj = Instantiate(prefabToSpawn, hit.position, Quaternion.identity);
                MonsterController newMonster = newSummonObj.GetComponent<MonsterController>();

                if (newMonster != null)
                {
                    newMonster.isAlerted = true;
                    newMonster.isTracking = true;
                    if (lockedTarget != null) newMonster.lastKnownPosition = lockedTarget.position;
                    
                    activeSummons.Add(newMonster);
                    Debug.Log($"<color=magenta>[SPAWNED]</color> {gameObject.name} đã triệu hồi {newMonster.gameObject.name}!");
                }
            }
        }

        yield return new WaitForSeconds(postSummonDelay);

        isSummoning = false; 
        // --- [ĐÃ THÊM] TẮT SÁNG BÀN TAY ---
        if (castVFX != null) castVFX.SetActive(false);
    }

    private void StopSummoning()
    {
        if (isSummoning)
        {
            isSummoning = false;
            lastSummonTime = -999f; 
            
            // --- [ĐÃ THÊM] TẮT HIỆU ỨNG NẾU BỊ ĐÁNH NGẮT CHIÊU ---
            if (castVFX != null) castVFX.SetActive(false);
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