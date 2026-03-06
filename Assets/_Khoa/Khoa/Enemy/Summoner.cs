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
    
    [Header("Flee Settings")]
    public float safeDistance = 6f;      

    private float lastSummonTime = -999f;
    private bool isSummoning = false; 
    
    private List<MonsterController> activeSummons = new List<MonsterController>();
    
    // [ĐÃ THÊM] Biến khóa não: Lưu trữ kẻ thù bất chấp lớp cha có xóa hay không
    private Transform lockedTarget; 

    // --- SỬA LỖI TẬN GỐC: ÉP KHÔNG CHO QUAY VỀ ---
    protected override void Update()
    {
        // 1. Lưu lại kẻ thù vào bộ nhớ riêng TRƯỚC KHI lớp cha xử lý
        if (targetPlayer != null) 
        {
            lockedTarget = targetPlayer;
        }

        // 2. Cho lớp cha chạy (Lớp cha có thể sẽ ngớ ngẩn đòi quay về nhà ở bước này)
        base.Update();

        if (isDead) return;

        // 3. KIỂM DUYỆT LẠI: Chống lệnh lớp cha
        if (lockedTarget != null)
        {
            float dist = Vector3.Distance(transform.position, lockedTarget.position);

            // Nếu bạn vẫn ở trong bán kính 35m mà nó dám đòi bỏ về hồi máu hoặc lùng sục -> Ép đánh tiếp!
            if (dist < 35f && (isReturning || isSearching || targetPlayer == null))
            {
                isReturning = false;
                isSearching = false;
                isAlerted = true;
                targetPlayer = lockedTarget; // Nhét lại Player vào não nó
                lastKnownPosition = lockedTarget.position;
            }
            // Chỉ tha cho nó về nhà nếu bạn thực sự chạy xa hơn 35m
            else if (dist >= 35f)
            {
                lockedTarget = null;
            }
        }
    }

    public override void OnCombatBehavior(Transform player)
    {
        // Nếu bị đánh, chết, hoặc đang bối rối lùng sục thì ngắt chiêu ngay
        if (isHit || isDead || isSearching || isReturning) { StopSummoning(); return; }

        if (isSummoning) return;

        activeSummons.RemoveAll(m => m == null || m.isDead);

        float distance = Vector3.Distance(transform.position, player.position);

        // --- 1. XA QUÁ -> CHẠY LẠI GẦN ---
        if (distance > data.attackRange)
        {
            MoveToPosition(player.position, true); 
        }
        // --- 2. GẦN QUÁ -> LÙI LẠI (THẢ DIỀU) ---
        else if (distance < safeDistance)
        {
            Vector3 dir = (transform.position - player.position).normalized;
            MoveToPosition(transform.position + dir * 3f, true);
        }
        // --- 3. VỪA TẦM AN TOÀN -> GỌI ĐỆ ---
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
    }

    private void StopSummoning()
    {
        if (isSummoning)
        {
            isSummoning = false;
            lastSummonTime = -999f; 
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