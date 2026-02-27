using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DifferentNPC : MonoBehaviour
{
    [Header("Movement")]
    public NavMeshAgent agent;
    public Animator anim;

    public float wanderRadius = 8f;
    public float idleTime = 2f;
    public float minMoveDistance = 2.5f;
    public float stuckTimeout = 3f;

    [Header("Talk")]
    public float talkDuration = 5f;
    public float talkCooldown = 30f;

    bool isTalking;
    bool canTalk = true;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponent<Animator>();

        agent.autoBraking = true;
        agent.stoppingDistance = 0.2f;

        StartCoroutine(WanderLoop());
    }

    // =====================================================
    IEnumerator WanderLoop()
    {
        while (true)
        {
            if (isTalking)
            {
                yield return null;
                continue;
            }

            // -------- IDLE --------
            agent.isStopped = true;
            agent.ResetPath();
            anim.SetBool("IsWalking", false);

            yield return new WaitForSeconds(idleTime);

            if (isTalking) continue;

            // -------- WALK --------
            Vector3 destination;
            if (!FindValidPoint(out destination))
                continue;

            agent.isStopped = false;
            agent.SetDestination(destination);
            anim.SetBool("IsWalking", true);

            float timer = 0f;

            while (true)
            {
                if (isTalking) break;

                if (agent.pathStatus == NavMeshPathStatus.PathComplete &&
                    agent.remainingDistance <= agent.stoppingDistance)
                    break;

                timer += Time.deltaTime;
                if (timer > stuckTimeout)
                    break;

                yield return null;
            }

            anim.SetBool("IsWalking", false);
        }
    }

    // =====================================================
    bool FindValidPoint(out Vector3 result)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir += transform.position; // QUANH VỊ TRÍ HIỆN TẠI

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(randomDir, out hit, wanderRadius, NavMesh.AllAreas))
                continue;

            if (Vector3.Distance(transform.position, hit.position) < minMoveDistance)
                continue;

            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(hit.position, path);

            if (path.status != NavMeshPathStatus.PathComplete)
                continue;

            result = hit.position;
            return true;
        }

        result = transform.position;
        return false;
    }

    // =====================================================
    void OnTriggerEnter(Collider other)
    {
        if (!canTalk || isTalking) return;
        if (!other.CompareTag("Villager")) return;

        StartCoroutine(TalkRoutine(other.transform));
    }

    IEnumerator TalkRoutine(Transform target)
    {
        isTalking = true;
        canTalk = false;

        agent.isStopped = true;
        agent.ResetPath();
        anim.SetBool("IsWalking", false);

        Face(target);
        anim.SetBool("IsTalking", true);

        VillagerWanderNPC villager = target.GetComponent<VillagerWanderNPC>();
        if (villager)
            villager.ForceTalk(transform, talkDuration);

        yield return new WaitForSeconds(talkDuration);

        anim.SetBool("IsTalking", false);
        isTalking = false;

        yield return new WaitForSeconds(talkCooldown);
        canTalk = true;
    }

    void Face(Transform t)
    {
        Vector3 dir = t.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}