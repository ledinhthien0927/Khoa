using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class FarmerNPC : NPCController
{
    [Header("Move")]
    public float walkSpeed = 1.5f;
    public float randomWalkSpeed = 1.2f;
    public float randomWalkRadius = 4f;
    public float stopDistance = 0.15f;

    [Header("Idle Time")]
    public float idleBeforeWork = 20f;
    public float idleBetweenAction = 10f;

    [Header("Angry")]
    public float angryDuration = 3f;
    public float angryCooldown = 5f;

    [Header("Targets")]
    public Transform fieldPoint;
    public Transform wellPoint;

    [Header("Particles")]
    public ParticleSystem dirtParticle;
    public ParticleSystem seedParticle;
    public ParticleSystem waterParticle;

    NavMeshAgent agent;

    bool isAngry;
    bool isDoingAction;
    float lastAngryTime;

    Coroutine mainRoutine;

    // ================= START =================
    protected override void Start()
    {
        base.Start();

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.stoppingDistance = stopDistance;

        Lock();
        mainRoutine = StartCoroutine(MainLoop());
    }

    // ================= MAIN LOOP =================
    IEnumerator MainLoop()
    {
        // Idle / Walk ban đầu
        yield return StartCoroutine(IdleOrWalkRandom(idleBeforeWork));

        while (true)
        {
            if (isAngry)
            {
                yield return null;
                continue;
            }

            yield return StartCoroutine(DoWork());
        }
    }

    // ================= WORK =================
    IEnumerator DoWork()
    {
        // 👉 Ra ruộng
        yield return StartCoroutine(MoveTo(fieldPoint));

        // 1️⃣ Hoe
        yield return StartCoroutine(DoAction("Hoe", "Hoe"));
        yield return StartCoroutine(IdleOrWalkRandom(idleBetweenAction));

        // 2️⃣ Plant
        yield return StartCoroutine(DoAction("Plant", "Plant"));
        yield return StartCoroutine(IdleOrWalkRandom(idleBetweenAction));

        // 👉 Ra giếng
        yield return StartCoroutine(MoveTo(wellPoint));

        // 3️⃣ Water
        yield return StartCoroutine(DoAction("Water", "Water"));
        yield return StartCoroutine(IdleOrWalkRandom(idleBetweenAction));
    }

    // ================= MOVE =================
    IEnumerator MoveTo(Transform target)
    {
        if (!target || isAngry) yield break;

        agent.speed = walkSpeed;
        agent.SetDestination(target.position);
        anim.SetBool("IsWalking", true);

        while (!agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
        {
            if (isAngry) yield break;

            Vector3 dir = agent.desiredVelocity.normalized;
            FaceDirection(dir);
            yield return null;
        }

        anim.SetBool("IsWalking", false);
    }

    // ================= IDLE / WALK RANDOM =================
    IEnumerator IdleOrWalkRandom(float duration)
    {
        float timer = 0f;

        while (timer < duration && !isAngry)
        {
            bool willWalk = Random.value > 0.4f;

            if (willWalk)
            {
                Vector3 dest = GetRandomNavMeshPoint(randomWalkRadius);

                agent.speed = randomWalkSpeed;
                agent.SetDestination(dest);
                anim.SetBool("IsWalking", true);

                while (!agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
                {
                    if (isAngry) yield break;

                    Vector3 dir = agent.desiredVelocity.normalized;
                    FaceDirection(dir);

                    timer += Time.deltaTime;
                    yield return null;

                    if (timer >= duration)
                        break;
                }
            }
            else
            {
                agent.ResetPath();
                anim.SetBool("IsWalking", false);

                float idleTime = Random.Range(1.5f, 3f);
                yield return new WaitForSeconds(idleTime);
                timer += idleTime;
            }
        }

        agent.ResetPath();
        anim.SetBool("IsWalking", false);
    }

    // ================= ACTION =================
    IEnumerator DoAction(string trigger, string stateName)
    {
        if (isAngry) yield break;

        isDoingAction = true;

        agent.ResetPath();
        anim.SetBool("IsWalking", false);
        ResetTriggers();
        yield return null;

        anim.SetTrigger(trigger);

        yield return StartCoroutine(WaitForAnimation(stateName));

        isDoingAction = false;
    }

    // ================= WAIT ANIMATION =================
    IEnumerator WaitForAnimation(string stateName)
    {
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            if (isAngry) yield break;
            yield return null;
        }

        while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            if (isAngry) yield break;
            yield return null;
        }
    }

    // ================= ANGRY =================
    public void TriggerAngry()
    {
        if (Time.time - lastAngryTime < angryCooldown) return;

        lastAngryTime = Time.time;

        if (mainRoutine != null)
            StopCoroutine(mainRoutine);

        StopAllCoroutines();

        isAngry = true;
        isDoingAction = false;

        agent.ResetPath();
        anim.SetBool("IsWalking", false);
        ResetTriggers();

        anim.SetTrigger("Angry");

        StartCoroutine(AngryRoutine());
    }

    IEnumerator AngryRoutine()
    {
        yield return new WaitForSeconds(angryDuration);

        isAngry = false;

        // Sau Angry → quay lại Idle rồi Work
        mainRoutine = StartCoroutine(MainLoop());
    }

    // ================= ROTATION =================
    void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.deltaTime);
    }

    // ================= NAVMESH RANDOM =================
    Vector3 GetRandomNavMeshPoint(float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * radius;
            randomPos.y = transform.position.y;

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }

        return transform.position;
    }

    // ================= PARTICLE (EVENT) =================
    public void PlayDirt()  { dirtParticle?.Play(); }
    public void PlaySeed()  { seedParticle?.Play(); }
    public void PlayWater() { waterParticle?.Play(); }

    // ================= UTIL =================
    void ResetTriggers()
    {
        anim.ResetTrigger("Hoe");
        anim.ResetTrigger("Plant");
        anim.ResetTrigger("Water");
        anim.ResetTrigger("Angry");
    }

    public override void Interact()
    {
        TriggerAngry();
    }
}