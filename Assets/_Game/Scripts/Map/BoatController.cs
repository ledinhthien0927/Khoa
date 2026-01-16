using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class BoatController : MonoBehaviour
{
    [Header("UI & Interactions")]
    public GameObject fPromptIcon;      
    public GameObject openMapMessage;   
    
    [Header("Boat Positions")]
    public Transform steeringPos;     // Vị trí đứng lái
    
    // [THAY ĐỔI] Gộp điểm lên/xuống thành 1 điểm duy nhất
    public Transform accessPoint;     // Điểm mép tàu (vừa để leo lên, vừa để leo xuống)
    
    [Header("Camera")]
    public GameObject boatCamera; 

    private NavMeshAgent _agent;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = true; 
        _agent.updatePosition = true;
    }

    void Start()
    {
        if (fPromptIcon != null) fPromptIcon.SetActive(false);
        if (openMapMessage != null) openMapMessage.SetActive(false);
        if (boatCamera != null) boatCamera.SetActive(false);
    }

    public void TogglePrompt(bool isVisible)
    {
        if (fPromptIcon != null) fPromptIcon.SetActive(isVisible);
        if (openMapMessage != null) openMapMessage.SetActive(isVisible);
    }

    public void SetBoatCamera(bool isActive)
    {
        if (boatCamera != null) boatCamera.SetActive(isActive);
    }

    // --- NAVMESH LOGIC ---
    public void SetDestination(Vector3 targetPos)
    {
        if (_agent != null)
        {
            _agent.isStopped = false;
            _agent.SetDestination(targetPos);
        }
    }

    public bool IsReachedDestination()
    {
        if (_agent.pathPending) return false;
        if (_agent.remainingDistance <= _agent.stoppingDistance + 0.5f)
        {
            if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f) return true;
        }
        return false;
    }
}