using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DifferentNPC : MonoBehaviour
{
    [Header("Movement")]
    public NavMeshAgent agent;
    public Animator anim;
    public float wanderRadius = 5f;
    public float idleTime = 3f;

    [Header("Talk")]
    public float talkDuration = 5f;

    bool isIdle;
    bool isTalking;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponent<Animator>();

        StartCoroutine(WanderRoutine());
    }

    // =====================================================
    IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (isTalking) yield return null;

            // IDLE
            isIdle = true;
            anim.SetBool("IsWalking", false);
            yield return new WaitForSeconds(idleTime);

            if (isTalking) continue;

            // WALK
            Vector3 randomPos = RandomNavmeshLocation(wanderRadius);
            agent.isStopped = false;
            agent.SetDestination(randomPos);
            anim.SetBool("IsWalking", true);

            isIdle = false;

            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                if (isTalking) yield break;
                yield return null;
            }

            anim.SetBool("IsWalking", false);
        }
    }

    // =====================================================
    void OnTriggerEnter(Collider other)
    {
        if (isTalking) return;

        if (other.CompareTag("Villager"))
        {
            StartCoroutine(TalkWithVillager(other.transform));
        }
    }

    // =====================================================
    IEnumerator TalkWithVillager(Transform villager)
    {
        isTalking = true;

        // DỪNG DI CHUYỂN
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        anim.SetBool("IsWalking", false);

        // QUAY MẶT
        Vector3 dir = villager.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);

        // BẬT TALK
        anim.SetBool("IsTalking", true);

        // GỌI NPC DÂN LÀNG TALK
        VillagerWanderNPC villagerNPC = villager.GetComponent<VillagerWanderNPC>();
        if (villagerNPC != null)
        {
            villagerNPC.ForceTalk(transform, talkDuration);
        }

        yield return new WaitForSeconds(talkDuration);

        anim.SetBool("IsTalking", false);
        isTalking = false;

        StartCoroutine(WanderRoutine());
    }

    // =====================================================
    Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDir = Random.insideUnitSphere * radius;
        randomDir += transform.position;

        NavMeshHit hit;
        NavMesh.SamplePosition(randomDir, out hit, radius, NavMesh.AllAreas);
        return hit.position;
    }
}