using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class QuestNavigation : MonoBehaviour
{
    public static QuestNavigation Instance;
    public LineRenderer line;
    public TextMeshProUGUI statusText;
    
    private Transform target;
    private bool isNavigating;

    void Awake() => Instance = this;

    public void ToggleNavigation(Transform newTarget, string questName)
    {
        if (isNavigating && target == newTarget) { StopNav(); return; }
        
        target = newTarget;
        isNavigating = true;
        line.enabled = true;
        statusText.gameObject.SetActive(true);
        statusText.text = "Đang dẫn đường: " + questName;
    }

    public void StopNav()
    {
        isNavigating = false;
        line.enabled = false;
        statusText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isNavigating || target == null) return;
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path))
        {
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);
        }
    }
}