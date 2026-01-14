using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public Transform player;

    public float chaseDistance = 8f;
    public float loseSightDistance = 12f;
    public float loseSightTime = 2f;

    private NavMeshAgent agent;
    private int patrolIndex = 0;

    private enum State { Patrol, Chase, Surround }
    private State currentState = State.Patrol;

    private float timeSinceLastSeen = 0f;

    // --- Surrounding support ---
    private Vector3 surroundTarget;
    private bool useSurround = false; // set true by manager when needed

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        GoToNextPatrolPoint();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- STATE LOGIC ---
        if (currentState == State.Patrol)
        {
            if (distanceToPlayer < chaseDistance)
            {
                currentState = State.Chase;
            }
            else
                Patrol();
        }
        else if (currentState == State.Chase)
        {
            if (distanceToPlayer < loseSightDistance)
            {
                timeSinceLastSeen = 0f;
                Chase();
            }
            else
            {
                timeSinceLastSeen += Time.deltaTime;

                if (timeSinceLastSeen >= loseSightTime)
                {
                    currentState = State.Patrol;
                    GoToNextPatrolPoint();
                }
                else
                {
                    agent.SetDestination(player.position);
                }
            }
        }
        else if (currentState == State.Surround)
        {
            Surround();
        }

        // If the manager wants this enemy to surround, force that state
        if (useSurround)
            currentState = State.Surround;
    }

    // ===============================
    // PATROL
    // ===============================
    void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.3f)
        {
            GoToNextPatrolPoint();
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[patrolIndex].position);
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    // ===============================
    // CHASE
    // ===============================
    void Chase()
    {
        agent.SetDestination(player.position);
    }

    // ===============================
    // SURROUND THE PLAYER
    // ===============================
    void Surround()
    {
        agent.SetDestination(surroundTarget);
    }

    // Called by the EnemyGroupManager
    public void SetSurroundTarget(Vector3 pos)
    {
        surroundTarget = pos;
        useSurround = true;
    }

    // Called by manager to switch back if needed
    public void StopSurround()
    {
        useSurround = false;
        currentState = State.Patrol;
        GoToNextPatrolPoint();
    }
}
