using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class VillagerWanderNPC : NPCController
{
    [Header("Movement")]
    public NavMeshAgent agent;
    public Transform[] waypoints;

    [Header("Idle Settings")]
    public float idleTime = 5f;
    public float benchSitTime = 20f;

    [Header("Sit Settings")]
    public float sitAnimTime = 1.2f;
    public float standAnimTime = 1.2f;

    private int currentIndex = 0;
    private bool isIdle = false;
    private Coroutine idleRoutine;

    // =====================================================
    protected override void Start()
    {
        base.Start();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = true;
        agent.updateUpAxis = true;

        Unlock();
        MoveToNextPoint();
    }

    // =====================================================
    void MoveToNextPoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(waypoints[currentIndex].position);

        anim.SetBool("IsWalking", true);
    }

    // =====================================================
    // TRIGGER ĐIỂM ĐẾN
    // =====================================================
    private void OnTriggerEnter(Collider other)
    {
        if (isIdle) return;
        if (other.transform != waypoints[currentIndex]) return;

        idleRoutine = StartCoroutine(IdleAtPoint(other));
    }

    // =====================================================
    IEnumerator IdleAtPoint(Collider point)
    {
        isIdle = true;

        // Dừng di chuyển
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        anim.SetBool("IsWalking", false);

        // ================= BENCH =================
        if (point.CompareTag("Bench"))
        {
            Transform seat = point.transform.Find("SeatPoint");

            if (seat != null)
            {
                // TẮT NAVMESH XOAY + DI CHUYỂN
                agent.updateRotation = false;
                agent.enabled = false;

                // SNAP CHUẨN VÀO GHẾ
                transform.position = seat.position;
                transform.rotation = seat.rotation;
            }

            // NGỒI
            anim.ResetTrigger("StandUp");
            anim.SetTrigger("SitDown");

            yield return new WaitForSeconds(sitAnimTime);

            // IDLE SITTING
            yield return new WaitForSeconds(benchSitTime);

            // ĐỨNG DẬY
            anim.ResetTrigger("SitDown");
            anim.SetTrigger("StandUp");

            yield return new WaitForSeconds(standAnimTime);

            // BẬT LẠI NAVMESH
            agent.enabled = true;
            agent.updateRotation = true;
        }
        // ================= NOTICE BOARD =================
        else if (point.CompareTag("NoticeBoard"))
        {
            FaceTarget(point.transform);
            anim.SetTrigger("LookBoard");
            yield return new WaitForSeconds(idleTime);
        }
        // ================= DEFAULT =================
        else
        {
            FaceTarget(point.transform);
            yield return new WaitForSeconds(idleTime);
        }

        // WAYPOINT TIẾP THEO
        currentIndex = (currentIndex + 1) % waypoints.Length;
        isIdle = false;

        MoveToNextPoint();
    }

    // =====================================================
    void FaceTarget(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
    public void ForceTalk(Transform other, float time)
    {
        if (isIdle) return;
        StartCoroutine(ForceTalkRoutine(other, time));
    }

    IEnumerator ForceTalkRoutine(Transform other, float time)
    {
        isIdle = true;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        anim.SetBool("IsWalking", false);

        Vector3 dir = other.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);

        anim.SetBool("IsTalking", true);

        yield return new WaitForSeconds(time);

        anim.SetBool("IsTalking", false);
        isIdle = false;

        MoveToNextPoint();
    }

    // =====================================================
    // PLAYER INTERACT
    // =====================================================
    public override void Interact() { }

    protected override void OnStartInteract()
    {
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);

        isIdle = false;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        anim.SetBool("IsWalking", false);
    }

    protected override void OnEndInteract()
    {
        MoveToNextPoint();
    }
}