using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class VillagerWanderNPC : NPCController
{
    [Header("Movement")]
    public NavMeshAgent agent;
    public Transform[] waypoints;

    [Header("Idle")]
    public float idleTime = 5f;
    public float benchSitTime = 20f;

    int index;
    bool isBusy;
    Coroutine idleRoutine;

    protected override void Start()
    {
        base.Start();

        if (!agent) agent = GetComponent<NavMeshAgent>();
        Unlock();
        MoveNext();
    }

    void MoveNext()
    {
        if (waypoints.Length == 0) return;

        agent.isStopped = false;
        agent.SetDestination(waypoints[index].position);
        anim.SetBool("IsWalking", true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isBusy) return;
        if (other.transform != waypoints[index]) return;

        idleRoutine = StartCoroutine(IdleAtPoint(other));
    }

    IEnumerator IdleAtPoint(Collider point)
    {
        isBusy = true;

        agent.isStopped = true;
        anim.SetBool("IsWalking", false);

        if (point.CompareTag("Bench"))
        {
            Transform seat = point.transform.Find("SeatPoint");
            if (seat)
            {
                agent.enabled = false;
                transform.position = seat.position;
                transform.rotation = seat.rotation;
            }

            anim.SetTrigger("SitDown");
            yield return new WaitForSeconds(benchSitTime);
            anim.SetTrigger("StandUp");

            agent.enabled = true;
        }
        else
        {
            Face(point.transform);
            yield return new WaitForSeconds(idleTime);
        }

        index = (index + 1) % waypoints.Length;
        isBusy = false;
        MoveNext();
    }

    public void ForceTalk(Transform other, float time)
    {
        if (isBusy) return;
        StartCoroutine(TalkRoutine(other, time));
    }

    IEnumerator TalkRoutine(Transform other, float time)
    {
        isBusy = true;

        agent.isStopped = true;
        anim.SetBool("IsWalking", false);

        Face(other);
        anim.SetBool("IsTalking", true);

        yield return new WaitForSeconds(time);

        anim.SetBool("IsTalking", false);
        isBusy = false;

        MoveNext();
    }

    void Face(Transform t)
    {
        Vector3 dir = t.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    public override void Interact() { }
}