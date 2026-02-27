using UnityEngine;
using System.Collections;

public class FarmerNPC : NPCController
{
    [Header("Move")]
    public float walkSpeed = 1.5f;
    public float stopDistance = 0.2f;

    [Header("Work")]
    public float workDelay = 2f;

    [Header("Angry")]
    public float angryCooldown = 5f;

    [Header("Targets")]
    public Transform wellPoint;
    public Transform fieldPoint;

    [Header("Particles")]
    public ParticleSystem dirtParticle;
    public ParticleSystem seedParticle;
    public ParticleSystem waterParticle;
    [Header("Idle / Walk Random")]
    public float randomWalkRadius = 1.5f;
    public float randomWalkSpeed = 1.2f;

    bool isWorking;
    bool isAngry;
    bool isDoingAction;

    float lastAngryTime;
    Coroutine workRoutine;

    protected override void Start()
    {
        base.Start();
        Lock();
        StartWork();
    }

    // ================= WORK LOOP =================
    void StartWork()
    {
        if (isWorking) return;
        isWorking = true;
        workRoutine = StartCoroutine(WorkLoop());
    }

    void StopWork()
    {
        isWorking = false;
        if (workRoutine != null)
            StopCoroutine(workRoutine);
    }

    IEnumerator WorkLoop()
    {
        yield return new WaitForSeconds(1f);

        while (isWorking && !isAngry)
        {
            // 👉 ĐI RA RUỘNG
            yield return StartCoroutine(MoveTo(fieldPoint));

            // 1️⃣ Cuốc đất
            yield return DoAction("Hoe", 2f);
            yield return new WaitForSeconds(10f);

            // 2️⃣ Gieo hạt
            yield return DoAction("Plant", 2f);
            yield return new WaitForSeconds(10f);

            // 👉 ĐI RA GIẾNG
            yield return StartCoroutine(MoveTo(wellPoint));

            // 3️⃣ Tưới nước
            yield return DoAction("Water", 2f);

            yield return new WaitForSeconds(workDelay);
        }
    }
IEnumerator IdleOrWalkRandom(float duration)
{
    float timer = 0f;

    while (timer < duration && !isAngry)
    {
        // 50% idle – 50% đi bộ
        bool willWalk = Random.value > 0.5f;

        if (willWalk)
        {
            // Chọn điểm ngẫu nhiên gần NPC
            Vector3 randomDir = Random.insideUnitCircle.normalized;
            Vector3 targetPos = transform.position + 
                new Vector3(randomDir.x, 0, randomDir.y) * randomWalkRadius;

            anim.SetBool("IsWalking", true);

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    randomWalkSpeed * Time.deltaTime
                );

                timer += Time.deltaTime;
                yield return null;

                if (timer >= duration || isAngry)
                    break;
            }
        }
        else
        {
            // Idle
            anim.SetBool("IsWalking", false);

            float idleTime = Random.Range(1f, 3f);
            float t = 0f;

            while (t < idleTime && timer < duration)
            {
                t += Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    anim.SetBool("IsWalking", false);
}
    // ================= ACTION =================
    IEnumerator DoAction(string trigger, float duration)
    {
        isDoingAction = true;

        anim.SetBool("IsWalking", false);
        ResetTriggers();
        yield return null;

        anim.SetTrigger(trigger);
        yield return new WaitForSeconds(duration);

        isDoingAction = false;
    }

    // ================= MOVE =================
        IEnumerator MoveTo(Transform target)
    {
        if (!target) yield break;
        if (isDoingAction) yield break;

        anim.SetBool("IsWalking", true);

        Vector3 targetPos = target.position;
        targetPos.y = transform.position.y;

        while (Vector3.Distance(transform.position, targetPos) > stopDistance)
        {
            if (isAngry) break;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                walkSpeed * Time.deltaTime
            );

            yield return null;
        }

        anim.SetBool("IsWalking", false);
    }
    // ================= ANGRY =================
        public void TriggerAngry()
    {
        if (isAngry) return;
        if (Time.time - lastAngryTime < angryCooldown) return;

        lastAngryTime = Time.time;
        isAngry = true;

        StopAllCoroutines();   // 🔥 RẤT QUAN TRỌNG
        ResetTriggers();
        anim.SetTrigger("Angry");
    }
    public override void OnDialogueFinished()
    {
        base.OnDialogueFinished();

        isAngry = false;
        StartWork();   // WorkLoop sẽ gọi MoveTo lại
    }
    // ================= PARTICLE (ĐƯỢC GỌI TỪ EVENT) =================
    public void PlayDirt()
    {
        dirtParticle?.Play();
    }

    public void PlaySeed()
    {
        seedParticle?.Play();
    }

    public void PlayWater()
    {
        waterParticle?.Play();
    }

    // ================= UTIL =================
    void ResetTriggers()
    {
        anim.ResetTrigger("Hoe");
        anim.ResetTrigger("Plant");
        anim.ResetTrigger("Water");
        anim.ResetTrigger("Angry");
    }

    public override void Interact() { }
}